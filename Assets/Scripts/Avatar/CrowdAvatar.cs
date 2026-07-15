using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CrowdAvatar : MonoBehaviour
{
    public TextMesh nameText;
    [SerializeField] private TMP_FontAsset japaneseFontAsset;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite standSprite;
    public Sprite walkSprite;
    public Sprite sitSprite;

    [Header("Movement")]
    public float moveSpeed = 2.0f;
    public float walkRadius = 4.0f;
    public float minWaitTime = 1.0f;
    public float maxWaitTime = 4.0f;
    public float facingDeadZone = 0.001f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float fixedY;
    private float waitTimer;
    private bool isWalking;
    private TMP_Text tmpNameText;
    private AvatarSpeechBubble speechBubble;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (nameText == null)
        {
            nameText = GetComponentInChildren<TextMesh>();
        }

        tmpNameText = GetComponentInChildren<TMP_Text>();
        speechBubble = GetComponent<AvatarSpeechBubble>();
        if (speechBubble == null)
        {
            speechBubble = gameObject.AddComponent<AvatarSpeechBubble>();
        }

        ConfigurePlayerLabel();
    }

    private void Start()
    {
        startPosition = transform.position;
        fixedY = startPosition.y;

        if (spriteRenderer != null && standSprite != null)
        {
            spriteRenderer.sprite = standSprite;
        }

        SetNewTargetPosition();
    }

    private void Update()
    {
        if (isWalking)
        {
            Vector3 previousPosition = transform.position;
            Vector3 nextPosition = Vector3.MoveTowards(previousPosition, targetPosition, moveSpeed * Time.deltaTime);
            nextPosition.y = fixedY;
            transform.position = nextPosition;
            ApplyFacing(nextPosition.x - previousPosition.x);

            if (spriteRenderer != null && walkSprite != null)
            {
                spriteRenderer.sprite = walkSprite;
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                Vector3 snappedPosition = transform.position;
                snappedPosition.y = fixedY;
                transform.position = snappedPosition;

                isWalking = false;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
                ApplyIdleSprite();
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                SetNewTargetPosition();
            }
        }
    }

    private void SetNewTargetPosition()
    {
        float randomX = Random.Range(-walkRadius, walkRadius);
        float randomZ = Random.Range(-walkRadius, walkRadius);
        targetPosition = new Vector3(startPosition.x + randomX, fixedY, startPosition.z + randomZ);
        ApplyFacing(targetPosition.x - transform.position.x);
        isWalking = true;
    }

    private void ApplyFacing(float deltaX)
    {
        if (spriteRenderer == null || Mathf.Abs(deltaX) <= facingDeadZone)
        {
            return;
        }

        spriteRenderer.flipX = deltaX > 0f;
    }

    private void ApplyIdleSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (Random.value > 0.5f && sitSprite != null)
        {
            spriteRenderer.sprite = sitSprite;
        }
        else if (standSprite != null)
        {
            spriteRenderer.sprite = standSprite;
        }
    }

    public void SetPlayerName(string playerName)
    {
        SetPlayerInfo(playerName, null);
    }

    public void SetPlayerInfo(string playerName, IReadOnlyList<string> messages)
    {
        if (nameText != null)
        {
            nameText.text = playerName;
        }

        if (tmpNameText != null)
        {
            tmpNameText.text = $"<b>{EscapeRichText(playerName)}</b>";
        }

        if (speechBubble != null)
        {
            speechBubble.Configure(japaneseFontAsset, messages, tmpNameText, spriteRenderer);
        }
    }

    private void ConfigurePlayerLabel()
    {
        if (tmpNameText == null)
        {
            return;
        }

        if (japaneseFontAsset != null)
        {
            tmpNameText.font = japaneseFontAsset;
        }

        tmpNameText.richText = true;
        tmpNameText.alignment = TextAlignmentOptions.Bottom;
        tmpNameText.enableAutoSizing = true;
        tmpNameText.fontSizeMin = 14f;
        tmpNameText.fontSizeMax = 30f;
        tmpNameText.color = Color.white;
        tmpNameText.outlineColor = new Color32(27, 39, 51, 255);
        tmpNameText.outlineWidth = 0.16f;

        if (spriteRenderer != null)
        {
            Renderer labelRenderer = tmpNameText.GetComponent<Renderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                labelRenderer.sortingOrder = spriteRenderer.sortingOrder + 22;
            }
        }

        RectTransform labelTransform = tmpNameText.rectTransform;
        labelTransform.anchoredPosition = new Vector2(0f, 5.5f);
        labelTransform.sizeDelta = new Vector2(22f, 4f);
        labelTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static string EscapeRichText(string value)
    {
        return (value ?? string.Empty)
            .Replace("<", "\uFF1C")
            .Replace(">", "\uFF1E");
    }

    public void ApplyCostume(CostumeEntry costume)
    {
        if (costume == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (costume.standSprite != null)
        {
            standSprite = costume.standSprite;
        }

        if (costume.walkSprite != null)
        {
            walkSprite = costume.walkSprite;
        }

        if (costume.sitSprite != null)
        {
            sitSprite = costume.sitSprite;
        }

        if (standSprite != null)
        {
            spriteRenderer.sprite = standSprite;
        }
    }
}
