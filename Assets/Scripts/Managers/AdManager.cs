using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Services.LevelPlay;

public class AdManager : MonoBehaviour
{
    public static AdManager instance;

    [Header("LevelPlay Mediation Config")]
    [Tooltip("Your LevelPlay App Key from platform.ironsrc.com")]
    [SerializeField] private string androidAppKey = "29586337d";
    [SerializeField] private string iosAppKey = "";
    [Tooltip("Your Rewarded Ad Unit ID from platform.ironsrc.com")]
    [SerializeField] private string androidRewardedAdUnitId = "fy3roig8lrt2kjgv";
    [SerializeField] private string iosRewardedAdUnitId = "";

    [Header("Simulation & Fallback Settings")]
    [Tooltip("If true and LevelPlay ad is not ready or in Editor, uses the interactive placeholder ad.")]
    [SerializeField] private bool useInteractiveFallback = true;
    [SerializeField] private float simulatedAdDuration = 4.0f;

    [Header("Privacy & Child-Directed (COPPA / Google Play Families)")]
    [Tooltip("If true, activates COPPA compliance, disables device ID tracking and configures child-directed ad filtering for Google Play Families.")]
    [SerializeField] private bool isChildDirected = true;

    private bool isShowingAd = false;
    private bool isLevelPlayInitialized = false;
    private bool isAdLoaded = false;
    private LevelPlayRewardedAd rewardedAd;

    private GameObject placeholderCanvasObj;
    private Image progressFillImage;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private Button closeButton;
    private TextMeshProUGUI closeButtonText;
    private Image closeButtonImage;

    private Coroutine currentAdRoutine;
    private Coroutine retryLoadRoutine;
    private Action currentRewardCallback;
    private Action currentCloseCallback;
    private bool rewardGiven = false;

    private const string DefaultAppKey = "29586337d";
    private const string DefaultAdUnitId = "fy3roig8lrt2kjgv";

    private string ActiveAppKey
    {
        get
        {
            string key = androidAppKey;
#if UNITY_IOS
            key = !string.IsNullOrEmpty(iosAppKey) ? iosAppKey : androidAppKey;
#else
            if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(iosAppKey)) key = iosAppKey;
#endif
            return !string.IsNullOrEmpty(key) ? key : DefaultAppKey;
        }
    }

    private string ActiveRewardedAdUnitId
    {
        get
        {
            string unit = androidRewardedAdUnitId;
#if UNITY_IOS
            unit = !string.IsNullOrEmpty(iosRewardedAdUnitId) ? iosRewardedAdUnitId : androidRewardedAdUnitId;
#else
            if (string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(iosRewardedAdUnitId)) unit = iosRewardedAdUnitId;
#endif
            return !string.IsNullOrEmpty(unit) ? unit : DefaultAdUnitId;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLevelPlay();
            BuildPlaceholderAdUI();
            return;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            if (retryLoadRoutine != null)
            {
                StopCoroutine(retryLoadRoutine);
                retryLoadRoutine = null;
            }

            if (rewardedAd != null)
            {
                UnsubscribeRewardedEvents();
            }

            if (placeholderCanvasObj != null)
            {
                Destroy(placeholderCanvasObj);
            }
            instance = null;
        }
    }

    private void InitializeLevelPlay()
    {
        string appKey = ActiveAppKey;
        if (string.IsNullOrEmpty(appKey))
        {
            Debug.LogWarning("[AdManager] LevelPlay App Key is blank. Using interactive simulation mode.");
            return;
        }

        // Apply Privacy & COPPA Settings BEFORE LevelPlay.Init()
        ApplyPrivacyAndAgeSettings();

        Debug.Log($"[AdManager] Initializing LevelPlay Mediation with App Key: '{appKey}'...");
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void ApplyPrivacyAndAgeSettings()
    {
        if (isChildDirected)
        {
            Debug.Log("[AdManager] Setting COPPA = true and child-directed metadata for Families Policy compliance.");
            LevelPlayPrivacySettings.SetCOPPA(true);
            LevelPlay.SetMetaData("is_child_directed", "true");
            LevelPlay.SetMetaData("is_deviceid_optout", "true");
            LevelPlay.SetMetaData("Google_Family_Self_Certified_SDKS", "true");
        }
        else
        {
            Debug.Log("[AdManager] Setting COPPA = false.");
            LevelPlayPrivacySettings.SetCOPPA(false);
            LevelPlay.SetMetaData("is_child_directed", "false");
        }
    }

    private void OnInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("[AdManager] LevelPlay successfully initialized!");
        isLevelPlayInitialized = true;
        SetupRewardedAd();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogWarning($"[AdManager] LevelPlay initialization failed: {error?.ErrorMessage}");
        isLevelPlayInitialized = false;
    }

    private void SetupRewardedAd()
    {
        string adUnitId = ActiveRewardedAdUnitId;
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdManager] LevelPlay Rewarded Ad Unit ID is not configured.");
            return;
        }

        if (rewardedAd != null)
        {
            UnsubscribeRewardedEvents();
        }

        Debug.Log($"[AdManager] Creating LevelPlayRewardedAd for unit: '{adUnitId}'");
        rewardedAd = new LevelPlayRewardedAd(adUnitId);
        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        LoadRewardedAd();
    }

    private void UnsubscribeRewardedEvents()
    {
        if (rewardedAd == null) return;
        rewardedAd.OnAdLoaded -= OnAdLoaded;
        rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        rewardedAd.OnAdDisplayed -= OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        rewardedAd.OnAdRewarded -= OnAdRewarded;
        rewardedAd.OnAdClosed -= OnAdClosed;
    }

    public void LoadRewardedAd()
    {
        if (rewardedAd == null)
        {
            if (isLevelPlayInitialized)
            {
                SetupRewardedAd();
            }
            return;
        }

        Debug.Log("[AdManager] Loading LevelPlay Rewarded Ad...");
        rewardedAd.LoadAd();
    }

    public bool IsRewardedAdReady()
    {
        if (rewardedAd != null && isAdLoaded && rewardedAd.IsAdReady())
        {
            return true;
        }
        return useInteractiveFallback;
    }

    public void ShowRewardedAd(Action onRewardEarned, Action onAdClosed = null)
    {
        if (isShowingAd)
        {
            Debug.LogWarning("[AdManager] An ad is already in progress.");
            return;
        }

        currentRewardCallback = onRewardEarned;
        currentCloseCallback = onAdClosed;
        rewardGiven = false;

        // 1. Try real mediated LevelPlay ad first
        if (rewardedAd != null && isAdLoaded && rewardedAd.IsAdReady())
        {
            isShowingAd = true;
            Debug.Log("[AdManager] Displaying real LevelPlay Rewarded Ad...");
            rewardedAd.ShowAd();
            return;
        }

        // 2. Fall back to our interactive simulated ad overlay
        if (useInteractiveFallback)
        {
            Debug.Log("[AdManager] Real ad not ready yet. Displaying interactive simulated ad overlay...");
            if (currentAdRoutine != null)
            {
                StopCoroutine(currentAdRoutine);
            }
            currentAdRoutine = StartCoroutine(ShowInteractivePlaceholderAdRoutine());
        }
        else
        {
            Debug.LogWarning("[AdManager] No ad ready and simulation disabled. Calling callbacks directly.");
            onRewardEarned?.Invoke();
            onAdClosed?.Invoke();
        }
    }

    // ------------------------------------------------------------------------
    // LevelPlay Rewarded Ad Event Listeners
    // ------------------------------------------------------------------------
    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdManager] LevelPlay Rewarded Ad loaded! (Network: {adInfo?.AdNetwork})");
        isAdLoaded = true;
        if (retryLoadRoutine != null)
        {
            StopCoroutine(retryLoadRoutine);
            retryLoadRoutine = null;
        }
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdManager] LevelPlay Rewarded Ad failed to load: {error?.ErrorMessage} (Code: {error?.ErrorCode})");
        isAdLoaded = false;

        // Automatically retry loading after a brief interval if not currently in an ad
        if (!isShowingAd && gameObject.activeInHierarchy)
        {
            if (retryLoadRoutine != null) StopCoroutine(retryLoadRoutine);
            retryLoadRoutine = StartCoroutine(RetryLoadAdRoutine(8.0f));
        }
    }

    private IEnumerator RetryLoadAdRoutine(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        retryLoadRoutine = null;
        if (!isAdLoaded && !isShowingAd && isLevelPlayInitialized)
        {
            Debug.Log("[AdManager] Retrying LevelPlay Rewarded Ad load...");
            LoadRewardedAd();
        }
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdManager] LevelPlay Rewarded Ad displayed. (Network: {adInfo?.AdNetwork})");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"[AdManager] LevelPlay Rewarded Ad display failed: {error?.ErrorMessage}");
        isShowingAd = false;
        isAdLoaded = false;

        // Fallback to placeholder overlay so gameplay is never blocked
        if (useInteractiveFallback)
        {
            if (currentAdRoutine != null) StopCoroutine(currentAdRoutine);
            currentAdRoutine = StartCoroutine(ShowInteractivePlaceholderAdRoutine());
        }
        else
        {
            Action closeCb = currentCloseCallback;
            currentCloseCallback = null;
            currentRewardCallback = null;
            closeCb?.Invoke();
        }
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[AdManager] LevelPlay Rewarded Ad completed! Reward earned: {reward?.Name} x {reward?.Amount}");
        rewardGiven = true;
        currentRewardCallback?.Invoke();
        currentRewardCallback = null;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] LevelPlay Rewarded Ad closed.");
        isShowingAd = false;
        isAdLoaded = false;

        Action closeCb = currentCloseCallback;
        currentCloseCallback = null;
        closeCb?.Invoke();

        // Preload next ad
        LoadRewardedAd();
    }

    // ------------------------------------------------------------------------
    // Interactive Simulated Placeholder Ad Overlay
    // ------------------------------------------------------------------------
    private IEnumerator ShowInteractivePlaceholderAdRoutine()
    {
        isShowingAd = true;
        Debug.Log("[AdManager] Launching Interactive Placeholder Rewarded Ad...");

        EnsureEventSystem();
        if (placeholderCanvasObj == null)
        {
            BuildPlaceholderAdUI();
        }

        placeholderCanvasObj.SetActive(true);

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        if (closeButton != null)
        {
            closeButton.interactable = true;
        }

        if (closeButtonImage != null)
        {
            closeButtonImage.color = new Color(0.35f, 0.38f, 0.45f, 0.9f);
        }

        if (closeButtonText != null)
        {
            closeButtonText.text = "✕ Skip";
            closeButtonText.fontSize = 26;
        }

        if (titleText != null)
        {
            titleText.text = "🍰 Tartita: Sweet Adventure";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "LevelPlay Simulation • Watch to earn your reward!";
        }

        float elapsed = 0f;
        float duration = Mathf.Max(1.0f, simulatedAdDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            int remainingSeconds = Mathf.CeilToInt(duration - elapsed);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = progress;
            }

            if (timerText != null)
            {
                timerText.text = $"Reward in: {remainingSeconds}s";
            }

            yield return null;
        }

        // Grant reward
        rewardGiven = true;
        Debug.Log("[AdManager] Ad completed! Granting reward.");
        currentRewardCallback?.Invoke();
        currentRewardCallback = null;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 1f;
        }

        if (timerText != null)
        {
            timerText.text = "🎉 Reward Granted!";
            timerText.color = new Color(0.2f, 1f, 0.5f, 1f);
        }

        if (descriptionText != null)
        {
            descriptionText.text = "Reward unlocked! Tap Claim or wait to continue.";
        }

        if (closeButtonImage != null)
        {
            closeButtonImage.color = new Color(0.18f, 0.78f, 0.35f, 1f);
        }

        if (closeButtonText != null)
        {
            closeButtonText.text = "✓ Claim";
            closeButtonText.fontSize = 28;
        }

        yield return new WaitForSecondsRealtime(1.2f);

        CloseAd();
    }

    private void OnCloseButtonClicked()
    {
        if (!isShowingAd) return;

        if (!rewardGiven)
        {
            Debug.Log("[AdManager] Ad skipped early by player. No reward granted.");
            currentRewardCallback = null;
        }

        CloseAd();
    }

    private void CloseAd()
    {
        if (currentAdRoutine != null)
        {
            StopCoroutine(currentAdRoutine);
            currentAdRoutine = null;
        }

        if (placeholderCanvasObj != null)
        {
            placeholderCanvasObj.SetActive(false);
        }

        isShowingAd = false;
        Action closeCb = currentCloseCallback;
        currentCloseCallback = null;
        closeCb?.Invoke();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGo);
        }
    }

    private void BuildPlaceholderAdUI()
    {
        if (placeholderCanvasObj != null) return;

        // 1. Root Canvas
        placeholderCanvasObj = new GameObject("AdPlaceholderCanvas");
        DontDestroyOnLoad(placeholderCanvasObj);

        Canvas canvas = placeholderCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = placeholderCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        placeholderCanvasObj.AddComponent<GraphicRaycaster>();

        // 2. Fullscreen Scrim (Dark Backdrop)
        GameObject scrimObj = new GameObject("Scrim");
        scrimObj.transform.SetParent(placeholderCanvasObj.transform, false);
        Image scrimImage = scrimObj.AddComponent<Image>();
        scrimImage.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
        RectTransform scrimRect = scrimObj.GetComponent<RectTransform>();
        scrimRect.anchorMin = Vector2.zero;
        scrimRect.anchorMax = Vector2.one;
        scrimRect.sizeDelta = Vector2.zero;

        // 3. Central Ad Card Container
        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(scrimObj.transform, false);
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.color = new Color(0.11f, 0.13f, 0.18f, 1f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(900f, 1280f);

        // 4. Top Header Banner
        GameObject headerObj = new GameObject("HeaderBanner");
        headerObj.transform.SetParent(cardObj.transform, false);
        Image headerImage = headerObj.AddComponent<Image>();
        headerImage.color = new Color(0.16f, 0.19f, 0.27f, 1f);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 110f);

        // Header Title Text
        GameObject headerTextObj = new GameObject("HeaderText");
        headerTextObj.transform.SetParent(headerObj.transform, false);
        TextMeshProUGUI headerTmp = headerTextObj.AddComponent<TextMeshProUGUI>();
        headerTmp.text = "📢 LEVELPLAY MEDIATION • REWARDED VIDEO";
        headerTmp.fontSize = 28f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.alignment = TextAlignmentOptions.Center;
        headerTmp.color = new Color(0.9f, 0.92f, 0.98f, 1f);
        RectTransform headerTextRect = headerTextObj.GetComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.sizeDelta = Vector2.zero;

        // 5. Close / Skip Button (Top Right)
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(cardObj.transform, false);
        closeButtonImage = closeBtnObj.AddComponent<Image>();
        closeButtonImage.color = new Color(0.35f, 0.38f, 0.45f, 0.9f);
        closeButton = closeBtnObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-25f, -135f);
        closeBtnRect.sizeDelta = new Vector2(180f, 65f);

        // Close Button Text
        GameObject closeBtnTextObj = new GameObject("Text");
        closeBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
        closeButtonText = closeBtnTextObj.AddComponent<TextMeshProUGUI>();
        closeButtonText.text = "✕ Skip";
        closeButtonText.fontSize = 26f;
        closeButtonText.fontStyle = FontStyles.Bold;
        closeButtonText.alignment = TextAlignmentOptions.Center;
        closeButtonText.color = Color.white;
        RectTransform closeBtnTextRect = closeBtnTextObj.GetComponent<RectTransform>();
        closeBtnTextRect.anchorMin = Vector2.zero;
        closeBtnTextRect.anchorMax = Vector2.one;
        closeBtnTextRect.sizeDelta = Vector2.zero;

        // 6. Video Preview Box
        GameObject previewObj = new GameObject("VideoPreview");
        previewObj.transform.SetParent(cardObj.transform, false);
        Image previewImage = previewObj.AddComponent<Image>();
        previewImage.color = new Color(0.07f, 0.08f, 0.12f, 1f);
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0f, 100f);
        previewRect.sizeDelta = new Vector2(800f, 580f);

        // Video Preview Icon / Animation mockup
        GameObject videoTextObj = new GameObject("VideoCenterText");
        videoTextObj.transform.SetParent(previewObj.transform, false);
        TextMeshProUGUI videoTmp = videoTextObj.AddComponent<TextMeshProUGUI>();
        videoTmp.text = "🎬\nLEVELPLAY TEST AD\n\n[ Real-Time Auction Active ]";
        videoTmp.fontSize = 36f;
        videoTmp.fontStyle = FontStyles.Bold;
        videoTmp.alignment = TextAlignmentOptions.Center;
        videoTmp.color = new Color(0.6f, 0.7f, 0.9f, 0.95f);
        RectTransform videoTextRect = videoTextObj.GetComponent<RectTransform>();
        videoTextRect.anchorMin = Vector2.zero;
        videoTextRect.anchorMax = Vector2.one;
        videoTextRect.sizeDelta = Vector2.zero;

        // 7. Title & Description
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "🍰 Tartita: Sweet Adventure";
        titleText.fontSize = 42f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, -240f);
        titleRect.sizeDelta = new Vector2(800f, 60f);

        GameObject descObj = new GameObject("DescText");
        descObj.transform.SetParent(cardObj.transform, false);
        descriptionText = descObj.AddComponent<TextMeshProUGUI>();
        descriptionText.text = "LevelPlay Simulation • Watch to earn your reward!";
        descriptionText.fontSize = 28f;
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.color = new Color(0.75f, 0.78f, 0.85f, 1f);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.anchoredPosition = new Vector2(0f, -300f);
        descRect.sizeDelta = new Vector2(800f, 50f);

        // 8. Progress Bar Background
        GameObject progressBgObj = new GameObject("ProgressBg");
        progressBgObj.transform.SetParent(cardObj.transform, false);
        Image progressBgImage = progressBgObj.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.23f, 0.32f, 1f);
        RectTransform progressBgRect = progressBgObj.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressBgRect.anchoredPosition = new Vector2(0f, -380f);
        progressBgRect.sizeDelta = new Vector2(760f, 32f);

        // 9. Progress Bar Fill
        GameObject progressFillObj = new GameObject("ProgressFill");
        progressFillObj.transform.SetParent(progressBgObj.transform, false);
        progressFillImage = progressFillObj.AddComponent<Image>();
        progressFillImage.color = new Color(0.22f, 0.85f, 0.45f, 1f);
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.fillAmount = 0f;
        RectTransform progressFillRect = progressFillObj.GetComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.sizeDelta = Vector2.zero;

        // 10. Timer Text
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(cardObj.transform, false);
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "Reward in: 4s";
        timerText.fontSize = 36f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = new Color(1f, 0.88f, 0.35f, 1f);
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = new Vector2(0f, -440f);
        timerRect.sizeDelta = new Vector2(760f, 60f);

        // Initially inactive
        placeholderCanvasObj.SetActive(false);
    }
}
