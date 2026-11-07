using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class MockGoogleCloudProvider : MonoBehaviour, ICloudSaveProvider
{
    private bool isSignedIn = false;
    private string accountId = "";
    private string accountDisplayName = "";
    private string mockCloudFilePath;

    public bool IsSignedIn => isSignedIn;
    public string AccountDisplayName => accountDisplayName;
    public string AccountId => accountId;

    public void Initialize(Action<bool> onInitialized)
    {
        mockCloudFilePath = Path.Combine(Application.persistentDataPath, "cloud_mock_storage.json");
        onInitialized?.Invoke(true);
    }

    public void SignIn(Action<bool, string, string> onComplete)
    {
        StartCoroutine(SimulateSignIn(onComplete));
    }

    private IEnumerator SimulateSignIn(Action<bool, string, string> onComplete)
    {
        yield return new WaitForSecondsRealtime(0.6f);
        isSignedIn = true;
        accountId = "google_user_tartita_9876";
        accountDisplayName = "Google Player";
        Debug.Log($"[MockGoogleCloudProvider] Signed in as: {accountDisplayName} ({accountId})");
        onComplete?.Invoke(true, accountId, accountDisplayName);
    }

    public void SignOut(Action onComplete)
    {
        isSignedIn = false;
        accountId = "";
        accountDisplayName = "";
        Debug.Log("[MockGoogleCloudProvider] Signed out.");
        onComplete?.Invoke();
    }

    public void UploadSave(PlayerSaveData data, Action<bool, string> onComplete)
    {
        StartCoroutine(SimulateUpload(data, onComplete));
    }

    private IEnumerator SimulateUpload(PlayerSaveData data, Action<bool, string> onComplete)
    {
        yield return new WaitForSecondsRealtime(0.4f);
        try
        {
            if (string.IsNullOrEmpty(mockCloudFilePath))
            {
                mockCloudFilePath = Path.Combine(Application.persistentDataPath, "cloud_mock_storage.json");
            }

            string json = data.ToJson(true);
            File.WriteAllText(mockCloudFilePath, json);
            Debug.Log($"[MockGoogleCloudProvider] Uploaded cloud save successfully to: {mockCloudFilePath}");
            onComplete?.Invoke(true, "Cloud save updated successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MockGoogleCloudProvider] Upload error: {ex.Message}");
            onComplete?.Invoke(false, ex.Message);
        }
    }

    public void DownloadSave(Action<bool, PlayerSaveData, string> onComplete)
    {
        StartCoroutine(SimulateDownload(onComplete));
    }

    private IEnumerator SimulateDownload(Action<bool, PlayerSaveData, string> onComplete)
    {
        yield return new WaitForSecondsRealtime(0.4f);
        try
        {
            if (string.IsNullOrEmpty(mockCloudFilePath))
            {
                mockCloudFilePath = Path.Combine(Application.persistentDataPath, "cloud_mock_storage.json");
            }

            if (!File.Exists(mockCloudFilePath))
            {
                onComplete?.Invoke(true, null, "No existing cloud save found.");
                yield break;
            }

            string json = File.ReadAllText(mockCloudFilePath);
            PlayerSaveData data = PlayerSaveData.FromJson(json);
            Debug.Log($"[MockGoogleCloudProvider] Downloaded cloud save successfully.");
            onComplete?.Invoke(true, data, "Downloaded cloud save successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MockGoogleCloudProvider] Download error: {ex.Message}");
            onComplete?.Invoke(false, null, ex.Message);
        }
    }
}

