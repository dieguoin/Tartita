using System.Collections;
using UnityEngine;

public class SpecialPlatform : MonoBehaviour
{
    public enum PlatformType
    {
        Standard,
        BouncyJelly,
        Crumbling,
        Moving
    }

    [Header("Platform Type")]
    [SerializeField] private PlatformType platformType = PlatformType.Standard;

    [Header("Bouncy Jelly Settings")]
    [SerializeField] private float jumpMultiplier = 1.75f;
    [SerializeField] private float squishScaleY = 0.55f;
    [SerializeField] private float squishDuration = 0.22f;

    [Header("Crumbling Cookie Settings")]
    [SerializeField] private float crumbleDelay = 0.75f;
    [SerializeField] private float shakeAmount = 0.08f;
    [SerializeField] private float fallSpeed = 9f;

    [Header("Moving Wafer Settings")]
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float leftBound = -2.0f;
    [SerializeField] private float rightBound = 2.0f;

    public PlatformType Type => platformType;

    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private float moveDirection = 1f;
    private bool isCrumbling = false;
    private bool isFalling = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Resets the platform state when retrieved from the object pool.
    /// </summary>
    public void ResetSpecialPlatform()
    {
        StopAllCoroutines();
        isCrumbling = false;
        isFalling = false;
        if (platformCollider == null) platformCollider = GetComponent<Collider2D>();
        if (platformCollider != null) platformCollider.enabled = true;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (originalScale != Vector3.zero) transform.localScale = originalScale;
        if (platformType == PlatformType.Moving)
        {
            moveDirection = Random.value > 0.5f ? 1f : -1f;
        }
    }

    /// <summary>
    /// Configures platform parameters dynamically according to the current difficulty tier.
    /// </summary>
    public void ConfigureDifficulty(float speed, float crumbleTime, float widthScaleMultiplier)
    {
        ConfigureDifficulty(speed, crumbleTime, widthScaleMultiplier, leftBound, rightBound);
    }

    /// <summary>
    /// Configures platform parameters including dynamic screen-aware movement boundaries.
    /// </summary>
    public void ConfigureDifficulty(float speed, float crumbleTime, float widthScaleMultiplier, float dynamicLeftBound, float dynamicRightBound)
    {
        if (originalScale == Vector3.zero)
        {
            originalScale = transform.localScale;
        }

        moveSpeed = speed;
        crumbleDelay = crumbleTime;
        transform.localScale = new Vector3(originalScale.x * widthScaleMultiplier, originalScale.y, originalScale.z);

        if (!float.IsNaN(dynamicLeftBound) && !float.IsNaN(dynamicRightBound) && dynamicLeftBound < dynamicRightBound)
        {
            leftBound = dynamicLeftBound;
            rightBound = dynamicRightBound;
        }
    }

    /// <summary>
    /// Explicitly sets the horizontal movement boundaries for moving platforms.
    /// </summary>
    public void SetMovementBounds(float left, float right)
    {
        if (left < right)
        {
            leftBound = left;
            rightBound = right;
        }
    }

    private void Start()
    {
        if (platformType == PlatformType.Moving)
        {
            // Pick a random starting direction
            moveDirection = Random.value > 0.5f ? 1f : -1f;
        }
    }

    private void Update()
    {
        if (platformType == PlatformType.Moving && !isFalling)
        {
            UpdateMovingPlatform();
        }

        if (isFalling)
        {
            transform.position -= new Vector3(0, fallSpeed * Time.deltaTime, 0);
        }
    }

    private void UpdateMovingPlatform()
    {
        transform.position += new Vector3(moveDirection * moveSpeed * Time.deltaTime, 0, 0);

        if (transform.position.x >= rightBound)
        {
            transform.position = new Vector3(rightBound, transform.position.y, transform.position.z);
            moveDirection = -1f;
        }
        else if (transform.position.x <= leftBound)
        {
            transform.position = new Vector3(leftBound, transform.position.y, transform.position.z);
            moveDirection = 1f;
        }
    }

    /// <summary>
    /// Handles custom player bounce interaction.
    /// Returns true if custom jump logic was handled, false otherwise.
    /// </summary>
    public bool HandleBounce(PlayerMovement player)
    {
        if (player == null || isFalling) return false;

        switch (platformType)
        {
            case PlatformType.BouncyJelly:
                StartCoroutine(SquishRoutine());
                if (VFXManager.instance != null)
                {
                    VFXManager.instance.PlayJellySplash(transform.position + Vector3.up * 0.15f);
                }
                player.Jump(jumpMultiplier);
                return true;

            case PlatformType.Crumbling:
                if (!isCrumbling)
                {
                    StartCoroutine(CrumbleRoutine());
                }
                player.Jump(1.0f);
                return true;

            case PlatformType.Moving:
                player.Jump(1.0f);
                return true;

            default:
                return false;
        }
    }

    private IEnumerator SquishRoutine()
    {
        float elapsed = 0f;
        float halfDur = squishDuration * 0.5f;

        // Squish down
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            float targetY = Mathf.Lerp(originalScale.y, originalScale.y * squishScaleY, t);
            float targetX = Mathf.Lerp(originalScale.x, originalScale.x * 1.25f, t);
            transform.localScale = new Vector3(targetX, targetY, originalScale.z);
            yield return null;
        }

        elapsed = 0f;
        // Pop back up with rebound
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            float targetY = Mathf.Lerp(originalScale.y * squishScaleY, originalScale.y * 1.15f, t);
            float targetX = Mathf.Lerp(originalScale.x * 1.25f, originalScale.x * 0.95f, t);
            transform.localScale = new Vector3(targetX, targetY, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator CrumbleRoutine()
    {
        isCrumbling = true;
        if (VFXManager.instance != null)
        {
            VFXManager.instance.PlayCrumblePopup(transform.position + Vector3.up * 0.25f);
        }
        Vector3 basePos = transform.position;
        float elapsed = 0f;

        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        // Rapid shake & darkening warning
        while (elapsed < crumbleDelay)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / crumbleDelay;
            float currentShake = Mathf.Lerp(0.02f, shakeAmount, progress);
            float offsetX = Random.Range(-currentShake, currentShake);
            transform.position = new Vector3(basePos.x + offsetX, transform.position.y, transform.position.z);

            if (spriteRenderer != null)
            {
                // Darken slightly to signal cracking
                spriteRenderer.color = Color.Lerp(originalColor, originalColor * 0.7f, progress);
            }

            yield return null;
        }

        // Crack and fall!
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayCrumble();
        }
        if (VFXManager.instance != null)
        {
            VFXManager.instance.PlayCrumbleDust(transform.position);
        }

        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }

        isFalling = true;
        yield return new WaitForSeconds(1.2f);
        if (GroundManager.instance != null)
        {
            GroundManager.instance.DespawnPlatform(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

