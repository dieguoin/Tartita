using System;

public enum CloudSyncStatus
{
    NotSignedIn,
    SigningIn,
    Synced,
    Syncing,
    Failed,
    Offline
}

public interface ICloudSaveProvider
{
    bool IsSignedIn { get; }
    string AccountDisplayName { get; }
    string AccountId { get; }

    void Initialize(Action<bool> onInitialized);
    void SignIn(Action<bool, string, string> onComplete); // success, accountId, displayName
    void SignOut(Action onComplete);
    void UploadSave(PlayerSaveData data, Action<bool, string> onComplete);
    void DownloadSave(Action<bool, PlayerSaveData, string> onComplete);
}

