using TMPro;
using UnityEngine;

public class FloatingPopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float floatSpeed = 1.5f;

    private float elapsed = 0f;
    private Vector3 baseScale = Vector3.one;
    private Vector3 velocity;
    private Color topColor = Color.white;
    private Color bottomColor = Color.white;
    private bool isInitialized = false;

    private void Awake()
    {
        EnsureTextMesh();
    }

    private void EnsureTextMesh()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                textMesh = gameObject.AddComponent<TextMeshPro>();
            }
        }

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(8f, 2f);
        }

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.fontSize = 3.8f;
        textMesh.overflowMode = TextOverflowModes.Overflow;

        Material outlineMat = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
        if (outlineMat != null)
        {
            textMesh.fontSharedMaterial = outlineMat;
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 25;
        }
    }

    public void Setup(string message, Color topGradColor, Color bottomGradColor, float scaleMultiplier = 1f)
    {
        EnsureTextMesh();

        textMesh.text = message;
        topColor = topGradColor;
        bottomColor = bottomGradColor;

        textMesh.color = Color.white;
        textMesh.enableVertexGradient = true;
        textMesh.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);

        baseScale = Vector3.one * scaleMultiplier;
        transform.localScale = baseScale * 0.2f;

        // Slight random horizontal drift and playful tilt
        float driftX = Random.Range(-0.35f, 0.35f);
        velocity = new Vector3(driftX, floatSpeed, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));

        elapsed = 0f;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / lifetime);

        // 1. Upward drift with gentle deceleration
        float currentSpeedMultiplier = Mathf.Lerp(1.15f, 0.4f, progress);
        transform.position += velocity * (currentSpeedMultiplier * Time.deltaTime);

        // 2. Elastic scale pop-in then slight shrink at end
        float scaleFactor;
        if (progress < 0.12f)
        {
            float t = progress / 0.12f;
            scaleFactor = Mathf.Lerp(0.2f, 1.28f, t);
        }
        else if (progress < 0.24f)
        {
            float t = (progress - 0.12f) / 0.12f;
            scaleFactor = Mathf.Lerp(1.28f, 1.0f, t);
        }
        else if (progress > 0.65f)
        {
            float t = (progress - 0.65f) / 0.35f;
            scaleFactor = Mathf.Lerp(1.0f, 0.8f, t);
        }
        else
        {
            scaleFactor = 1.0f;
        }
        transform.localScale = baseScale * scaleFactor;

        // 3. Smooth alpha fade-out in the last 40% of lifetime
        if (textMesh != null)
        {
            float alpha = progress < 0.55f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.55f) / 0.45f);
            Color cTop = new Color(topColor.r, topColor.g, topColor.b, alpha);
            Color cBot = new Color(bottomColor.r, bottomColor.g, bottomColor.b, alpha);
            textMesh.colorGradient = new VertexGradient(cTop, cTop, cBot, cBot);
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
