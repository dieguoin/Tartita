using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoogleCloudSaveUI : MonoBehaviour
{
    public static GoogleCloudSaveUI instance;

    [Header("Modal Panel")]
    [SerializeField] private GameObject modalPanel;

    [Header("Text Displays")]
    [SerializeField] private TMP_Text accountStatusText;
    [SerializeField] private TMP_Text syncStatusText;
    [SerializeField] private TMP_Text dataSummaryText;

    [Header("Buttons")]
    [SerializeField] private Button signInButton;
    [SerializeField] private Button syncNowButton;
    [SerializeField] private Button signOutButton;
    [SerializeField] private Button closeButton;

    [Header("Header Button (Main Menu)")]
    [SerializeField] private Button openModalButton;
    [SerializeField] private TMP_Text openButtonLabel;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (openModalButton != null)
        {
            openModalButton.onClick.AddListener(OpenModal);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseModal);
        }

        if (signInButton != null)
        {
            signInButton.onClick.AddListener(OnSignInClicked);
        }

        if (syncNowButton != null)
        {
            syncNowButton.onClick.AddListener(OnSyncNowClicked);
        }

        if (signOutButton != null)
        {
            signOutButton.onClick.AddListener(OnSignOutClicked);
        }

        if (SaveManager.instance != null)
        {
            SaveManager.instance.OnCloudSyncStatusChanged += HandleCloudSyncStatusChanged;
            SaveManager.instance.OnSaveDataUpdated += HandleSaveDataUpdated;
        }

        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.OnCloudSyncStatusChanged -= HandleCloudSyncStatusChanged;
            SaveManager.instance.OnSaveDataUpdated -= HandleSaveDataUpdated;
        }
    }

    public void OpenModal()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(true);
        }
        RefreshUI();
    }

    public void CloseModal()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (SaveManager.instance == null) return;

        bool isLinked = SaveManager.instance.IsGoogleLinked;
        string displayName = SaveManager.instance.GoogleDisplayName;

        // Main Menu Button Pill Label
        if (openButtonLabel != null)
        {
            openButtonLabel.text = isLinked ? "Cloud: On" : "Cloud Save";
        }

        // Account Status Text
        if (accountStatusText != null)
        {
            if (isLinked)
            {
                accountStatusText.text = $"<color=#81C784>Connected as:</color>\n<b>{displayName}</b>";
            }
            else
            {
                accountStatusText.text = "<color=#FFB74D>Local Save Only</color>\n<size=18>Log in with Google to protect your save online!</size>";
            }
        }

        // Button Visibility
        if (signInButton != null)
        {
            signInButton.gameObject.SetActive(!isLinked);
        }

        if (syncNowButton != null)
        {
            syncNowButton.gameObject.SetActive(isLinked);
        }

        if (signOutButton != null)
        {
            signOutButton.gameObject.SetActive(isLinked);
        }

        // Data Summary
        if (dataSummaryText != null)
        {
            int coins = TransferManager.instance != null ? TransferManager.instance.GetCoins() : 0;
            int highScore = ScoreManager.instance != null ? ScoreManager.instance.HighScore : 0;
            int cakes = FileManager.instance != null ? FileManager.instance.GetUnlockedCakeNames().Count : 0;
            dataSummaryText.text = $"Coins: <b>{coins}</b>   |   Best: <b>{highScore}</b>   |   Cakes: <b>{cakes}</b>";
        }
    }

    private void OnSignInClicked()
    {
        if (SaveManager.instance == null) return;

        if (signInButton != null) signInButton.interactable = false;
        if (syncStatusText != null) syncStatusText.text = "Signing in...";

        SaveManager.instance.SignInWithGoogle((success, msg) =>
        {
            if (signInButton != null) signInButton.interactable = true;
            if (syncStatusText != null) syncStatusText.text = msg;
            RefreshUI();
        });
    }

    private void OnSyncNowClicked()
    {
        if (SaveManager.instance == null) return;

        if (syncNowButton != null) syncNowButton.interactable = false;
        if (syncStatusText != null) syncStatusText.text = "Syncing with cloud...";

        SaveManager.instance.SyncNow((success, msg) =>
        {
            if (syncNowButton != null) syncNowButton.interactable = true;
            if (syncStatusText != null) syncStatusText.text = msg;
            RefreshUI();
        });
    }

    private void OnSignOutClicked()
    {
        if (SaveManager.instance == null) return;

        SaveManager.instance.SignOutGoogle(() =>
        {
            if (syncStatusText != null) syncStatusText.text = "Signed out of Google.";
            RefreshUI();
        });
    }

    private void HandleCloudSyncStatusChanged(CloudSyncStatus status, string message)
    {
        if (syncStatusText != null)
        {
            syncStatusText.text = message;
        }
        RefreshUI();
    }

    private void HandleSaveDataUpdated(PlayerSaveData data)
    {
        RefreshUI();
    }
}

