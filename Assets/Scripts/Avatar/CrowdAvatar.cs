using TMPro;
using UnityEngine;

public class CrowdAvatar : MonoBehaviour
{
    public TextMesh nameText;

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

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private bool isWalking;
    private TMP_Text tmpNameText;

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
    }

    private void Start()
    {
        startPosition = transform.position;

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
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (spriteRenderer != null && walkSprite != null)
            {
                spriteRenderer.sprite = walkSprite;
                spriteRenderer.flipX = targetPosition.x < transform.position.x;
            }

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
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
        targetPosition = new Vector3(startPosition.x + randomX, startPosition.y, startPosition.z + randomZ);
        isWalking = true;
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
        if (nameText != null)
        {
            nameText.text = playerName;
        }

        if (tmpNameText != null)
        {
            tmpNameText.text = playerName;
        }
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
