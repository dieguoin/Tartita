using UnityEngine;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VFXManager>();
            }
            return _instance;
        }
    }

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject coinSparklePrefab;
    [SerializeField] private GameObject jumpPuffPrefab;
    [SerializeField] private GameObject jellySplashPrefab;
    [SerializeField] private GameObject crumbleDustPrefab;

    [Header("Floating Popup Prefab")]
    [SerializeField] private GameObject floatingPopupPrefab;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void PlayCoinSparkle(Vector3 position)
    {
        SpawnVFX(coinSparklePrefab, position, 1.2f);
    }

    public void PlayJumpPuff(Vector3 position)
    {
        SpawnVFX(jumpPuffPrefab, position, 0.8f);
    }

    public void PlayJellySplash(Vector3 position)
    {
        SpawnVFX(jellySplashPrefab, position, 1.0f);
    }

    public void PlayCrumbleDust(Vector3 position)
    {
        SpawnVFX(crumbleDustPrefab, position, 1.2f);
    }

    public void PlayCoinPopup(Vector3 position)
    {
        Color topGold = new Color(1f, 0.91f, 0.32f, 1f);
        Color botAmber = new Color(1f, 0.62f, 0.11f, 1f);
        SpawnFloatingText("+1 COIN!", position, topGold, botAmber, 1.0f);
    }

    public void PlayBouncePopup(Vector3 position, int comboCount = 1)
    {
        if (comboCount >= 2)
        {
            Color topMint = new Color(0.49f, 1f, 0.9f, 1f);
            Color botSky = new Color(0.22f, 0.71f, 1f, 1f);
            float scale = Mathf.Min(1.15f + (comboCount - 1) * 0.1f, 1.5f);
            SpawnFloatingText($"SWEET COMBO x{comboCount}!", position, topMint, botSky, scale);
        }
        else
        {
            Color topPink = new Color(1f, 0.49f, 0.7f, 1f);
            Color botBerry = new Color(1f, 0.18f, 0.49f, 1f);
            SpawnFloatingText("MEGA BOUNCE!", position, topPink, botBerry, 1.15f);
        }
    }

    public void PlayCrumblePopup(Vector3 position)
    {
        Color topCaramel = new Color(1f, 0.82f, 0.58f, 1f);
        Color botBrown = new Color(0.78f, 0.48f, 0.24f, 1f);
        SpawnFloatingText("CRUNCH!", position, topCaramel, botBrown, 0.9f);
    }

    public void PlayMilestonePopup(Vector3 position, int milestoneScore)
    {
        Color topLavender = new Color(0.95f, 0.78f, 1f, 1f);
        Color botOrchid = new Color(0.72f, 0.42f, 1f, 1f);
        SpawnFloatingText($"{milestoneScore} PTS!", position, topLavender, botOrchid, 1.2f);
    }

    public void PlayNewBestPopup(Vector3 position)
    {
        Color topBrightGold = new Color(1f, 0.96f, 0.49f, 1f);
        Color botOrangeGold = new Color(1f, 0.53f, 0f, 1f);
        SpawnFloatingText("NEW BEST!", position, topBrightGold, botOrangeGold, 1.35f);
    }

    public void SpawnFloatingText(string message, Vector3 position, Color topColor, Color bottomColor, float scaleMultiplier = 1f)
    {
        GameObject popupGO;
        if (floatingPopupPrefab != null)
        {
            popupGO = Instantiate(floatingPopupPrefab, position, Quaternion.identity);
        }
        else
        {
            popupGO = new GameObject("VFX_FloatingPopup");
            popupGO.transform.position = position;
        }

        FloatingPopup popup = popupGO.GetComponent<FloatingPopup>();
        if (popup == null)
        {
            popup = popupGO.AddComponent<FloatingPopup>();
        }

        popup.Setup(message, topColor, bottomColor, scaleMultiplier);
    }

    private void SpawnVFX(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;

        GameObject vfxInstance = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            float dur = ps.main.duration + ps.main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, dur);
        }

        Destroy(vfxInstance, lifetime);
    }
}
