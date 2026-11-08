using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGameManager : MonoBehaviour
{
    public static UIGameManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            return;
        }
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.OnMusicMuteToggled -= UpdatePauseMusicVisual;
            AudioManager.instance.OnSfxMuteToggled -= UpdatePauseSfxVisual;
        }

        if(instance == this)
        {
            instance = null;
        }
    }

    [Header("Coins")]
    [SerializeField] private GameObject coinNumber;
    [Header("Dead Menu")]
    [SerializeField] private GameObject deadPannel;
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MenuButton;

    [Header("Pause Menu")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRetryButton;
    [SerializeField] private Button pauseMenuButton;
    [SerializeField] private Button pauseMusicButton;
    [SerializeField] private Button pauseSfxButton;

    [Header("Dead Panel")]
    [SerializeField] private TMP_Text coinsThisRunText;

    [Header("Score Display")]
    [SerializeField] private TMP_Text hudScoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Font & Styling")]
    [SerializeField] private Font customFont;
    [SerializeField] private TMP_FontAsset customTMPFont;
    [SerializeField] private Color scoreTextColor = new Color(0.20f, 0.09f, 0.03f, 1f); // Rich dark chocolate
    [SerializeField] private Color coinsTextColor = new Color(0.20f, 0.09f, 0.03f, 1f); // Rich dark chocolate

    [Header("Safe Area")]
    [SerializeField] private bool autoApplySafeArea = true;

    [Header("Tap to Start Prompt")]
    [SerializeField] private GameObject tapToStartPrompt;
    [SerializeField] private TMP_Text tapToStartText;
    private Coroutine tapPromptPulseCoroutine;

    private static TMP_FontAsset cachedFontAsset;

    private int coinsThisRun = 0;
    private PlayerMovement cachedPlayer;
    private bool isPaused = false;
    private bool isGameOver = false;

    public bool IsPaused => isPaused;

    private void Start()
    {
        coinsThisRun = 0;
        isPaused = false;
        isGameOver = false;

        cachedPlayer = FindFirstObjectByType<PlayerMovement>();
        EnsureAudioManager();
        EnsureSaveManager();
        EnsureScoreManager();
        EnsureSafeArea();

        if (cachedPlayer != null && !cachedPlayer.HasGameStarted)
        {
            ShowTapToStart();
        }
        else if (ScoreManager.instance != null)
        {
            float playerY = cachedPlayer != null ? cachedPlayer.transform.position.y : -2.77f;
            ScoreManager.instance.ResetScore(playerY);
        }

        deadPannel.SetActive(false);
        isGameOver = false;
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
        if (pausePanel != null) pausePanel.SetActive(false);

        RetryButton.onClick.AddListener(Retry);
        MenuButton.onClick.AddListener(Menu);

        if (pauseButton != null) pauseButton.onClick.AddListener(PauseGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (pauseRetryButton != null) pauseRetryButton.onClick.AddListener(PauseRetry);
        if (pauseMenuButton != null) pauseMenuButton.onClick.AddListener(PauseMenu);
        if (pauseMusicButton != null) pauseMusicButton.onClick.AddListener(OnPauseMusicClicked);
        if (pauseSfxButton != null) pauseSfxButton.onClick.AddListener(OnPauseSfxClicked);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.OnMusicMuteToggled += UpdatePauseMusicVisual;
            AudioManager.instance.OnSfxMuteToggled += UpdatePauseSfxVisual;
            UpdatePauseMusicVisual(AudioManager.instance.IsMusicMuted);
            UpdatePauseSfxVisual(AudioManager.instance.IsSfxMuted);
        }


        SetupTextStyles();
        UpdateNumber();
        UpdateScoreHUD();
    }

    private void SetupTextStyles()
    {
        TMP_FontAsset fontToUse = customTMPFont;
        if (fontToUse == null)
        {
            if (cachedFontAsset != null)
            {
                fontToUse = cachedFontAsset;
            }
            else
            {
                fontToUse = Resources.Load<TMP_FontAsset>("Fonts/LilitaOne SDF");
#if UNITY_EDITOR
                if (fontToUse == null)
                {
                    fontToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/LilitaOne SDF.asset");
                }
#endif
                if (fontToUse == null)
                {
                    Font font = customFont != null ? customFont : Resources.Load<Font>("Fonts/LilitaOne");
                    if (font == null) font = Resources.Load<Font>("Fonts/Fredoka");
                    if (font != null)
                    {
                        cachedFontAsset = TMP_FontAsset.CreateFontAsset(font);
                        fontToUse = cachedFontAsset;
                    }
                }
            }
        }

        Color32 creamOutline = new Color32(255, 248, 225, 255);
        float outlineThick = 0.22f;

        Material pastryMat = Resources.Load<Material>("Fonts/LilitaOne SDF - Pastry");
#if UNITY_EDITOR
        if (pastryMat == null)
        {
            pastryMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Fonts/LilitaOne SDF - Pastry.mat");
        }
#endif

        if (hudScoreText != null)
        {
            if (fontToUse != null) hudScoreText.font = fontToUse;
            if (pastryMat != null) hudScoreText.fontSharedMaterial = pastryMat;
            hudScoreText.color = scoreTextColor;
            hudScoreText.fontStyle = FontStyles.Bold;
            hudScoreText.outlineColor = creamOutline;
            hudScoreText.outlineWidth = outlineThick;
        }

        if (coinNumber != null)
        {
            TMP_Text coinTmp = coinNumber.GetComponent<TMP_Text>();
            if (coinTmp != null)
            {
                if (fontToUse != null) coinTmp.font = fontToUse;
                if (pastryMat != null) coinTmp.fontSharedMaterial = pastryMat;
                coinTmp.color = coinsTextColor;
                coinTmp.fontStyle = FontStyles.Bold;
                coinTmp.outlineColor = creamOutline;
                coinTmp.outlineWidth = outlineThick;
            }
        }

        if (coinsThisRunText != null)
        {
            if (fontToUse != null) coinsThisRunText.font = fontToUse;
            if (pastryMat != null) coinsThisRunText.fontSharedMaterial = pastryMat;
            coinsThisRunText.color = scoreTextColor;
            coinsThisRunText.fontStyle = FontStyles.Bold;
            coinsThisRunText.outlineColor = creamOutline;
            coinsThisRunText.outlineWidth = outlineThick;
        }

        if (finalScoreText != null)
        {
            if (fontToUse != null) finalScoreText.font = fontToUse;
            if (pastryMat != null) finalScoreText.fontSharedMaterial = pastryMat;
            finalScoreText.color = scoreTextColor;
            finalScoreText.fontStyle = FontStyles.Bold;
            finalScoreText.outlineColor = creamOutline;
            finalScoreText.outlineWidth = outlineThick;
        }

        if (bestScoreText != null)
        {
            if (fontToUse != null) bestScoreText.font = fontToUse;
            if (pastryMat != null) bestScoreText.fontSharedMaterial = pastryMat;
            bestScoreText.color = scoreTextColor;
            bestScoreText.fontStyle = FontStyles.Bold;
            bestScoreText.outlineColor = creamOutline;
            bestScoreText.outlineWidth = outlineThick;
        }

        // Style all action buttons to match the cute, plump pastry "PLAY" button
        StylePastryButton(RetryButton, fontToUse);
        StylePastryButton(MenuButton, fontToUse);
        StylePastryButton(resumeButton, fontToUse);
        StylePastryButton(pauseRetryButton, fontToUse);
        StylePastryButton(pauseMenuButton, fontToUse);

        FormatDeadPanelButtons();
    }

    /// <summary>
    /// Configures the dead panel buttons to be plump and well-proportioned
    /// with generous vertical spacing (25px clean gap) matching the aesthetic of the main menu Play button.
    /// </summary>
    private void FormatDeadPanelButtons()
    {
        Button[] buttons = new Button[] { RetryButton, MenuButton };
        float[] yPositions = new float[] { 50f, -125f };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(280f, 150f);
                rt.anchoredPosition = new Vector2(0f, yPositions[i]);
            }
        }
    }

    /// <summary>
    /// Styles a button with the cute confectionery aesthetic seen on the main menu Play button:
    /// simple unsliced pastry image, thick warm coral outline, creamy honey-yellow lettering, bold rounded font, and upper frosting placement.
    /// </summary>
    private void StylePastryButton(Button button, TMP_FontAsset fontToUse)
    {
        if (button == null) return;

        Image btnImg = button.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.type = Image.Type.Simple; // Un-sliced to preserve the natural rounded donut curves
            btnImg.preserveAspect = false;
            btnImg.color = Color.white;
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.90f, 1f);
            cb.pressedColor = new Color(0.92f, 0.88f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            button.colors = cb;
        }

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>();
        if (tmp == null) return;

        if (fontToUse != null)
        {
            tmp.font = fontToUse;
        }

        Material pastryMat = Resources.Load<Material>("Fonts/LilitaOne SDF - Pastry");
#if UNITY_EDITOR
        if (pastryMat == null)
        {
            pastryMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Fonts/LilitaOne SDF - Pastry.mat");
        }
#endif
        if (pastryMat != null)
        {
            tmp.fontSharedMaterial = pastryMat;
        }

        // 1. Rich dark chocolate face with soft warm cream outline
        tmp.color = scoreTextColor;
        tmp.enableVertexGradient = false;

        // 2. Soft warm cream outline
        tmp.outlineColor = new Color32(255, 248, 225, 255);
        tmp.outlineWidth = 0.22f;

        // 3. Bold font style and centered alignment
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        // 4. Shift text slightly upward into the pink frosting portion of the button
        RectTransform textRt = tmp.rectTransform;
        if (textRt != null)
        {
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 22f);
            textRt.offsetMax = new Vector2(-16f, 6f);
        }

        // 5. Appropriate responsive font sizing
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20;
        tmp.fontSizeMax = 42;
    }

    private void Update()
    {
        if (!isGameOver && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            TogglePause();
        }

        if (Time.timeScale > 0 && cachedPlayer != null && cachedPlayer.HasGameStarted && ScoreManager.instance != null)
        {
            float speed = GroundManager.instance != null ? GroundManager.instance.CurrentPlatformSpeed : 1.5f;
            ScoreManager.instance.UpdateAltitude(cachedPlayer.transform.position.y, speed, Time.deltaTime);
            UpdateScoreHUD();
        }
    }

    private void UpdateScoreHUD()
    {
        if (hudScoreText != null && ScoreManager.instance != null)
        {
            hudScoreText.text = ScoreManager.instance.CurrentScore.ToString();
        }
    }

    public void OnCoinCollectedThisRun()
    {
        coinsThisRun++;
    }

    public void changePannel()
    {
        isGameOver = true;
        if (isPaused)
        {
            isPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }

        deadPannel.SetActive(true);
        if (tapToStartPrompt != null)
        {
            if (tapPromptPulseCoroutine != null)
            {
                StopCoroutine(tapPromptPulseCoroutine);
                tapPromptPulseCoroutine = null;
            }
            tapToStartPrompt.SetActive(false);
        }

        if (ScoreManager.instance != null)
        {
            bool isNewBest = ScoreManager.instance.CheckAndSaveHighScore();
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Score: {ScoreManager.instance.CurrentScore}";
            }
            if (bestScoreText != null)
            {
                if (isNewBest)
                {
                    bestScoreText.text = $"NEW BEST! {ScoreManager.instance.HighScore}";
                    bestScoreText.color = new Color(1f, 0.85f, 0.2f, 1f);
                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.PlayNewBest();
                    }
                }
                else
                {
                    bestScoreText.text = $"Best: {ScoreManager.instance.HighScore}";
                    bestScoreText.color = Color.white;
                }
            }
        }

        if (coinsThisRunText != null)
        {
            coinsThisRunText.text = $"Coins: +{coinsThisRun}";
        }

        if (SaveManager.instance != null)
        {
            SaveManager.instance.Save();
        }

        FormatDeadPanelButtons();
    }

    private void EnsureAudioManager()
    {
        if (AudioManager.instance == null)
        {
            GameObject audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<AudioManager>();
        }
    }

    private void EnsureSaveManager()
    {
        if (SaveManager.instance == null)
        {
            GameObject saveGo = new GameObject("SaveManager");
            saveGo.AddComponent<SaveManager>();
        }
    }

    private void EnsureScoreManager()
    {
        if (ScoreManager.instance == null)
        {
            GameObject scoreGo = new GameObject("ScoreManager");
            scoreGo.AddComponent<ScoreManager>();
        }
    }
    private void Retry()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        GameManager.instance.ChangeScene("SampleScene");
    }
    private void Menu()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        GameManager.instance.ChangeScene("MainMenu");
    }
    public void TogglePause()
    {
        if (isGameOver) return;
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isGameOver || isPaused) return;
        isPaused = true;
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        if (GameManager.instance != null)
        {
            GameManager.instance.StopTime();
        }
        else
        {
            Time.timeScale = 0f;
        }

        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        // Hide "Drag To Move" prompt so it does not overstep / overlap with pause dialog
        if (tapToStartPrompt != null && tapToStartPrompt.activeSelf)
        {
            if (tapPromptPulseCoroutine != null)
            {
                StopCoroutine(tapPromptPulseCoroutine);
                tapPromptPulseCoroutine = null;
            }
            tapToStartPrompt.SetActive(false);
        }

        if (AudioManager.instance != null)
        {
            UpdatePauseMusicVisual(AudioManager.instance.IsMusicMuted);
            UpdatePauseSfxVisual(AudioManager.instance.IsSfxMuted);
        }
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        if (GameManager.instance != null)
        {
            GameManager.instance.SetTime(1f);
        }
        else
        {
            Time.timeScale = 1f;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null && !isGameOver) pauseButton.gameObject.SetActive(true);

        // If the game has not started yet (player hasn't jumped), restore the prompt
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<PlayerMovement>();
        }
        if (cachedPlayer != null && !cachedPlayer.HasGameStarted && !isGameOver)
        {
            ShowTapToStart();
        }
    }

    private void PauseRetry()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        isPaused = false;
        if (GameManager.instance != null)
        {
            GameManager.instance.ChangeScene("SampleScene");
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }
    }

    private void PauseMenu()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        isPaused = false;
        if (GameManager.instance != null)
        {
            GameManager.instance.ChangeScene("MainMenu");
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnPauseMusicClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ToggleMusic();
        }
    }

    private void OnPauseSfxClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ToggleSFX();
            if (!AudioManager.instance.IsSfxMuted)
            {
                AudioManager.instance.PlayClick();
            }
        }
    }

    private void UpdatePauseMusicVisual(bool isMuted)
    {
        if (pauseMusicButton != null)
        {
            Image img = pauseMusicButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isMuted ? new Color(0.6f, 0.6f, 0.6f, 0.45f) : Color.white;
            }
        }
    }

    private void UpdatePauseSfxVisual(bool isMuted)
    {
        if (pauseSfxButton != null)
        {
            Image img = pauseSfxButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isMuted ? new Color(0.6f, 0.6f, 0.6f, 0.45f) : Color.white;
            }
        }
    }

    public void UpdateNumber()
    {
        if (coinNumber != null)
        {
            TMP_Text tmp = coinNumber.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = TransferManager.instance != null ? TransferManager.instance.GetCoins().ToString() : "0";
            }
        }
    }

    /// <summary>
    /// Configures the Safe Area container so that top HUD elements (ScorePanel and PauseButton)
    /// never collide with camera notches, punch-holes, or system status bars.
    /// </summary>
    private void EnsureSafeArea()
    {
        if (!autoApplySafeArea) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Check for existing SafeAreaContainer
        Transform existing = canvas.transform.Find("SafeAreaContainer");
        RectTransform safeContainerRect;

        if (existing != null)
        {
            safeContainerRect = existing.GetComponent<RectTransform>();
        }
        else
        {
            GameObject container = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(SafeAreaFitter));
            container.transform.SetParent(canvas.transform, false);
            // Place as first sibling so it renders behind overlays/panels
            container.transform.SetAsFirstSibling();

            safeContainerRect = container.GetComponent<RectTransform>();
            safeContainerRect.anchorMin = Vector2.zero;
            safeContainerRect.anchorMax = Vector2.one;
            safeContainerRect.offsetMin = Vector2.zero;
            safeContainerRect.offsetMax = Vector2.zero;
        }

        // Reparent top HUD elements (ScorePanel and PauseButton) so their anchors align with safe area
        Transform scorePanelTransform = null;
        if (hudScoreText != null)
        {
            scorePanelTransform = hudScoreText.transform.parent;
            while (scorePanelTransform != null && scorePanelTransform.parent != canvas.transform && scorePanelTransform.parent != safeContainerRect)
            {
                scorePanelTransform = scorePanelTransform.parent;
            }
        }
        else
        {
            scorePanelTransform = canvas.transform.Find("ScorePanel");
        }

        if (scorePanelTransform != null && scorePanelTransform.parent == canvas.transform)
        {
            scorePanelTransform.SetParent(safeContainerRect, true);
        }

        if (pauseButton != null && pauseButton.transform.parent == canvas.transform)
        {
            pauseButton.transform.SetParent(safeContainerRect, true);
        }

        // Also reparent coin counter panel so top HUD margins are respected
        Transform coinPanelTransform = null;
        if (coinNumber != null)
        {
            coinPanelTransform = coinNumber.transform.parent;
            while (coinPanelTransform != null && coinPanelTransform.parent != canvas.transform && coinPanelTransform.parent != safeContainerRect)
            {
                coinPanelTransform = coinPanelTransform.parent;
            }
        }
        else
        {
            coinPanelTransform = canvas.transform.Find("Panel");
        }

        if (coinPanelTransform != null && coinPanelTransform.parent == canvas.transform)
        {
            coinPanelTransform.SetParent(safeContainerRect, true);
        }
    }

    /// <summary>
    /// Displays the pulsing Drag To Move prompt on screen.
    /// </summary>
    public void ShowTapToStart()
    {
        EnsureTapToStartPrompt();
        if (tapToStartPrompt != null)
        {
            CanvasGroup cg = tapToStartPrompt.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            tapToStartPrompt.SetActive(true);
            if (tapPromptPulseCoroutine != null) StopCoroutine(tapPromptPulseCoroutine);
            tapPromptPulseCoroutine = StartCoroutine(TapToStartPulseRoutine());
        }
    }

    /// <summary>
    /// Smoothly fades out and deactivates the Drag To Move prompt.
    /// </summary>
    public void HideTapToStart()
    {
        if (tapToStartPrompt != null && tapToStartPrompt.activeSelf)
        {
            if (tapPromptPulseCoroutine != null) StopCoroutine(tapPromptPulseCoroutine);
            StartCoroutine(FadeOutTapPromptRoutine());
        }
    }

    private void EnsureTapToStartPrompt()
    {
        if (tapToStartPrompt != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("TapToStartPrompt");
        if (existing != null)
        {
            tapToStartPrompt = existing.gameObject;
            tapToStartText = tapToStartPrompt.GetComponentInChildren<TMP_Text>();
            return;
        }

        // Create charming pastry-styled Drag To Move prompt centered on screen
        GameObject promptGO = new GameObject("TapToStartPrompt", typeof(RectTransform), typeof(CanvasGroup));
        promptGO.transform.SetParent(canvas.transform, false);

        RectTransform rt = promptGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(850f, 260f);
        rt.anchoredPosition = new Vector2(0f, 40f);

        // 1. Text: DRAG TO MOVE
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(promptGO.transform, false);
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0.35f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        tapToStartText = textGO.GetComponent<TextMeshProUGUI>();
        tapToStartText.text = "DRAG TO MOVE";
        tapToStartText.alignment = TextAlignmentOptions.Center;
        tapToStartText.fontSize = 62;
        tapToStartText.fontStyle = FontStyles.Bold;
        tapToStartText.color = new Color(0.20f, 0.09f, 0.03f, 1f); // Rich dark chocolate
        tapToStartText.outlineColor = new Color32(255, 248, 225, 255);
        tapToStartText.outlineWidth = 0.22f;

        if (customTMPFont != null)
        {
            tapToStartText.font = customTMPFont;
        }
        else if (cachedFontAsset != null)
        {
            tapToStartText.font = cachedFontAsset;
        }

        // 2. Confectionery Double Arrow indicator
        GameObject arrowGO = new GameObject("DragArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        arrowGO.transform.SetParent(promptGO.transform, false);
        RectTransform arrowRt = arrowGO.GetComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(0.5f, 0.22f);
        arrowRt.anchorMax = new Vector2(0.5f, 0.22f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.sizeDelta = new Vector2(280f, 85f);
        arrowRt.anchoredPosition = Vector2.zero;

        Image arrowImg = arrowGO.GetComponent<Image>();
        arrowImg.raycastTarget = false;
        arrowImg.preserveAspect = true;
        arrowImg.sprite = Resources.Load<Sprite>("Ui/Icon_Drag_Arrow");
#if UNITY_EDITOR
        if (arrowImg.sprite == null)
        {
            arrowImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Ui/Icon_Drag_Arrow.png");
            if (arrowImg.sprite == null)
            {
                arrowImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Icon_Drag_Arrow.png");
            }
        }
#endif
        // Failsafe: Never render an empty solid white box if sprite is missing
        arrowImg.enabled = (arrowImg.sprite != null);

        tapToStartPrompt = promptGO;
    }

    private IEnumerator TapToStartPulseRoutine()
    {
        if (tapToStartPrompt == null) yield break;
        Transform promptT = tapToStartPrompt.transform;
        Transform arrowT = promptT.Find("DragArrow");
        Vector3 baseScale = Vector3.one;

        while (tapToStartPrompt != null && tapToStartPrompt.activeSelf)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.05f;
            promptT.localScale = baseScale * scale;

            if (arrowT != null)
            {
                // Gentle horizontal sliding hint
                float slideX = Mathf.Sin(Time.unscaledTime * 3.8f) * 16f;
                arrowT.localPosition = new Vector3(slideX, -45f, 0f);
            }
            yield return null;
        }
    }

    private IEnumerator FadeOutTapPromptRoutine()
    {
        if (tapToStartPrompt == null) yield break;
        CanvasGroup cg = tapToStartPrompt.GetComponent<CanvasGroup>();
        if (cg == null) cg = tapToStartPrompt.AddComponent<CanvasGroup>();

        float dur = 0.18f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(1f - (elapsed / dur));
            yield return null;
        }

        tapToStartPrompt.SetActive(false);
        cg.alpha = 1f;
    }
}
