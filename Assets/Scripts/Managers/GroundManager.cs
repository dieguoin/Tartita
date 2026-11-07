using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundManager : MonoBehaviour
{
    public static GroundManager instance;

    [Header("Spawn Settings")]
#pragma warning disable 0414
    [SerializeField] private float height = 5f;
#pragma warning restore 0414
    [SerializeField] private float coinDistance = 0.5f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private Transform leftLimit;
    [SerializeField] private Transform rightLimit;

    [Header("Special Platform Prefabs")]
    [SerializeField] private GameObject bouncyPrefab;
    [SerializeField] private GameObject movingPrefab;
    [SerializeField] private GameObject crumblingPrefab;

    [Header("Dynamic Difficulty Scaling")]
    [Tooltip("Score at which difficulty progression reaches 100% (Master Tier).")]
    [SerializeField] private float maxDifficultyScore = 1500f;

    [Header("Vertical Gap Progression")]
    [Tooltip("Minimum vertical gap at score 0.")]
    [SerializeField] private float minGapStart = 1.7f;
    [Tooltip("Minimum vertical gap at max difficulty.")]
    [SerializeField] private float minGapEnd = 2.8f;
    [Tooltip("Maximum vertical gap at score 0.")]
    [SerializeField] private float maxGapStart = 2.2f;
    [Tooltip("Maximum vertical gap at max difficulty.")]
    [SerializeField] private float maxGapEnd = 3.4f;

    [Header("Platform Width Progression")]
    [Tooltip("Platform width scale multiplier at score 0 (1.0 = standard width).")]
    [Range(0.5f, 1.2f)] [SerializeField] private float platformWidthStart = 1.0f;
    [Tooltip("Platform width scale multiplier at max difficulty (0.70 = 70% width).")]
    [Range(0.4f, 1.0f)] [SerializeField] private float platformWidthEnd = 0.70f;

    [Header("Special Platform Speed & Crumble")]
    [Tooltip("Moving platform speed at score 0.")]
    [SerializeField] private float movingSpeedStart = 1.5f;
    [Tooltip("Moving platform speed at max difficulty.")]
    [SerializeField] private float movingSpeedEnd = 3.2f;
    [Tooltip("Crumble warning delay at score 0 (seconds).")]
    [SerializeField] private float crumbleDelayStart = 0.80f;
    [Tooltip("Crumble warning delay at max difficulty (seconds).")]
    [SerializeField] private float crumbleDelayEnd = 0.38f;

    [Header("Platform Spawn Weights: Start (Score 0)")]
    [Range(0, 100)] [SerializeField] private int bouncyChanceStart = 15;
    [Range(0, 100)] [SerializeField] private int movingChanceStart = 10;
    [Range(0, 100)] [SerializeField] private int crumblingChanceStart = 5;

    [Header("Platform Spawn Weights: Peak (Score 1500+)")]
    [Range(0, 100)] [SerializeField] private int bouncyChanceEnd = 10;
    [Range(0, 100)] [SerializeField] private int movingChanceEnd = 40;
    [Range(0, 100)] [SerializeField] private int crumblingChanceEnd = 35;

    [Header("Anti-Frustration Safeguards")]
    [Tooltip("Max consecutive crumbling platforms allowed when vertical gap is large.")]
    [SerializeField] private int maxConsecutiveCrumbling = 1;

    [Header("Initial Platform Positions")]
    [SerializeField] private Transform firstPosition;
    [SerializeField] private Transform secondPosition;
    [SerializeField] private Transform thirdPosition;
    [SerializeField] private Transform fourthPosition;

    [Header("Vertical Climber Spacing")]
    [Tooltip("How far above the camera to generate platforms ahead.")]
    [SerializeField] private float spawnAheadDistance = 9.0f;

    private float highestYSpawned = 0f;
    private Camera mainCamera;
    private int consecutiveCrumblingCount = 0;

    [Header("Object Pooling")]
    private Transform platformPoolRoot;
    private Transform coinPoolRoot;
    private readonly Dictionary<GameObject, Queue<GameObject>> platformPools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Queue<GameObject> coinPool = new Queue<GameObject>();
    private readonly Dictionary<GameObject, GameObject> activePlatformToPrefab = new Dictionary<GameObject, GameObject>();
    private readonly HashSet<GameObject> activePlatforms = new HashSet<GameObject>();
    private readonly HashSet<GameObject> activeCoins = new HashSet<GameObject>();
    [SerializeField] private Vector3 baseCoinScale = new Vector3(0.88f, 0.88f, 0.88f);

    public float CurrentDifficultyT { get; private set; } = 0f;

    // Backward compatibility property
    public float CurrentPlatformSpeed => 0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePoolContainers();
    }

    private void InitializePoolContainers()
    {
        if (platformPoolRoot == null)
        {
            GameObject pRoot = new GameObject("[PlatformPool]");
            pRoot.transform.SetParent(transform);
            platformPoolRoot = pRoot.transform;
        }

        if (coinPoolRoot == null)
        {
            GameObject cRoot = new GameObject("[CoinPool]");
            cRoot.transform.SetParent(transform);
            coinPoolRoot = cRoot.transform;
        }
    }

    private void PrewarmPools()
    {
        PrewarmPlatform(groundPrefab, 10);
        PrewarmPlatform(bouncyPrefab, 4);
        PrewarmPlatform(movingPrefab, 4);
        PrewarmPlatform(crumblingPrefab, 4);
        PrewarmCoins(10);
    }

    private void PrewarmPlatform(GameObject prefab, int count)
    {
        if (prefab == null) return;
        if (!platformPools.ContainsKey(prefab))
        {
            platformPools[prefab] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, platformPoolRoot);
            activePlatformToPrefab[obj] = prefab;
            obj.SetActive(false);
            platformPools[prefab].Enqueue(obj);
        }
    }

    private void PrewarmCoins(int count)
    {
        if (coinPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(coinPrefab, coinPoolRoot);
            obj.SetActive(false);
            coinPool.Enqueue(obj);
        }
    }

    public GameObject SpawnPlatform(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!platformPools.ContainsKey(prefab))
        {
            platformPools[prefab] = new Queue<GameObject>();
        }

        GameObject platform;
        if (platformPools[prefab].Count > 0)
        {
            platform = platformPools[prefab].Dequeue();
            platform.transform.position = position;
            platform.transform.rotation = rotation;
            platform.SetActive(true);
        }
        else
        {
            platform = Instantiate(prefab, position, rotation, platformPoolRoot);
            activePlatformToPrefab[platform] = prefab;
        }

        // Cleanly reset components for reuse
        FloorScript fs = platform.GetComponent<FloorScript>();
        if (fs != null) fs.ResetPlatform();

        SpecialPlatform sp = platform.GetComponent<SpecialPlatform>();
        if (sp != null) sp.ResetSpecialPlatform();

        activePlatforms.Add(platform);
        return platform;
    }

    public void DespawnPlatform(GameObject platform)
    {
        if (platform == null) return;

        // Clean up any child coin attached to this platform before recycling
        Coin childCoin = platform.GetComponentInChildren<Coin>();
        if (childCoin != null)
        {
            DespawnCoin(childCoin.gameObject);
        }

        activePlatforms.Remove(platform);
        platform.SetActive(false);

        if (activePlatformToPrefab.TryGetValue(platform, out GameObject prefab))
        {
            if (!platformPools.ContainsKey(prefab))
            {
                platformPools[prefab] = new Queue<GameObject>();
            }
            platformPools[prefab].Enqueue(platform);
        }
        else
        {
            Destroy(platform);
        }
    }

    public GameObject SpawnCoin(Vector3 position)
    {
        if (coinPrefab == null) return null;

        GameObject coin;
        if (coinPool.Count > 0)
        {
            coin = coinPool.Dequeue();
            coin.transform.position = position;
            coin.transform.rotation = Quaternion.identity;
            coin.SetActive(true);
        }
        else
        {
            coin = Instantiate(coinPrefab, position, Quaternion.identity, coinPoolRoot);
        }

        Coin c = coin.GetComponent<Coin>();
        if (c != null) c.ResetCoin();

        activeCoins.Add(coin);
        return coin;
    }

    /// <summary>
    /// Spawns a coin as a child of the platform so it moves with it,
    /// counter-scaled to always maintain its round base size even if the platform shrinks.
    /// Positioned at z = -0.1f so it renders cleanly in front of the platform.
    /// </summary>
    public GameObject SpawnCoinOnPlatform(GameObject platform, float verticalOffset)
    {
        if (coinPrefab == null || platform == null) return null;

        GameObject coin = SpawnCoin(Vector3.zero);
        if (coin != null)
        {
            coin.transform.SetParent(platform.transform);
            // Center horizontally, place at verticalOffset, and z = -0.1f (closer to camera than platform at z = 0)
            coin.transform.localPosition = new Vector3(0f, verticalOffset, -0.1f);
            coin.transform.localRotation = Quaternion.identity;

            // Counter-scale to preserve exact round proportions regardless of parent platform scale
            Vector3 lossyParent = platform.transform.lossyScale;
            float scaleFactorX = Mathf.Abs(lossyParent.x) > 0.001f ? (baseCoinScale.x / lossyParent.x) : baseCoinScale.x;
            float scaleFactorY = Mathf.Abs(lossyParent.y) > 0.001f ? (baseCoinScale.y / lossyParent.y) : baseCoinScale.y;
            coin.transform.localScale = new Vector3(scaleFactorX, scaleFactorY, baseCoinScale.z);
        }

        return coin;
    }

    public void DespawnCoin(GameObject coin)
    {
        if (coin == null) return;

        activeCoins.Remove(coin);

        // Reparent back to coin pool root and restore base scale
        if (coinPoolRoot != null)
        {
            coin.transform.SetParent(coinPoolRoot);
        }
        coin.transform.localScale = baseCoinScale;

        coin.SetActive(false);
        coinPool.Enqueue(coin);
    }

    public void RecycleAllActive()
    {
        List<GameObject> platformsToRecycle = new List<GameObject>(activePlatforms);
        foreach (var p in platformsToRecycle)
        {
            DespawnPlatform(p);
        }

        List<GameObject> coinsToRecycle = new List<GameObject>(activeCoins);
        foreach (var c in coinsToRecycle)
        {
            DespawnCoin(c);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        UpdateHorizontalLimits();
        PrewarmPools();
        GenerateInitialPlatforms();
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        UpdateHorizontalLimits();

        // Procedurally generate platforms ahead of the camera as Tartita ascends
        float targetAltitude = mainCamera.transform.position.y + spawnAheadDistance;
        while (highestYSpawned < targetAltitude)
        {
            float difficultyT = CalculateDifficulty(highestYSpawned);
            CurrentDifficultyT = difficultyT;

            float curMinGap = Mathf.Lerp(minGapStart, minGapEnd, difficultyT);
            float curMaxGap = Mathf.Lerp(maxGapStart, maxGapEnd, difficultyT);
            float gap = Random.Range(curMinGap, curMaxGap);

            highestYSpawned += gap;
            GenerateFloor(highestYSpawned, difficultyT, gap);
        }
    }

    /// <summary>
    /// Computes safe horizontal spawn bounds adapting dynamically to any screen aspect ratio.
    /// </summary>
    public (float minX, float maxX) GetDynamicHorizontalBounds(float platformWidthMultiplier = 1.0f)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        float halfWidth;
        if (mainCamera != null && mainCamera.orthographic)
        {
            halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        }
        else
        {
            halfWidth = 2.8125f; // Standard fallback (5 * 9/16)
        }

        // Base platform half-width is ~0.61 units (with scale ~0.62 on 1.98 unit sprite)
        float baseHalfWidth = 0.61f;
        float scaledHalfWidth = baseHalfWidth * platformWidthMultiplier;
        float edgePadding = 0.20f; // Safe margin so platforms never touch or clip the screen borders
        float margin = scaledHalfWidth + edgePadding;

        float minX = -halfWidth + margin;
        float maxX = halfWidth - margin;

        if (minX >= maxX)
        {
            minX = -0.5f;
            maxX = 0.5f;
        }

        return (minX, maxX);
    }

    /// <summary>
    /// Synchronizes leftLimit and rightLimit transforms to the camera viewport edges if present.
    /// </summary>
    public void UpdateHorizontalLimits()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        if (leftLimit != null)
        {
            leftLimit.position = new Vector3(-halfWidth, leftLimit.position.y, leftLimit.position.z);
        }
        if (rightLimit != null)
        {
            rightLimit.position = new Vector3(halfWidth, rightLimit.position.y, rightLimit.position.z);
        }
    }

    public float CalculateDifficulty(float worldY)
    {
        float score = ScoreManager.instance != null ? ScoreManager.instance.CurrentScore : Mathf.Max(0f, (worldY + 2.77f) * 10f);
        return Mathf.Clamp01(score / Mathf.Max(100f, maxDifficultyScore));
    }

    private void GenerateInitialPlatforms()
    {
        if (firstPosition != null)
        {
            GenerateFloor(firstPosition.position.y, 0f, 1.8f, forceStandard: true);
            highestYSpawned = firstPosition.position.y;
        }
        if (secondPosition != null)
        {
            GenerateFloor(secondPosition.position.y, 0f, 1.8f, forceStandard: true);
            highestYSpawned = Mathf.Max(highestYSpawned, secondPosition.position.y);
        }
        if (thirdPosition != null)
        {
            GenerateFloor(thirdPosition.position.y, 0f, 1.8f, forceStandard: true);
            highestYSpawned = Mathf.Max(highestYSpawned, thirdPosition.position.y);
        }
        if (fourthPosition != null)
        {
            GenerateFloor(fourthPosition.position.y, 0f, 1.8f, forceStandard: true);
            highestYSpawned = Mathf.Max(highestYSpawned, fourthPosition.position.y);
        }

        if (highestYSpawned == 0f)
        {
            highestYSpawned = -2.0f;
        }

        // Fill initial platforms up to the top of the starting screen
        float initialTarget = (mainCamera != null ? mainCamera.transform.position.y : 0f) + spawnAheadDistance;
        while (highestYSpawned < initialTarget)
        {
            float difficultyT = CalculateDifficulty(highestYSpawned);
            float curMinGap = Mathf.Lerp(minGapStart, minGapEnd, difficultyT);
            float curMaxGap = Mathf.Lerp(maxGapStart, maxGapEnd, difficultyT);
            float gap = Random.Range(curMinGap, curMaxGap);

            highestYSpawned += gap;
            bool forceStandard = highestYSpawned < 3.0f;
            GenerateFloor(highestYSpawned, difficultyT, gap, forceStandard: forceStandard);
        }
    }

    private void GenerateFloor(float yPos, float difficultyT, float verticalGap, bool forceStandard = false)
    {
        float widthMultiplier = Mathf.Lerp(platformWidthStart, platformWidthEnd, difficultyT);
        var (dynMinX, dynMaxX) = GetDynamicHorizontalBounds(widthMultiplier);
        float xPos = Random.Range(dynMinX, dynMaxX);

        GameObject prefabToSpawn = groundPrefab;

        if (!forceStandard)
        {
            // Interpolate platform type weights according to difficulty curve
            int bouncyChance = Mathf.RoundToInt(Mathf.Lerp(bouncyChanceStart, bouncyChanceEnd, difficultyT));
            int movingChance = Mathf.RoundToInt(Mathf.Lerp(movingChanceStart, movingChanceEnd, difficultyT));
            int crumblingChance = Mathf.RoundToInt(Mathf.Lerp(crumblingChanceStart, crumblingChanceEnd, difficultyT));

            // Anti-frustration rule: don't spawn consecutive crumbling platforms if the gap is challenging
            if (consecutiveCrumblingCount >= maxConsecutiveCrumbling && verticalGap > 2.6f)
            {
                crumblingChance = 0;
            }

            int roll = Random.Range(0, 100);
            if (roll < bouncyChance && bouncyPrefab != null)
            {
                prefabToSpawn = bouncyPrefab;
                consecutiveCrumblingCount = 0;
            }
            else if (roll < bouncyChance + movingChance && movingPrefab != null)
            {
                prefabToSpawn = movingPrefab;
                consecutiveCrumblingCount = 0;
            }
            else if (roll < bouncyChance + movingChance + crumblingChance && crumblingPrefab != null)
            {
                prefabToSpawn = crumblingPrefab;
                consecutiveCrumblingCount++;
            }
            else
            {
                consecutiveCrumblingCount = 0;
            }
        }
        else
        {
            consecutiveCrumblingCount = 0;
        }

        if (prefabToSpawn != null)
        {
            GameObject floor = SpawnPlatform(prefabToSpawn, new Vector3(xPos, yPos, 0), Quaternion.identity);

            // Configure dynamic moving speed and crumble delay
            float currentMoveSpeed = Mathf.Lerp(movingSpeedStart, movingSpeedEnd, difficultyT);
            float currentCrumbleDelay = Mathf.Lerp(crumbleDelayStart, crumbleDelayEnd, difficultyT);

            SpecialPlatform special = floor.GetComponent<SpecialPlatform>();
            if (special != null)
            {
                special.ConfigureDifficulty(currentMoveSpeed, currentCrumbleDelay, widthMultiplier, dynMinX, dynMaxX);
            }

            FloorScript floorScript = floor.GetComponent<FloorScript>();
            if (floorScript != null)
            {
                floorScript.SetWidthScale(widthMultiplier);
            }

            // 8% chance to spawn a coin on the platform (childed to follow movement, counter-scaled so it never shrinks, z = -0.1f)
            if (Random.Range(0, 100) < 8 && coinPrefab != null)
            {
                SpawnCoinOnPlatform(floor, coinDistance);
            }
        }
    }

    public void TriggerSpeedBoost(float targetBoost = -1f, float duration = -1f)
    {
        // Platforms are stationary in the vertical climber architecture.
    }
}
