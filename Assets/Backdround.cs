using UnityEngine;

public class Backdround : MonoBehaviour
{
    [SerializeField] private GameObject BackGround1;
    [SerializeField] private GameObject BackGround2;
    [SerializeField] private GameObject BackGround3;

    [Header("Background Cycle Sprites")]
    [Tooltip("Sprite for the starting ground tile (Bakery House).")]
    [SerializeField] private Sprite startBackgroundSprite;
    [Tooltip("Seamless vertical loop sprite for upper sky tiles.")]
    [SerializeField] private Sprite loopBackgroundSprite;

    [Tooltip("Tile height used for seamless vertical looping.")]
    [SerializeField] private float tileHeight = 10.032f;

    [Tooltip("Minimum horizontal world width guaranteed to be covered.")]
    [SerializeField] private float minCoverageWidth = 8.5f;

    private GameObject[] BackGrounds;
    private SpriteRenderer[] backgroundRenderers;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (transform.childCount >= 3)
        {
            BackGround1 = transform.GetChild(0).gameObject;
            BackGround2 = transform.GetChild(1).gameObject;
            BackGround3 = transform.GetChild(2).gameObject;
        }

        BackGrounds = new GameObject[] { BackGround1, BackGround2, BackGround3 };
        backgroundRenderers = new SpriteRenderer[BackGrounds.Length];

        for (int i = 0; i < BackGrounds.Length; i++)
        {
            if (BackGrounds[i] != null)
            {
                backgroundRenderers[i] = BackGrounds[i].GetComponent<SpriteRenderer>();
            }
        }

        // Ensure first tile uses starting ground sprite and upper tiles use seamless sky loop sprite
        if (backgroundRenderers[0] != null && startBackgroundSprite != null)
        {
            backgroundRenderers[0].sprite = startBackgroundSprite;
        }

        for (int i = 1; i < backgroundRenderers.Length; i++)
        {
            if (backgroundRenderers[i] != null && loopBackgroundSprite != null)
            {
                backgroundRenderers[i].sprite = loopBackgroundSprite;
            }
        }

        AdjustBackgroundWidth();
    }

    private void AdjustBackgroundWidth()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Calculate maximum camera view width (including max jump dynamic zoom)
        float maxOrtho = 5.5f;
        float visibleCameraWidth = maxOrtho * 2f * mainCamera.aspect;
        // Apply at least 25% safety overscan so edges are never visible even on wide viewports or shakes
        float requiredWidth = Mathf.Max(visibleCameraWidth * 1.25f, minCoverageWidth);

        for (int i = 0; i < BackGrounds.Length; i++)
        {
            if (BackGrounds[i] == null) continue;

            SpriteRenderer sr = backgroundRenderers[i];
            float spriteWidth = (sr != null && sr.sprite != null) ? sr.sprite.rect.width / sr.sprite.pixelsPerUnit : 7.68f;
            if (spriteWidth <= 0f) spriteWidth = 7.68f;

            float newScaleX = requiredWidth / spriteWidth;
            Vector3 currentScale = BackGrounds[i].transform.localScale;
            BackGrounds[i].transform.localScale = new Vector3(newScaleX, currentScale.y, currentScale.z);

            // Center tile in world space
            Vector3 worldPos = BackGrounds[i].transform.position;
            BackGrounds[i].transform.position = new Vector3(0f, worldPos.y, worldPos.z);
        }
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        float camY = mainCamera.transform.position.y;
        float lowerWrapThreshold = camY - (tileHeight * 1.15f);

        // Check if any tile is below the camera viewport and wrap it to the top
        for (int i = 0; i < BackGrounds.Length; i++)
        {
            if (BackGrounds[i] != null && BackGrounds[i].transform.position.y < lowerWrapThreshold)
            {
                float highestY = float.MinValue;
                for (int j = 0; j < BackGrounds.Length; j++)
                {
                    if (BackGrounds[j] != null && BackGrounds[j].transform.position.y > highestY)
                    {
                        highestY = BackGrounds[j].transform.position.y;
                    }
                }

                // Switch wrapped tile (e.g., BackGround1) to the seamless sky loop sprite
                if (loopBackgroundSprite != null && backgroundRenderers[i] != null && backgroundRenderers[i].sprite != loopBackgroundSprite)
                {
                    backgroundRenderers[i].sprite = loopBackgroundSprite;
                }

                Vector3 currentPos = BackGrounds[i].transform.position;
                BackGrounds[i].transform.position = new Vector3(0f, highestY + tileHeight, currentPos.z);
            }
        }
    }
}
