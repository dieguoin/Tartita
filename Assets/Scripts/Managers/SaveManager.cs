using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("Configuration")]
    [SerializeField] private string saveFileName = "player_save.json";
    [SerializeField] private bool useMockCloudInEditor = true;

    private PlayerSaveData currentSave;
    private string saveFilePath;
    private ICloudSaveProvider cloudProvider;
    private CloudSyncStatus currentSyncStatus = CloudSyncStatus.NotSignedIn;

    public PlayerSaveData CurrentSave => currentSave;
    public CloudSyncStatus CurrentSyncStatus => currentSyncStatus;
    public bool IsGoogleLinked => currentSave != null && currentSave.isGoogleLinked;
    public string GoogleDisplayName => currentSave != null ? currentSave.googleDisplayName : "";

    public event Action<PlayerSaveData> OnSaveDataUpdated;
    public event Action<CloudSyncStatus, string> OnCloudSyncStatusChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
            return;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void InitializeSystem()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

        // Setup Cloud Provider
#if UNITY_EDITOR
        if (useMockCloudInEditor)
        {
            cloudProvider = gameObject.AddComponent<MockGoogleCloudProvider>();
        }
        else
        {
            cloudProvider = gameObject.AddComponent<GooglePlayCloudProvider>();
        }
#elif UNITY_ANDROID
        cloudProvider = gameObject.AddComponent<GooglePlayCloudProvider>();
#else
        cloudProvider = gameObject.AddComponent<MockGoogleCloudProvider>();
#endif

        cloudProvider.Initialize(success =>
        {
            Debug.Log($"[SaveManager] Cloud provider initialized. Status: {success}");
        });

        Load();
    }

    /// <summary>
    /// Loads the player save file from JSON. If missing, attempts migration from legacy data.
    /// </summary>
    public void Load()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        }

        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                currentSave = PlayerSaveData.FromJson(json);
                Debug.Log($"[SaveManager] Loaded save from JSON. Coins: {currentSave.coins}, HighScore: {currentSave.highScore}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error reading JSON file: {ex.Message}");
                currentSave = new PlayerSaveData();
            }
        }
        else
        {
            // First time running or migrating
            currentSave = MigrateLegacyData();
            Save(syncCloudIfLoggedIn: false);
        }

        // Set status
        if (currentSave.isGoogleLinked)
        {
            currentSyncStatus = CloudSyncStatus.Synced;
        }
        else
        {
            currentSyncStatus = CloudSyncStatus.NotSignedIn;
        }

        ApplySaveToManagers();
        OnSaveDataUpdated?.Invoke(currentSave);
    }

    /// <summary>
    /// Migrates legacy SaveStates.json (cakes) and PlayerPrefs ("HighScore") into the JSON save model.
    /// </summary>
    private PlayerSaveData MigrateLegacyData()
    {
        PlayerSaveData migrated = new PlayerSaveData();

        // 1. High Score legacy migration
        if (PlayerPrefs.HasKey("HighScore"))
        {
            migrated.highScore = PlayerPrefs.GetInt("HighScore", 0);
            Debug.Log($"[SaveManager] Migrated HighScore ({migrated.highScore}) from legacy PlayerPrefs into JSON.");
            PlayerPrefs.DeleteKey("HighScore");
            PlayerPrefs.Save();
        }

        // 2. Cakes legacy migration
        string legacyCakePath = Path.Combine(Application.persistentDataPath, "SaveStates.json");
        if (File.Exists(legacyCakePath))
        {
            try
            {
                string cakeJson = File.ReadAllText(legacyCakePath);
                DataSave ds = JsonUtility.FromJson<DataSave>(cakeJson);
                if (ds != null && ds.cakeEntries != null)
                {
                    foreach (var entry in ds.cakeEntries)
                    {
                        if (entry.state == "Unlocked" && !migrated.unlockedCakeNames.Contains(entry.cakeName))
                        {
                            migrated.unlockedCakeNames.Add(entry.cakeName);
                        }
                    }
                }
                Debug.Log($"[SaveManager] Migrated {migrated.unlockedCakeNames.Count} unlocked cakes from legacy SaveStates.json.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Legacy cake migration failed: {ex.Message}");
            }
        }

        return migrated;
    }

    /// <summary>
    /// Distributes loaded JSON data to all active game managers.
    /// </summary>
    public void ApplySaveToManagers()
    {
        if (currentSave == null) return;

        // Apply Coins
        if (TransferManager.instance != null)
        {
            TransferManager.instance.SetCoins(currentSave.coins, save: false);
        }

        // Apply High Score
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.SetHighScore(currentSave.highScore);
        }

        // Apply Cakes
        if (FileManager.instance != null)
        {
            FileManager.instance.ApplyUnlockedCakes(currentSave.unlockedCakeNames);
        }
    }

    /// <summary>
    /// Saves current game state to player_save.json.
    /// </summary>
    public void Save(bool syncCloudIfLoggedIn = true)
    {
        if (currentSave == null) currentSave = new PlayerSaveData();

        // Pull latest values from managers
        if (TransferManager.instance != null)
        {
            currentSave.coins = TransferManager.instance.GetCoins();
        }

        if (ScoreManager.instance != null)
        {
            currentSave.highScore = ScoreManager.instance.HighScore;
        }

        if (FileManager.instance != null)
        {
            currentSave.unlockedCakeNames = FileManager.instance.GetUnlockedCakeNames();
        }

        currentSave.lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        try
        {
            string json = currentSave.ToJson(prettyPrint: true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[SaveManager] Successfully saved game to JSON ({saveFilePath}).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Failed to write JSON save: {ex.Message}");
        }

        OnSaveDataUpdated?.Invoke(currentSave);

        // Automatically sync to cloud if user is linked
        if (currentSave.isGoogleLinked && syncCloudIfLoggedIn && cloudProvider != null)
        {
            SyncNow(null);
        }
    }

    #region Google Account & Cloud Sync

    public void SignInWithGoogle(Action<bool, string> onComplete)
    {
        if (cloudProvider == null)
        {
            onComplete?.Invoke(false, "Cloud provider not ready.");
            return;
        }

        UpdateSyncStatus(CloudSyncStatus.SigningIn, "Signing into Google...");

        cloudProvider.SignIn((success, accountId, displayName) =>
        {
            if (success)
            {
                currentSave.isGoogleLinked = true;
                currentSave.googleUserId = accountId;
                currentSave.googleDisplayName = displayName;
                UpdateSyncStatus(CloudSyncStatus.Syncing, "Syncing data with cloud...");

                // Pull cloud data and merge
                cloudProvider.DownloadSave((dlSuccess, cloudData, msg) =>
                {
                    if (dlSuccess && cloudData != null)
                    {
                        Debug.Log("[SaveManager] Cloud save found. Merging local and cloud data...");
                        currentSave.Merge(cloudData);
                        currentSave.lastCloudSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                        // Write merged save back to local disk and cloud
                        ApplySaveToManagers();
                        Save(syncCloudIfLoggedIn: false);

                        cloudProvider.UploadSave(currentSave, (upSuccess, upMsg) =>
                        {
                            UpdateSyncStatus(CloudSyncStatus.Synced, "Cloud sync complete!");
                            onComplete?.Invoke(true, "Signed in & progress synchronized!");
                        });
                    }
                    else
                    {
                        // No cloud save exists yet; upload local save
                        Debug.Log("[SaveManager] No cloud save found. Uploading local save as cloud base...");
                        currentSave.lastCloudSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        Save(syncCloudIfLoggedIn: false);

                        cloudProvider.UploadSave(currentSave, (upSuccess, upMsg) =>
                        {
                            UpdateSyncStatus(CloudSyncStatus.Synced, "Cloud save created!");
                            onComplete?.Invoke(true, "Signed in & progress backed up!");
                        });
                    }
                });
            }
            else
            {
                UpdateSyncStatus(CloudSyncStatus.Failed, "Google Sign-In failed.");
                onComplete?.Invoke(false, "Failed to sign into Google.");
            }
        });
    }

    public void SignOutGoogle(Action onComplete)
    {
        if (cloudProvider == null)
        {
            onComplete?.Invoke();
            return;
        }

        cloudProvider.SignOut(() =>
        {
            if (currentSave != null)
            {
                currentSave.isGoogleLinked = false;
                currentSave.googleUserId = "";
                currentSave.googleDisplayName = "";
                Save(syncCloudIfLoggedIn: false);
            }

            UpdateSyncStatus(CloudSyncStatus.NotSignedIn, "Logged out of Google.");
            onComplete?.Invoke();
        });
    }

    public void SyncNow(Action<bool, string> onComplete)
    {
        if (!IsGoogleLinked || cloudProvider == null)
        {
            onComplete?.Invoke(false, "Not signed in to Google.");
            return;
        }

        UpdateSyncStatus(CloudSyncStatus.Syncing, "Syncing with cloud...");

        cloudProvider.DownloadSave((dlSuccess, cloudData, dlMsg) =>
        {
            if (dlSuccess && cloudData != null)
            {
                currentSave.Merge(cloudData);
                ApplySaveToManagers();
            }

            currentSave.lastCloudSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save(syncCloudIfLoggedIn: false);

            cloudProvider.UploadSave(currentSave, (upSuccess, upMsg) =>
            {
                if (upSuccess)
                {
                    UpdateSyncStatus(CloudSyncStatus.Synced, "Cloud Synced Just Now");
                    onComplete?.Invoke(true, "Cloud sync successful!");
                }
                else
                {
                    UpdateSyncStatus(CloudSyncStatus.Failed, "Cloud upload failed.");
                    onComplete?.Invoke(false, upMsg);
                }
            });
        });
    }

    private void UpdateSyncStatus(CloudSyncStatus status, string message)
    {
        currentSyncStatus = status;
        OnCloudSyncStatusChanged?.Invoke(status, message);
    }

    #endregion
}

