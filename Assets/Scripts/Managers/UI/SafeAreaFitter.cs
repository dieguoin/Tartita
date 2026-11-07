using UnityEngine;

/// <summary>
/// Adjusts a RectTransform's anchors to fit within the device's Screen.safeArea,
/// protecting UI elements from notches, punch-hole cameras, and system status/notification bars.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Conformity Settings")]
    [Tooltip("Apply safe area inset to the top (notches, hole-punch cameras, notification bar).")]
    [SerializeField] private bool conformTop = true;

    [Tooltip("Apply safe area inset to the bottom (home indicators, navigation bar).")]
    [SerializeField] private bool conformBottom = true;

    [Tooltip("Apply safe area inset to the left edge.")]
    [SerializeField] private bool conformLeft = true;

    [Tooltip("Apply safe area inset to the right edge.")]
    [SerializeField] private bool conformRight = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        // Re-apply if resolution, orientation, or safe area values change at runtime
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    public void SetConformAxes(bool top, bool bottom, bool left, bool right)
    {
        conformTop = top;
        conformBottom = bottom;
        conformLeft = left;
        conformRight = right;
        ApplySafeArea();
    }

    /// <summary>
    /// Computes and applies normalized anchors from Screen.safeArea.
    /// </summary>
    public void ApplySafeArea()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }

        Rect safeArea = Screen.safeArea;
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (screenWidth <= 0 || screenHeight <= 0) return;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(screenWidth, screenHeight);
        lastOrientation = Screen.orientation;

        // Convert safeArea pixel coordinates to 0..1 normalized anchors
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenWidth;
        anchorMin.y /= screenHeight;
        anchorMax.x /= screenWidth;
        anchorMax.y /= screenHeight;

        // Respect configuration flags
        if (!conformLeft) anchorMin.x = 0f;
        if (!conformBottom) anchorMin.y = 0f;
        if (!conformRight) anchorMax.x = 1f;
        if (!conformTop) anchorMax.y = 1f;

        // Clamp to valid range
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

