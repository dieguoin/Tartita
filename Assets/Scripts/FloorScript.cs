using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorScript : MonoBehaviour
{
    [Tooltip("Distance below the camera center after which this platform is despawned.")]
    [SerializeField] private float despawnDistanceBelowCamera = 7.5f;

    private Camera mainCamera;
    private Vector3 originalBaseScale = Vector3.zero;

    private void Awake()
    {
        originalBaseScale = transform.localScale;
    }

    /// <summary>
    /// Resets the platform state when recycled from the pool.
    /// </summary>
    public void ResetPlatform()
    {
        if (originalBaseScale != Vector3.zero)
        {
            transform.localScale = originalBaseScale;
        }
    }

    /// <summary>
    /// Scales platform width proportionally with the difficulty progression.
    /// </summary>
    public void SetWidthScale(float widthScaleMultiplier)
    {
        if (originalBaseScale == Vector3.zero)
        {
            originalBaseScale = transform.localScale;
        }
        transform.localScale = new Vector3(originalBaseScale.x * widthScaleMultiplier, originalBaseScale.y, originalBaseScale.z);
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Clean up platform once it is safely below the visible camera view
        float despawnThreshold = mainCamera.transform.position.y - despawnDistanceBelowCamera;
        if (transform.position.y < despawnThreshold)
        {
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Feet"))
        {
            Rigidbody2D parentRb = collision.GetComponentInParent<Rigidbody2D>();
            if (parentRb != null && parentRb.linearVelocity.y <= 0)
            {
                PlayerMovement player = collision.GetComponentInParent<PlayerMovement>();
                if (player != null)
                {
                    SpecialPlatform special = GetComponent<SpecialPlatform>();
                    if (special != null && special.HandleBounce(player))
                    {
                        return;
                    }
                    player.Jump();
                }
            }
        }
    }
}
