using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls the screen pointer that tracks touch/mouse input,
/// coordinates the initial Tap-to-Start game launch, and presents
/// an interactive touch reticle effect following the finger while pressed.
/// </summary>
public class PointerScript : MonoBehaviour
{
    [Header("Game Start Coordination")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Touch Reticle Visuals")]
    [SerializeField] private bool showTouchEffect = true;
    [SerializeField] private Color reticleColor = new Color(1f, 0.86f, 0.36f, 0.90f); // Warm honey amber glow
    [SerializeField] private Color accentRingColor = new Color(1f, 0.60f, 0.20f, 0.75f);
    [SerializeField] private float baseScale = 0.95f;
    [SerializeField] private float pulseFrequency = 6.0f;
    [SerializeField] private float pulseAmplitude = 0.08f;

    [Header("Trail Particles")]
    [SerializeField] private bool enableSparkles = true;
    [SerializeField] private Material starMaterial;

    private bool move = false;
    private Camera mainCamera;

    // Visual effect hierarchy
    private GameObject visualRoot;
    private SpriteRenderer mainRingRenderer;
    private SpriteRenderer outerAuraRenderer;
    private ParticleSystem sparkleParticles;
    private Coroutine transitionCoroutine;
    private static Sprite cachedRingSprite;
    private static Sprite cachedAuraSprite;

    private void Awake()
    {
        mainCamera = Camera.main;
        SetupVisualComponents();
    }

    private void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        // Hide touch reticle initially until the player presses the screen
        SetVisualsActive(false);
    }

    private void SetupVisualComponents()
    {
        if (!showTouchEffect) return;

        // Create child container for touch visuals so transform offset/scale animations stay isolated
        visualRoot = new GameObject("TouchReticleEffect");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.zero;

        if (cachedRingSprite == null)
        {
            cachedRingSprite = GenerateSmoothRingSprite(128, 0.44f, 0.30f, 0.20f);
        }
        if (cachedAuraSprite == null)
        {
            cachedAuraSprite = GenerateSoftGlowSprite(128);
        }

        // 1. Soft glowing aura behind the ring
        GameObject auraGO = new GameObject("Aura");
        auraGO.transform.SetParent(visualRoot.transform, false);
        outerAuraRenderer = auraGO.AddComponent<SpriteRenderer>();
        outerAuraRenderer.sprite = cachedAuraSprite;
        outerAuraRenderer.color = new Color(accentRingColor.r, accentRingColor.g, accentRingColor.b, 0.45f);
        outerAuraRenderer.sortingOrder = 90;
        auraGO.transform.localScale = Vector3.one * 1.35f;

        // 2. Crisp main honey ring
        GameObject ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(visualRoot.transform, false);
        mainRingRenderer = ringGO.AddComponent<SpriteRenderer>();
        mainRingRenderer.sprite = cachedRingSprite;
        mainRingRenderer.color = reticleColor;
        mainRingRenderer.sortingOrder = 95;
        ringGO.transform.localScale = Vector3.one;

        // 3. Subtle pastry star sparkles when dragging
        if (enableSparkles)
        {
            SetupSparkleParticles();
        }

        visualRoot.transform.localScale = Vector3.zero;
        visualRoot.SetActive(false);
    }

    private void SetupSparkleParticles()
    {
        GameObject psGO = new GameObject("TouchSparkles");
        psGO.transform.SetParent(visualRoot.transform, false);
        psGO.transform.localPosition = Vector3.zero;

        sparkleParticles = psGO.AddComponent<ParticleSystem>();
        var main = sparkleParticles.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.60f, 0.95f),
            new Color(1f, 0.75f, 0.25f, 0.85f)
        );
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.playOnAwake = false;

        var emission = sparkleParticles.emission;
        emission.rateOverTime = 10f;
        emission.rateOverDistance = 4f;

        var shape = sparkleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var sol = sparkleParticles.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.2f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var col = sparkleParticles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var renderer = psGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 96;

        if (starMaterial == null)
        {
            starMaterial = Resources.Load<Material>("Materials/Mat_VFX_Star");
        }
#if UNITY_EDITOR
        if (starMaterial == null)
        {
            starMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Mat_VFX_Star.mat");
            if (starMaterial == null)
            {
                starMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_VFX_Star.mat");
            }
        }
#endif
        if (starMaterial != null)
        {
            renderer.material = starMaterial;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (move)
            {
                move = false;
                HideEffect(instant: true);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
            {
                move = true;

                // 1. Position immediately before showing
                UpdatePointerPosition();

                // 2. Launch Tartita on first screen touch
                if (playerMovement == null)
                {
                    playerMovement = FindFirstObjectByType<PlayerMovement>();
                }
                if (playerMovement != null && !playerMovement.HasGameStarted)
                {
                    playerMovement.StartGame();
                }

                // 3. Display glowing touch reticle
                ShowEffect();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (move)
            {
                move = false;
                HideEffect(instant: false);
            }
        }

        if (move)
        {
            UpdatePointerPosition();
            AnimateHeldPulse();
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdatePointerPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector2 mousePosition = Input.mousePosition;
        Vector3 screenPos = new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        worldPosition.z = 0f;
        transform.position = worldPosition;
    }

    private void AnimateHeldPulse()
    {
        if (visualRoot == null || !visualRoot.activeSelf) return;

        // Gentle breathing pulse while finger remains held down
        float pulse = 1f + Mathf.Sin(Time.time * pulseFrequency) * pulseAmplitude;
        visualRoot.transform.localScale = Vector3.one * (baseScale * pulse);

        // Subtle slow rotation of outer aura for continuous alive feeling
        if (outerAuraRenderer != null)
        {
            outerAuraRenderer.transform.Rotate(0f, 0f, -35f * Time.deltaTime);
        }
    }

    private void ShowEffect()
    {
        if (visualRoot == null) return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        visualRoot.SetActive(true);
        if (sparkleParticles != null)
        {
            sparkleParticles.Play();
        }
        transitionCoroutine = StartCoroutine(PopInRoutine());
    }

    private void HideEffect(bool instant)
    {
        if (visualRoot == null) return;

        if (sparkleParticles != null)
        {
            sparkleParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        if (instant)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            visualRoot.transform.localScale = Vector3.zero;
            visualRoot.SetActive(false);
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private void SetVisualsActive(bool active)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(active);
            if (!active) visualRoot.transform.localScale = Vector3.zero;
        }
    }

    private IEnumerator PopInRoutine()
    {
        float duration = 0.14f;
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * (baseScale * 1.18f); // Slight elastic pop overshoot

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Elastic pop curve
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);
            visualRoot.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, scaleT);
            yield return null;
        }

        // Settle smoothly to base scale
        float settleDur = 0.08f;
        elapsed = 0f;
        while (elapsed < settleDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDur);
            visualRoot.transform.localScale = Vector3.Lerp(targetScale, Vector3.one * baseScale, t);
            yield return null;
        }

        visualRoot.transform.localScale = Vector3.one * baseScale;
        transitionCoroutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        Vector3 startScale = visualRoot.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            visualRoot.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            yield return null;
        }

        visualRoot.transform.localScale = Vector3.zero;
        visualRoot.SetActive(false);
        transitionCoroutine = null;
    }

    /// <summary>
    /// Generates a smooth, anti-aliased doughnut ring sprite with soft edges.
    /// </summary>
    private static Sprite GenerateSmoothRingSprite(int size, float outerRadiusNorm, float innerRadiusNorm, float centralAuraNorm)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size * 0.5f;
        float outerR = size * outerRadiusNorm;
        float innerR = size * innerRadiusNorm;
        float ringCenter = (outerR + innerR) * 0.5f;
        float ringHalfThick = (outerR - innerR) * 0.5f;
        float centralAuraR = size * centralAuraNorm;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float alpha = 0f;

                if (dist <= outerR + 4f && dist >= innerR - 4f)
                {
                    float distFromRing = Mathf.Abs(dist - ringCenter);
                    alpha = Mathf.Clamp01(1f - (distFromRing / ringHalfThick));
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                }

                // Delicate soft aura in the ring center
                if (dist < centralAuraR)
                {
                    float auraAlpha = Mathf.Clamp01(1f - (dist / centralAuraR)) * 0.22f;
                    alpha = Mathf.Max(alpha, auraAlpha);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Generates a soft radial glow sprite for outer aura backlighting.
    /// </summary>
    private static Sprite GenerateSoftGlowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size * 0.5f;
        float radius = size * 0.48f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float t = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - t, 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
