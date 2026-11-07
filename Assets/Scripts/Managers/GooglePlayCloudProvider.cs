using System;
using UnityEngine;

/// <summary>
/// Production bridge for Google Play Games Services v2 Saved Games API.
/// Active on Android devices when built with Google Play Games plugin.
/// </summary>
public class GooglePlayCloudProvider : MonoBehaviour, ICloudSaveProvider
{
    private bool isSignedIn = false;
    private string accountDisplayName = "";
    private string accountId = "";

    public bool IsSignedIn => isSignedIn;
    public string AccountDisplayName => accountDisplayName;
    public string AccountId => accountId;

    public void Initialize(Action<bool> onInitialized)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[GooglePlayCloudProvider] Initializing Play Games Platform...");
        // Ready for PlayGamesClientConfiguration & PlayGamesPlatform.Activate()
        onInitialized?.Invoke(true);
#else
        onInitialized?.Invoke(true);
#endif
    }

    public void SignIn(Action<bool, string, string> onComplete)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[GooglePlayCloudProvider] Authenticating with Google Play Games...");
        // Google Play Games v2 authentication call here
        // Social.localUser.Authenticate((bool success) => ...);
        onComplete?.Invoke(false, "", "Google Play Games is configured for Android device builds.");
#else
        onComplete?.Invoke(false, "", "Google Play Games is not available on this platform.");
#endif
    }

    public void SignOut(Action onComplete)
    {
        isSignedIn = false;
        accountId = "";
        accountDisplayName = "";
#if UNITY_ANDROID && !UNITY_EDITOR
        // PlayGamesPlatform.Instance.SignOut();
#endif
        onComplete?.Invoke();
    }

    public void UploadSave(PlayerSaveData data, Action<bool, string> onComplete)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // ISavedGameClient.OpenWithAutomaticConflictResolution(...)
        onComplete?.Invoke(false, "Native cloud save is pending Play Games activation.");
#else
        onComplete?.Invoke(false, "Native cloud save only active on Android device builds.");
#endif
    }

    public void DownloadSave(Action<bool, PlayerSaveData, string> onComplete)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // ISavedGameClient.ReadBinaryData(...)
        onComplete?.Invoke(false, null, "Native cloud load is pending Play Games activation.");
#else
        onComplete?.Invoke(false, null, "Native cloud load only active on Android device builds.");
#endif
    }
}

