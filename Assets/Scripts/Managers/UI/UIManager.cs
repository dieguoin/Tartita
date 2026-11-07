using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            return;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.OnSaveDataUpdated -= HandleSaveDataUpdated;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.OnMusicMuteToggled -= UpdateMusicButtonVisual;
            AudioManager.instance.OnSfxMuteToggled -= UpdateSfxButtonVisual;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    [Header("Menu Buttons")]
    [SerializeField] private Button StartButton;
    [SerializeField] private Button MusicButton;
    [SerializeField] private Button SFXButton;

    [Header("Main Menu Player Display")]
    [SerializeField] private GameObject cakeImage;

    [Header("Coins")]
    [SerializeField] private TMP_Text coinText;

    [Header("High Score")]
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Font & Styling")]
    [SerializeField] private Font customFont;
    [SerializeField] private TMP_FontAsset customTMPFont;
    [SerializeField] private Color scoreTextColor = new Color(0.20f, 0.09f, 0.03f, 1f); // Rich dark chocolate
    [SerializeField] private Color coinsTextColor = new Color(0.20f, 0.09f, 0.03f, 1f); // Rich dark chocolate

    private static TMP_FontAsset cachedFontAsset;

    [Header("Character Selection")]
    [SerializeField] private GameObject currentCake;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image priceImage;
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private GameObject playerPannel;

    [Header("Cakes (Add any number of Cake ScriptableObjects here)")]
    [SerializeField] public List<Cake> cakeList = new List<Cake>();

    private int cakeNumber = 0;
    private int equippedIndex = 0;

    // Shake & Red Flash tracking (affects only the currentCake image)
    private Coroutine feedbackCoroutine;
    private Vector2 originalCakePos;
    private Color originalCakeColor;
    private RectTransform currentCakeRect;
    private Image currentCakeImg;
    private bool feedbackInitialized = false;

    private void Start()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.OnSaveDataUpdated += HandleSaveDataUpdated;
        }

        if (playerPannel != null)
        {
            playerPannel.SetActive(false);
        }

        InitFeedbackComponents();
        EnsureSafeArea();
        EnsureCharacterShine(cakeImage);
        EnsureCharacterShine(currentCake);

        // Sync initial equipped cake
        SyncEquippedCake();

        // Print initial coins and high score
        SetupTextStyles();
        PrintText();
        UpdateBestScore();

        // Button listeners
        if (StartButton != null)
        {
            StartButton.onClick.AddListener(() =>
            {
                if (AudioManager.instance != null) AudioManager.instance.PlayClick();
                ChangeScene();
            });
        }

        if (MusicButton != null)
        {
            MusicButton.onClick.AddListener(OnMusicButtonClicked);
        }

        if (SFXButton != null)
        {
            SFXButton.onClick.AddListener(OnSfxButtonClicked);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.OnMusicMuteToggled += UpdateMusicButtonVisual;
            AudioManager.instance.OnSfxMuteToggled += UpdateSfxButtonVisual;
            UpdateMusicButtonVisual(AudioManager.instance.IsMusicMuted);
            UpdateSfxButtonVisual(AudioManager.instance.IsSfxMuted);
        }

        if (cakeImage != null)
        {
            Button cakeBtn = cakeImage.GetComponent<Button>();
            if (cakeBtn != null)
            {
                cakeBtn.onClick.AddListener(OpenCharacterSelection);
            }
        }

        if (currentCake != null)
        {
            Button currentCakeBtn = currentCake.GetComponent<Button>();
            if (currentCakeBtn != null)
            {
                currentCakeBtn.onClick.AddListener(OnCurrentCakeClicked);
            }
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(NextCake);
        }

        if (leftButton != null)
        {
            leftButton.onClick.AddListener(PreviousCake);
        }

        // Allow clicking the backdrop of playerPannel to close the menu
        if (playerPannel != null)
        {
            Button panelBgBtn = playerPannel.GetComponent<Button>();
            if (panelBgBtn == null)
            {
                panelBgBtn = playerPannel.AddComponent<Button>();
                panelBgBtn.transition = Selectable.Transition.None;
            }
            panelBgBtn.onClick.AddListener(CloseCharacterSelection);
        }
    }

    private void InitFeedbackComponents()
    {
        if (currentCake != null && !feedbackInitialized)
        {
            currentCakeRect = currentCake.GetComponent<RectTransform>();
            currentCakeImg = currentCake.GetComponent<Image>();
            if (currentCakeRect != null) originalCakePos = currentCakeRect.anchoredPosition;
            if (currentCakeImg != null) originalCakeColor = currentCakeImg.color;
            feedbackInitialized = true;
        }
    }

    private void SyncEquippedCake()
    {
        if (cakeList == null || cakeList.Count == 0) return;

        Sprite currentSkin = TransferManager.instance != null ? TransferManager.instance.GetImage() : null;
        equippedIndex = 0;

        if (currentSkin != null)
        {
            for (int i = 0; i < cakeList.Count; i++)
            {
                if (cakeList[i] != null && cakeList[i].skin == currentSkin)
                {
                    equippedIndex = i;
                    break;
                }
            }
        }
        else
        {
            if (cakeList[0] != null && cakeList[0].skin != null && TransferManager.instance != null)
            {
                TransferManager.instance.SetImage(cakeList[0].skin);
            }
        }

        cakeNumber = equippedIndex;

        if (cakeImage != null && cakeList[equippedIndex] != null)
        {
            Image img = cakeImage.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = cakeList[equippedIndex].skin;
            }
        }
    }

    public void OpenCharacterSelection()
    {
        if (cakeList == null || cakeList.Count == 0) return;

        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        ResetFeedback();
        cakeNumber = equippedIndex;
        UpdatePreview();

        if (playerPannel != null)
        {
            playerPannel.SetActive(true);
        }
    }

    public void CloseCharacterSelection()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        ResetFeedback();
        if (playerPannel != null)
        {
            playerPannel.SetActive(false);
        }
    }

    private void NextCake()
    {
        if (cakeList == null || cakeList.Count == 0) return;
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        ResetFeedback();

        cakeNumber = (cakeNumber + 1) % cakeList.Count;
        UpdatePreview();
    }

    private void PreviousCake()
    {
        if (cakeList == null || cakeList.Count == 0) return;
        if (AudioManager.instance != null) AudioManager.instance.PlayClick();
        ResetFeedback();

        cakeNumber = (cakeNumber - 1 < 0) ? cakeList.Count - 1 : cakeNumber - 1;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (cakeNumber < 0 || cakeNumber >= cakeList.Count) return;
        Cake selectedCake = cakeList[cakeNumber];
        if (selectedCake == null) return;

        if (currentCake != null)
        {
            Image img = currentCake.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = selectedCake.skin;
            }
        }

        ChangePrice();
    }

    private void ChangePrice()
    {
        if (cakeNumber < 0 || cakeNumber >= cakeList.Count) return;
        Cake selectedCake = cakeList[cakeNumber];
        if (selectedCake == null) return;

        bool isUnlocked = selectedCake.GetState() == Cake.State.Unlocked;

        // The whole price cartel/sign container (wooden sign background, coin icon, price text)
        GameObject priceContainer = pricePanel != null
            ? pricePanel
            : (priceText != null ? priceText.transform.parent.gameObject : (priceImage != null ? priceImage.transform.parent.gameObject : null));

        if (priceContainer != null)
        {
            // For unlocked characters, completely hide the price sign!
            priceContainer.SetActive(!isUnlocked);
        }

        if (!isUnlocked)
        {
            if (priceImage != null)
            {
                priceImage.gameObject.SetActive(true);
                priceImage.rectTransform.anchoredPosition = new Vector2(-65f, 0f);
            }

            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                priceText.text = selectedCake.GetPrice().ToString();
                priceText.rectTransform.anchoredPosition = new Vector2(35f, 0f);
                priceText.alignment = TextAlignmentOptions.Left;
                priceText.color = scoreTextColor;
            }
        }
    }

    private void OnCurrentCakeClicked()
    {
        if (cakeNumber < 0 || cakeNumber >= cakeList.Count) return;
        Cake selectedCake = cakeList[cakeNumber];
        if (selectedCake == null) return;

        // Case 1: Already unlocked -> equip immediately
        if (selectedCake.GetState() == Cake.State.Unlocked)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayClick();
            EquipCake(selectedCake);
            CloseCharacterSelection();
            return;
        }

        // Case 2: Locked -> attempt purchase
        int price = selectedCake.GetPrice();
        int playerCoins = TransferManager.instance != null ? TransferManager.instance.GetCoins() : 0;

        if (playerCoins >= price)
        {
            // Successful purchase
            if (TransferManager.instance != null)
            {
                TransferManager.instance.Pay(price);
            }

            selectedCake.Unlock();

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayUnlock();
            }

            if (FileManager.instance != null)
            {
                FileManager.instance.SaveCakesState();
            }

            PrintText();
            ChangePrice();
            EquipCake(selectedCake);
            CloseCharacterSelection();
        }
        else
        {
            // Insufficient coins -> shake and little red flash (ONLY on the character image)
            TriggerNotEnoughMoneyFeedback();
        }
    }

    public void ChangeCake(Sprite image)
    {
        if (image == null || cakeList == null) return;
        for (int i = 0; i < cakeList.Count; i++)
        {
            if (cakeList[i] != null && cakeList[i].skin == image)
            {
                cakeNumber = i;
                EquipCake(cakeList[i]);
                break;
            }
        }
    }

    private void EquipCake(Cake cake)
    {
        if (cake == null) return;

        equippedIndex = cakeNumber;

        if (cakeImage != null)
        {
            Image img = cakeImage.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = cake.skin;
            }
        }

        if (TransferManager.instance != null)
        {
            TransferManager.instance.SetImage(cake.skin);
        }
    }

    private void TriggerNotEnoughMoneyFeedback()
    {
        ResetFeedback();
        InitFeedbackComponents();
        if (currentCakeRect != null && currentCakeImg != null)
        {
            feedbackCoroutine = StartCoroutine(ShakeAndFlashRoutine());
        }
    }

    private IEnumerator ShakeAndFlashRoutine()
    {
        float duration = 0.35f;
        float elapsed = 0f;
        float shakeMagnitude = 14f;
        Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Decay shake over time
            float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, progress);
            float xOffset = Mathf.Sin(progress * Mathf.PI * 10f) * currentMagnitude;
            currentCakeRect.anchoredPosition = originalCakePos + new Vector2(xOffset, 0f);

            // Red flash lerps smoothly back to original color
            currentCakeImg.color = Color.Lerp(flashColor, originalCakeColor, progress);

            yield return null;
        }

        ResetFeedback();
    }

    private void ResetFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (currentCakeRect != null)
        {
            currentCakeRect.anchoredPosition = originalCakePos;
        }

        if (currentCakeImg != null)
        {
            currentCakeImg.color = originalCakeColor;
        }
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

        Material pastryMat = Resources.Load<Material>("Fonts/LilitaOne SDF - Pastry");
#if UNITY_EDITOR
        if (pastryMat == null)
        {
            pastryMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Fonts/LilitaOne SDF - Pastry.mat");
        }
#endif

        if (coinText != null)
        {
            if (fontToUse != null) coinText.font = fontToUse;
            if (pastryMat != null) coinText.fontSharedMaterial = pastryMat;
            coinText.color = coinsTextColor;
            coinText.fontStyle = FontStyles.Bold;
            ApplyPastryOutline(coinText);
        }

        if (bestScoreText != null)
        {
            if (fontToUse != null) bestScoreText.font = fontToUse;
            if (pastryMat != null) bestScoreText.fontSharedMaterial = pastryMat;
            bestScoreText.color = scoreTextColor;
            bestScoreText.fontStyle = FontStyles.Bold;
            ApplyPastryOutline(bestScoreText);
        }

        if (priceText != null)
        {
            if (fontToUse != null) priceText.font = fontToUse;
            if (pastryMat != null) priceText.fontSharedMaterial = pastryMat;
            priceText.color = scoreTextColor;
            priceText.fontStyle = FontStyles.Bold;
            ApplyPastryOutline(priceText);
        }
    }

    private void ApplyPastryOutline(TMP_Text text)
    {
        if (text == null) return;
        text.outlineColor = new Color32(255, 248, 225, 255); // Soft warm cream
        text.outlineWidth = 0.22f;
    }

    private void EnsureCharacterShine(GameObject target)
    {
        if (target == null) return;
        if (target.GetComponent<CharacterShineEffect>() == null)
        {
            target.AddComponent<CharacterShineEffect>();
        }
    }

    public void PrintText()
    {
        if (coinText != null && TransferManager.instance != null)
        {
            coinText.text = TransferManager.instance.GetCoins().ToString();
        }
    }

    public void UpdateBestScore()
    {
        if (bestScoreText != null)
        {
            int score = (SaveManager.instance != null && SaveManager.instance.CurrentSave != null)
                ? SaveManager.instance.CurrentSave.highScore
                : (ScoreManager.instance != null ? ScoreManager.instance.HighScore : 0);
            bestScoreText.text = $"Best: {score}";
        }
    }

    private void HandleSaveDataUpdated(PlayerSaveData data)
    {
        PrintText();
        UpdateBestScore();
        if (FileManager.instance != null && data != null && data.unlockedCakeNames != null)
        {
            FileManager.instance.ApplyUnlockedCakes(data.unlockedCakeNames);
        }
    }

    private void OnMusicButtonClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.ToggleMusic();
        }
    }

    private void OnSfxButtonClicked()
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

    private void UpdateMusicButtonVisual(bool isMuted)
    {
        if (MusicButton != null)
        {
            Image img = MusicButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isMuted ? new Color(0.6f, 0.6f, 0.6f, 0.45f) : Color.white;
            }
        }
    }

    private void UpdateSfxButtonVisual(bool isMuted)
    {
        if (SFXButton != null)
        {
            Image img = SFXButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isMuted ? new Color(0.6f, 0.6f, 0.6f, 0.45f) : Color.white;
            }
        }
    }

    private void ChangeScene()
    {
        if (FileManager.instance != null)
        {
            FileManager.instance.SaveCakesState();
        }

        if (SceneController.instance != null)
        {
            SceneController.instance.ChangeScene("SampleScene");
        }
    }

    /// <summary>
    /// Configures the Safe Area container so that top menu panels and audio buttons
    /// never collide with camera notches, punch-holes, or system status bars.
    /// </summary>
    private void EnsureSafeArea()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

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
            container.transform.SetAsLastSibling();

            safeContainerRect = container.GetComponent<RectTransform>();
            safeContainerRect.anchorMin = Vector2.zero;
            safeContainerRect.anchorMax = Vector2.one;
            safeContainerRect.offsetMin = Vector2.zero;
            safeContainerRect.offsetMax = Vector2.zero;
        }

        safeContainerRect.transform.SetAsLastSibling();

        Transform coinPanel = canvas.transform.Find("CoinPanel");
        if (coinPanel != null) coinPanel.SetParent(safeContainerRect, true);

        Transform bestScorePanel = canvas.transform.Find("BestScorePanel");
        if (bestScorePanel != null) bestScorePanel.SetParent(safeContainerRect, true);

        if (MusicButton != null && MusicButton.transform.parent == canvas.transform)
        {
            MusicButton.transform.SetParent(safeContainerRect, true);
        }
        if (SFXButton != null && SFXButton.transform.parent == canvas.transform)
        {
            SFXButton.transform.SetParent(safeContainerRect, true);
        }
    }
}
