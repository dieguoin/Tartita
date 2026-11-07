using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a soft confectionery golden aura and subtle twinkling sparkle effect
/// behind the character in the Main Menu, making it pop off the background.
/// </summary>
[DisallowMultipleComponent]
public class CharacterShineEffect : MonoBehaviour
{
    [Header("Aura Glow")]
    [SerializeField] private Sprite auraSprite;
    [SerializeField] private Color auraColor = new Color(1f, 0.95f, 0.78f, 0.38f);
    [SerializeField] private float baseScaleMultiplier = 1.22f;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseIntensity = 0.06f;

    [Header("Sparkle Settings")]
    [SerializeField] private bool enableSparkles = true;
    [SerializeField] private Material starMaterial;

    private RectTransform auraRect;
    private Image auraImage;
    private CanvasGroup auraGroup;
    private ParticleSystem sparkleParticles;

    private void Awake()
    {
        SetupAura();
        if (enableSparkles)
        {
            SetupSparkles();
        }
    }

    private void SetupAura()
    {
        // Check if Aura already exists
        Transform existing = transform.Find("AuraGlow");
        GameObject auraGO;
        if (existing != null)
        {
            auraGO = existing.gameObject;
        }
        else
        {
            auraGO = new GameObject("AuraGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            auraGO.transform.SetParent(transform, false);
            // Place behind parent graphics
            auraGO.transform.SetAsFirstSibling();
        }

        auraRect = auraGO.GetComponent<RectTransform>();
        auraRect.anchorMin = new Vector2(0.5f, 0.5f);
        auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot = new Vector2(0.5f, 0.5f);
        auraRect.anchoredPosition = Vector2.zero;

        RectTransform parentRect = GetComponent<RectTransform>();
        Vector2 parentSize = parentRect != null ? parentRect.rect.size : new Vector2(300f, 300f);
        if (parentSize.x <= 10f || parentSize.y <= 10f) parentSize = new Vector2(300f, 300f);
        auraRect.sizeDelta = parentSize * baseScaleMultiplier;

        auraImage = auraGO.GetComponent<Image>();
        auraImage.raycastTarget = false;
        auraImage.color = auraColor;

        if (auraSprite == null)
        {
            auraSprite = Resources.Load<Sprite>("Ui/Aura_Glow");
        }
#if UNITY_EDITOR
        if (auraSprite == null)
        {
            auraSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Ui/Aura_Glow.png");
            if (auraSprite == null)
            {
                auraSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Aura_Glow.png");
            }
        }
#endif
        if (auraSprite != null)
        {
            auraImage.sprite = auraSprite;
            auraImage.enabled = true;
        }
        else
        {
            // Failsafe: Never render a solid white quad if sprite is missing
            auraImage.enabled = false;
        }

        auraGroup = auraGO.GetComponent<CanvasGroup>();
        if (auraGroup == null) auraGroup = auraGO.AddComponent<CanvasGroup>();
    }

    private void SetupSparkles()
    {
        Transform existing = transform.Find("ShineSparkles");
        if (existing != null) return;

        GameObject sparkleGO = new GameObject("ShineSparkles", typeof(RectTransform), typeof(ParticleSystem));
        sparkleGO.transform.SetParent(transform, false);
        sparkleGO.transform.SetAsLastSibling();

        RectTransform sRt = sparkleGO.GetComponent<RectTransform>();
        sRt.anchoredPosition = Vector2.zero;

        sparkleParticles = sparkleGO.GetComponent<ParticleSystem>();
        var main = sparkleParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 35f);
        main.startSize = new ParticleSystem.MinMaxCurve(18f, 32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.98f, 0.85f, 0.9f), new Color(1f, 0.88f, 0.45f, 0.9f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles = 8;

        var emission = sparkleParticles.emission;
        emission.rateOverTime = 2.5f;

        var shape = sparkleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 110f;

        var sizeOverLifetime = sparkleParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.4f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = sparkleGO.GetComponent<ParticleSystemRenderer>();
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
        renderer.sortingOrder = 10;
    }

    private void Update()
    {
        if (auraRect == null || auraGroup == null) return;

        float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseIntensity;
        auraRect.localScale = Vector3.one * (1f + pulse);
        auraGroup.alpha = Mathf.Clamp01(1f + (pulse * 1.5f));
    }
}

