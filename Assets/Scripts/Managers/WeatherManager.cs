using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager instance;

    [Header("Particle Materials")]
    [SerializeField] private Material snowflakeMaterial;
    [SerializeField] private Material fogCloudMaterial;

    [Header("Snow / Powdered Sugar Settings")]
    [Tooltip("Score at which snow begins to fall.")]
    [SerializeField] private float snowStartScore = 200f;
    [Tooltip("Maximum snowflake emission rate at peak altitude.")]
    [SerializeField] private float maxSnowEmission = 75f;

    [Header("Fog & Mist Settings")]
    [Tooltip("Score at which rolling mist clouds begin to appear.")]
    [SerializeField] private float fogStartScore = 650f;
    [Tooltip("Maximum fog cloud emission rate at peak altitude.")]
    [SerializeField] private float maxFogEmission = 5f;

    [Header("Frosted Sky Overlay")]
    [Range(0f, 0.4f)] [SerializeField] private float maxFrostAlpha = 0.20f;

    private Camera mainCamera;
    private ParticleSystem snowParticles;
    private ParticleSystem fogParticles;
    private SpriteRenderer frostOverlayRenderer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        SetupWeatherSystems();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Lock weather position to camera center
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0f);

        UpdateWeatherAtmosphere();
    }

    private void SetupWeatherSystems()
    {
        // 1. Setup Snow Particle System
        GameObject snowGO = new GameObject("SnowParticles");
        snowGO.transform.SetParent(transform, false);
        snowGO.transform.localPosition = new Vector3(0f, 6.5f, -1f);

        snowParticles = snowGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer snowRend = snowGO.GetComponent<ParticleSystemRenderer>();
        if (snowflakeMaterial != null)
        {
            EnsureValidMaterialShader(snowflakeMaterial);
            snowRend.sharedMaterial = snowflakeMaterial;
        }
        snowRend.sortingOrder = 5;

        var snowMain = snowParticles.main;
        snowMain.loop = true;
        snowMain.playOnAwake = true;
        snowMain.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
        snowMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        snowMain.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
        snowMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        snowMain.startColor = new Color(0.95f, 0.98f, 1f, 0.85f);
        snowMain.simulationSpace = ParticleSystemSimulationSpace.World;
        snowMain.gravityModifier = 0.05f;

        var snowEmission = snowParticles.emission;
        snowEmission.rateOverTime = 0f;

        var snowShape = snowParticles.shape;
        snowShape.shapeType = ParticleSystemShapeType.Box;
        snowShape.scale = new Vector3(7.5f, 0.5f, 1f);

        var snowVel = snowParticles.velocityOverLifetime;
        snowVel.enabled = true;
        snowVel.space = ParticleSystemSimulationSpace.World;
        snowVel.y = new ParticleSystem.MinMaxCurve(-1.8f, -2.6f);
        snowVel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

        var snowRot = snowParticles.rotationOverLifetime;
        snowRot.enabled = true;
        snowRot.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        snowParticles.Play();

        // 2. Setup Fog / Mist Particle System
        GameObject fogGO = new GameObject("FogParticles");
        fogGO.transform.SetParent(transform, false);
        fogGO.transform.localPosition = new Vector3(0f, 0f, 1.5f);

        fogParticles = fogGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer fogRend = fogGO.GetComponent<ParticleSystemRenderer>();
        if (fogCloudMaterial != null)
        {
            EnsureValidMaterialShader(fogCloudMaterial);
            fogRend.sharedMaterial = fogCloudMaterial;
        }
        fogRend.sortingOrder = -2;

        var fogMain = fogParticles.main;
        fogMain.loop = true;
        fogMain.playOnAwake = true;
        fogMain.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
        fogMain.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        fogMain.startSize = new ParticleSystem.MinMaxCurve(3.0f, 5.0f);
        fogMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        fogMain.startColor = new Color(0.92f, 0.96f, 1f, 0.16f);
        fogMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var fogEmission = fogParticles.emission;
        fogEmission.rateOverTime = 0f;

        var fogShape = fogParticles.shape;
        fogShape.shapeType = ParticleSystemShapeType.Box;
        fogShape.scale = new Vector3(8f, 10f, 1f);

        var fogVel = fogParticles.velocityOverLifetime;
        fogVel.enabled = true;
        fogVel.space = ParticleSystemSimulationSpace.World;
        fogVel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);

        var fogRot = fogParticles.rotationOverLifetime;
        fogRot.enabled = true;
        fogRot.z = new ParticleSystem.MinMaxCurve(-8f * Mathf.Deg2Rad, 8f * Mathf.Deg2Rad);

        fogParticles.Play();

        // 3. Setup Frosted Sky Vignette Overlay
        GameObject frostGO = new GameObject("FrostOverlay");
        frostGO.transform.SetParent(transform, false);
        frostGO.transform.localPosition = new Vector3(0f, 0f, 2f);
        frostGO.transform.localScale = new Vector3(12f, 20f, 1f);

        frostOverlayRenderer = frostGO.AddComponent<SpriteRenderer>();
        frostOverlayRenderer.sortingOrder = -1;
        // Subtle icy cyan-white frost tint
        frostOverlayRenderer.color = new Color(0.85f, 0.94f, 1f, 0f);
        // Create 1x1 white texture for clean unlit overlay tint
        Texture2D whiteTex = Texture2D.whiteTexture;
        frostOverlayRenderer.sprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f), 1f);
    }

    private void EnsureValidMaterialShader(Material mat)
    {
        if (mat == null) return;
        if (mat.shader == null || mat.shader.name.Contains("InternalErrorShader") || mat.shader.name == "Hidden/InternalErrorShader")
        {
            Shader fallback = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            if (fallback != null)
            {
                mat.shader = fallback;
            }
        }
    }

    private void UpdateWeatherAtmosphere()
    {
        float score = ScoreManager.instance != null ? ScoreManager.instance.CurrentScore : 0f;

        // Snow emission scaling
        if (snowParticles != null)
        {
            float snowT = Mathf.Clamp01((score - snowStartScore) / 1200f);
            var emission = snowParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, maxSnowEmission, snowT);
        }

        // Fog emission scaling
        if (fogParticles != null)
        {
            float fogT = Mathf.Clamp01((score - fogStartScore) / 1000f);
            var emission = fogParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, maxFogEmission, fogT);
        }

        // Frosted overlay alpha scaling
        if (frostOverlayRenderer != null)
        {
            float frostT = Mathf.Clamp01(score / 1500f);
            float targetAlpha = Mathf.Lerp(0f, maxFrostAlpha, frostT);
            frostOverlayRenderer.color = new Color(0.85f, 0.94f, 1f, targetAlpha);
        }
    }
}

