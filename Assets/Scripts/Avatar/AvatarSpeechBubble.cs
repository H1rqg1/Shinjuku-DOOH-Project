using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class AvatarSpeechBubble : MonoBehaviour
{
    private const int MaxVisibleBubbles = 2;

    private static Sprite bubbleSprite;
    private static int activeBubbleCount;

    [Header("Timing")]
    [SerializeField] private Vector2 firstDelayRange = new Vector2(3f, 12f);
    [SerializeField] private Vector2 visibleDurationRange = new Vector2(2.5f, 4.5f);
    [SerializeField] private Vector2 hiddenIntervalRange = new Vector2(8f, 18f);
    [SerializeField, Min(0f)] private float fadeSeconds = 0.2f;

    [Header("Layout")]
    [SerializeField] private Vector3 localPosition = new Vector3(0f, 8.3f, 0f);
    [SerializeField] private Vector2 backgroundScale = new Vector2(5.6f, 4.6f);
    [SerializeField] private Vector2 textArea = new Vector2(22f, 9f);

    private readonly List<string> messages = new List<string>();
    private GameObject bubbleRoot;
    private SpriteRenderer backgroundRenderer;
    private TextMeshPro messageText;
    private Coroutine messageRoutine;
    private int lastMessageIndex = -1;
    private bool hasDisplaySlot;

    public void Configure(
        TMP_FontAsset fontAsset,
        IReadOnlyList<string> sourceMessages,
        TMP_Text labelTemplate,
        SpriteRenderer avatarRenderer)
    {
        messages.Clear();
        if (sourceMessages != null)
        {
            for (int i = 0; i < sourceMessages.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(sourceMessages[i]))
                {
                    messages.Add(sourceMessages[i].Trim());
                }
            }
        }

        EnsureVisuals(fontAsset, labelTemplate, avatarRenderer);
        RestartMessageRoutine();
    }

    private void OnDisable()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        ReleaseDisplaySlot();
        HideBubble();
    }

    private void EnsureVisuals(
        TMP_FontAsset fontAsset,
        TMP_Text labelTemplate,
        SpriteRenderer avatarRenderer)
    {
        if (bubbleRoot == null)
        {
            bubbleRoot = new GameObject("SpeechBubble");
            bubbleRoot.transform.SetParent(transform, false);
            bubbleRoot.transform.localPosition = localPosition;

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(bubbleRoot.transform, false);
            backgroundObject.transform.localScale = new Vector3(backgroundScale.x, backgroundScale.y, 1f);
            backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = GetBubbleSprite();

            GameObject textObject = new GameObject("MessageText", typeof(RectTransform));
            textObject.transform.SetParent(bubbleRoot.transform, false);
            messageText = textObject.AddComponent<TextMeshPro>();

            RectTransform textTransform = messageText.rectTransform;
            textTransform.localPosition = new Vector3(0f, 0.35f, -0.02f);
            textTransform.localScale = new Vector3(0.5f, 0.5f, 1f);
            textTransform.sizeDelta = textArea;

            messageText.alignment = TextAlignmentOptions.Center;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 12f;
            messageText.fontSizeMax = 24f;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
            messageText.fontStyle = FontStyles.Bold;
            messageText.richText = false;
            messageText.color = new Color32(27, 39, 51, 255);
        }

        TMP_FontAsset resolvedFont = fontAsset != null
            ? fontAsset
            : (labelTemplate != null ? labelTemplate.font : null);
        if (resolvedFont != null)
        {
            messageText.font = resolvedFont;
        }

        int sortingLayerId = avatarRenderer != null ? avatarRenderer.sortingLayerID : 0;
        int baseSortingOrder = avatarRenderer != null ? avatarRenderer.sortingOrder : 0;
        backgroundRenderer.sortingLayerID = sortingLayerId;
        backgroundRenderer.sortingOrder = baseSortingOrder + 20;
        Renderer textRenderer = messageText.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerID = sortingLayerId;
            textRenderer.sortingOrder = baseSortingOrder + 21;
        }

        HideBubble();
    }

    private void RestartMessageRoutine()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        ReleaseDisplaySlot();
        HideBubble();
        lastMessageIndex = -1;

        if (isActiveAndEnabled && messages.Count > 0)
        {
            messageRoutine = StartCoroutine(MessageLoop());
        }
    }

    private IEnumerator MessageLoop()
    {
        yield return new WaitForSeconds(RandomInRange(firstDelayRange));

        while (messages.Count > 0)
        {
            while (!TryAcquireDisplaySlot())
            {
                yield return new WaitForSeconds(Random.Range(0.8f, 2.2f));
            }

            int messageIndex = GetRandomMessageIndex();
            lastMessageIndex = messageIndex;
            messageText.text = messages[messageIndex];
            bubbleRoot.transform.localPosition = localPosition + new Vector3(
                Random.Range(-1.2f, 1.2f),
                Random.Range(0f, 1.2f),
                0f);

            bubbleRoot.SetActive(true);
            yield return FadeBubble(0f, 1f);

            float visibleSeconds = Mathf.Max(0.1f, RandomInRange(visibleDurationRange));
            yield return new WaitForSeconds(Mathf.Max(0f, visibleSeconds - fadeSeconds * 2f));
            yield return FadeBubble(1f, 0f);

            HideBubble();
            ReleaseDisplaySlot();
            yield return new WaitForSeconds(RandomInRange(hiddenIntervalRange));
        }

        ReleaseDisplaySlot();
        messageRoutine = null;
    }

    private bool TryAcquireDisplaySlot()
    {
        if (hasDisplaySlot)
        {
            return true;
        }

        if (activeBubbleCount >= MaxVisibleBubbles)
        {
            return false;
        }

        activeBubbleCount++;
        hasDisplaySlot = true;
        return true;
    }

    private void ReleaseDisplaySlot()
    {
        if (!hasDisplaySlot)
        {
            return;
        }

        activeBubbleCount = Mathf.Max(0, activeBubbleCount - 1);
        hasDisplaySlot = false;
    }

    private int GetRandomMessageIndex()
    {
        if (messages.Count <= 1)
        {
            return 0;
        }

        int index = Random.Range(0, messages.Count - 1);
        return index >= lastMessageIndex ? index + 1 : index;
    }

    private IEnumerator FadeBubble(float from, float to)
    {
        float duration = Mathf.Max(0f, fadeSeconds);
        if (duration <= 0f)
        {
            SetVisualAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetVisualAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetVisualAlpha(to);
    }

    private void HideBubble()
    {
        if (bubbleRoot == null)
        {
            return;
        }

        SetVisualAlpha(0f);
        bubbleRoot.SetActive(false);
    }

    private void SetVisualAlpha(float alpha)
    {
        if (backgroundRenderer != null)
        {
            Color color = backgroundRenderer.color;
            color.a = alpha;
            backgroundRenderer.color = color;
        }

        if (messageText != null)
        {
            Color color = messageText.color;
            color.a = alpha;
            messageText.color = color;
        }
    }

    private static float RandomInRange(Vector2 range)
    {
        float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return Random.Range(min, max);
    }

    private static Sprite GetBubbleSprite()
    {
        if (bubbleSprite != null)
        {
            return bubbleSprite;
        }

        const int width = 256;
        const int height = 128;
        const int outerBodyBottom = 24;
        const int border = 7;
        const float outerRadius = 24f;
        const float innerRadius = 18f;
        Color32 transparent = new Color32(0, 0, 0, 0);
        Color32 borderColor = new Color32(37, 52, 64, 245);
        Color32 fillColor = new Color32(255, 255, 255, 248);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "DOOH Speech Bubble",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool outerBody = IsInsideRoundedRect(
                    x,
                    y,
                    0f,
                    outerBodyBottom,
                    width - 1f,
                    height - 1f,
                    outerRadius);
                bool outerTail = IsInsideTail(x, y, width * 0.5f, 0f, outerBodyBottom + 5f, 28f);
                bool innerBody = IsInsideRoundedRect(
                    x,
                    y,
                    border,
                    outerBodyBottom + border,
                    width - 1f - border,
                    height - 1f - border,
                    innerRadius);
                bool innerTail = IsInsideTail(
                    x,
                    y,
                    width * 0.5f,
                    border + 2f,
                    outerBodyBottom + border + 1f,
                    18f);

                pixels[y * width + x] = innerBody || innerTail
                    ? fillColor
                    : outerBody || outerTail ? borderColor : transparent;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        bubbleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f);
        bubbleSprite.name = "DOOH Speech Bubble";
        bubbleSprite.hideFlags = HideFlags.HideAndDontSave;
        return bubbleSprite;
    }

    private static bool IsInsideRoundedRect(
        float x,
        float y,
        float left,
        float bottom,
        float right,
        float top,
        float radius)
    {
        if (x < left || x > right || y < bottom || y > top)
        {
            return false;
        }

        float nearestX = Mathf.Clamp(x, left + radius, right - radius);
        float nearestY = Mathf.Clamp(y, bottom + radius, top - radius);
        float deltaX = x - nearestX;
        float deltaY = y - nearestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }

    private static bool IsInsideTail(
        float x,
        float y,
        float centerX,
        float tipY,
        float baseY,
        float halfBaseWidth)
    {
        if (y < tipY || y > baseY || baseY <= tipY)
        {
            return false;
        }

        float progress = (y - tipY) / (baseY - tipY);
        return Mathf.Abs(x - centerX) <= halfBaseWidth * progress;
    }
}
