using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private float speed;
    [SerializeField] private float deathY = -6f;
    [SerializeField] private float gracePeriodDuration = 1.5f;
    private Rigidbody2D rb;

    [SerializeField] private Transform pointer;

    private delegate void Dead();
    private Dead deadAction;
    private bool isDead = false;
    private bool isInvulnerable = false;

    [Header("Juice / Squash & Stretch")]
    [SerializeField] private float squashFactor = 1.25f;
    [SerializeField] private float stretchFactor = 1.25f;

    private Vector3 initialBaseScale;
    private Coroutine squashCoroutine;
    private int consecutiveBouncyJumps = 0;

    [Header("Game Start State")]
    [SerializeField] private bool autoStart = false;
    private bool hasGameStarted = false;
    private RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
    private float originalGravityScale = 1f;

    public bool HasGameStarted => hasGameStarted;

    private void Awake()
    {
        initialBaseScale = transform.localScale;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalBodyType = rb.bodyType;
            originalGravityScale = rb.gravityScale;
            if (!autoStart)
            {
                hasGameStarted = false;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                hasGameStarted = true;
            }
        }

        if (UIGameManager.instance != null)
        {
            deadAction += UIGameManager.instance.changePannel;
            if (!hasGameStarted)
            {
                UIGameManager.instance.ShowTapToStart();
            }
        }
        if (GameManager.instance != null)
        {
            deadAction += GameManager.instance.StopTime;
        }
    }

    /// <summary>
    /// Starts the game when the screen is tapped, launching Tartita into motion.
    /// </summary>
    public void StartGame()
    {
        if (hasGameStarted) return;
        hasGameStarted = true;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = originalBodyType;
            rb.gravityScale = originalGravityScale;
            rb.linearVelocity = Vector2.zero;
        }

        if (UIGameManager.instance != null)
        {
            UIGameManager.instance.HideTapToStart();
        }

        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.ResetScore(transform.position.y);
        }

        Jump();
    }

    public void Jump(float multiplier = 1f)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(0, 0);
        rb.AddForce(new Vector2(0, force * multiplier), ForceMode2D.Impulse);
        if (AudioManager.instance != null)
        {
            if (multiplier > 1.2f)
            {
                AudioManager.instance.PlaySpring();
            }
            else
            {
                AudioManager.instance.PlayJump();
            }
        }

        if (multiplier > 1.2f)
        {
            consecutiveBouncyJumps++;
        }
        else
        {
            consecutiveBouncyJumps = 0;
        }

        if (VFXManager.instance != null)
        {
            Vector3 contactPos = transform.position - new Vector3(0, 0.5f, 0);
            if (multiplier > 1.2f)
            {
                VFXManager.instance.PlayJellySplash(contactPos);
                VFXManager.instance.PlayBouncePopup(transform.position + Vector3.up * 0.65f, consecutiveBouncyJumps);
            }
            else
            {
                VFXManager.instance.PlayJumpPuff(contactPos);
            }
        }

        TriggerSquashAndStretch(multiplier);
    }

    private void TriggerSquashAndStretch(float multiplier)
    {
        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
        }
        squashCoroutine = StartCoroutine(SquashAndStretchRoutine(multiplier));
    }

    private IEnumerator SquashAndStretchRoutine(float multiplier)
    {
        if (initialBaseScale == Vector3.zero)
        {
            initialBaseScale = transform.localScale;
        }

        float squishIntensity = Mathf.Min(squashFactor * (multiplier > 1.2f ? 1.2f : 1.0f), 1.45f);
        float stretchIntensity = Mathf.Min(stretchFactor * (multiplier > 1.2f ? 1.25f : 1.0f), 1.5f);

        // Phase 1: Squash on impact (flat & wide)
        float p1Duration = 0.07f;
        float elapsed = 0f;
        Vector3 squashedScale = new Vector3(initialBaseScale.x * squishIntensity, initialBaseScale.y * (1f / squishIntensity), initialBaseScale.z);

        while (elapsed < p1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p1Duration;
            transform.localScale = Vector3.Lerp(initialBaseScale, squashedScale, t);
            yield return null;
        }

        // Phase 2: Stretch during launch (tall & skinny)
        float p2Duration = 0.11f;
        elapsed = 0f;
        Vector3 stretchedScale = new Vector3(initialBaseScale.x * (1f / stretchIntensity), initialBaseScale.y * stretchIntensity, initialBaseScale.z);

        while (elapsed < p2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p2Duration;
            transform.localScale = Vector3.Lerp(squashedScale, stretchedScale, t);
            yield return null;
        }

        // Phase 3: Smooth rebound back to normal scale
        float p3Duration = 0.10f;
        elapsed = 0f;
        while (elapsed < p3Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p3Duration;
            transform.localScale = Vector3.Lerp(stretchedScale, initialBaseScale, t);
            yield return null;
        }

        transform.localScale = initialBaseScale;
        squashCoroutine = null;
    }

    public void Die()
    {
        if (isDead || isInvulnerable) return;
        isDead = true;
        consecutiveBouncyJumps = 0;

        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
            squashCoroutine = null;
        }
        if (initialBaseScale != Vector3.zero)
        {
            transform.localScale = initialBaseScale;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayGameOver();
        }

        if (UIGameManager.instance != null)
        {
            UIGameManager.instance.changePannel();
        }
        if (GameManager.instance != null)
        {
            GameManager.instance.StopTime();
        }

        deadAction?.Invoke();
    }

    public void Revive(Vector3 safePosition)
    {
        isDead = false;
        consecutiveBouncyJumps = 0;
        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
            squashCoroutine = null;
        }
        if (initialBaseScale != Vector3.zero)
        {
            transform.localScale = initialBaseScale;
        }
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        transform.position = safePosition;
        if (CameraController.instance != null)
        {
            CameraController.instance.ResetCamera(Mathf.Max(safePosition.y + 1.2f, CameraController.instance.HighestCameraY));
        }
        Jump();
        StartCoroutine(ReviveGraceRoutine());
    }

    private IEnumerator ReviveGraceRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSecondsRealtime(gracePeriodDuration);
        isInvulnerable = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInvulnerable && other.CompareTag("Death"))
        {
            Die();
        }
    }

    private void Update()
    {
        if (!hasGameStarted)
        {
            return;
        }

        float currentDeathY = Camera.main != null
            ? Camera.main.transform.position.y - Camera.main.orthographicSize - 0.8f
            : deathY;

        if (!isInvulnerable && transform.position.y < currentDeathY)
        {
            Die();
            return;
        }

        if (pointer != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(pointer.position.x, transform.position.y), speed * Time.deltaTime);
        }
    }
}
