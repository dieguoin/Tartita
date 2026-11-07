using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public int coins = 0;
    public int highScore = 0;
    public List<string> unlockedCakeNames = new List<string>();
    public string equippedCakeName = "";
    public long lastSaveTimestamp = 0;

    // Audio Preferences
    public bool isMusicMuted = false;
    public bool isSfxMuted = false;

    // Google Account & Cloud Sync
    public bool isGoogleLinked = false;
    public string googleUserId = "";
    public string googleDisplayName = "";
    public long lastCloudSyncTimestamp = 0;

    public PlayerSaveData()
    {
        lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        unlockedCakeNames = new List<string>();
    }

    public string ToJson(bool prettyPrint = true)
    {
        return JsonUtility.ToJson(this, prettyPrint);
    }

    public static PlayerSaveData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new PlayerSaveData();
        try
        {
            return JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerSaveData] JSON Deserialization error: {ex.Message}");
            return new PlayerSaveData();
        }
    }

    /// <summary>
    /// Performs a lossless merge of this save with remote cloud data.
    /// Takes max coins, max high score, and the union of all unlocked cakes.
    /// </summary>
    public void Merge(PlayerSaveData remote)
    {
        if (remote == null) return;

        coins = Mathf.Clamp(Mathf.Max(coins, remote.coins), 0, 9999);
        highScore = Mathf.Max(highScore, remote.highScore);

        if (remote.unlockedCakeNames != null)
        {
            foreach (string cakeName in remote.unlockedCakeNames)
            {
                if (!string.IsNullOrEmpty(cakeName) && !unlockedCakeNames.Contains(cakeName))
                {
                    unlockedCakeNames.Add(cakeName);
                }
            }
        }

        if (string.IsNullOrEmpty(equippedCakeName) && !string.IsNullOrEmpty(remote.equippedCakeName))
        {
            equippedCakeName = remote.equippedCakeName;
        }

        lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

