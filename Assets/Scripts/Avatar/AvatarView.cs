using System.Collections;
using TMPro;
using UnityEngine;

public class AvatarView : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMesh labelText;
    [SerializeField] private TMP_Text tmpLabelText;

    [Header("Motion")]
    [SerializeField] private float fadeSeconds = 1f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 baseLocalPosition;
    private Color originalSpriteColor = Color.white;
    private float staySeconds = 10f;
    private Coroutine lifeRoutine;

    private void Awake()
    {
        ResolveReferences();
        baseLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        Vector3 currentPosition = transform.localPosition;
        currentPosition.y = baseLocalPosition.y + offsetY;
        transform.localPosition = currentPosition;
    }

    public void Initialize(Encounter encounter, float displaySeconds)
    {
        ResolveReferences();

        staySeconds = Mathf.Max(0.1f, displaySeconds);
        baseLocalPosition = transform.localPosition;

        string targetId = string.IsNullOrWhiteSpace(encounter?.target_id)
            ? "unknown"
            : encounter.target_id.Trim();

        gameObject.name = $"Avatar_{targetId}";
        SetLabel(targetId);

        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }

        lifeRoutine = StartCoroutine(LifeTimer());
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMesh>();
        }

        if (tmpLabelText == null)
        {
            tmpLabelText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void SetLabel(string value)
    {
        if (labelText != null)
        {
            labelText.text = value;
        }

        if (tmpLabelText != null)
        {
            tmpLabelText.text = value;
        }
    }

    private IEnumerator LifeTimer()
    {
        float visibleSeconds = Mathf.Max(0f, staySeconds - fadeSeconds);
        yield return new WaitForSeconds(visibleSeconds);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeSeconds);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = originalSpriteColor;
            color.a *= alpha;
            spriteRenderer.color = color;
        }

        if (labelText != null)
        {
            Color color = labelText.color;
            color.a = alpha;
            labelText.color = color;
        }

        if (tmpLabelText != null)
        {
            Color color = tmpLabelText.color;
            color.a = alpha;
            tmpLabelText.color = color;
        }
    }
}
