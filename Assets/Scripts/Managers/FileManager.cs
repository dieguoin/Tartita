using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class CakeSaveEntry
{
    public string cakeName;
    public string state;

    public CakeSaveEntry() {}
    public CakeSaveEntry(string name, string state)
    {
        this.cakeName = name;
        this.state = state;
    }
}

[System.Serializable]
public class DataSave
{
    public List<CakeSaveEntry> cakeEntries = new List<CakeSaveEntry>();
    public string[] cakesStates;

    public DataSave() {}
    public DataSave(int number) 
    {
        cakesStates = new string[number];
        cakeEntries = new List<CakeSaveEntry>();
    }
}

public class FileManager : MonoBehaviour
{
    public static FileManager instance;

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
        if (instance == this)
        {
            instance = null;
        }
    }

    [Header("Save File")]
    [SerializeField] private string saveFileName = "SaveStates";
    private string filePath;

    private void Start()
    {
        filePath = Application.persistentDataPath + "/" + saveFileName + ".json";
        LoadCakesState();
    }

    public List<string> GetUnlockedCakeNames()
    {
        List<string> unlocked = new List<string>();
        if (UIManager.instance != null && UIManager.instance.cakeList != null)
        {
            foreach (Cake cake in UIManager.instance.cakeList)
            {
                if (cake != null && cake.GetState() == Cake.State.Unlocked)
                {
                    unlocked.Add(cake.name);
                }
            }
        }
        return unlocked;
    }

    public void ApplyUnlockedCakes(List<string> unlockedNames)
    {
        if (UIManager.instance == null || UIManager.instance.cakeList == null) return;

        HashSet<string> unlockedSet = new HashSet<string>(unlockedNames ?? new List<string>());

        foreach (Cake cake in UIManager.instance.cakeList)
        {
            if (cake == null) continue;

            if (cake.GetPrice() == 0 || unlockedSet.Contains(cake.name))
            {
                cake.Unlock();
            }
            else
            {
                cake.Lock();
            }
        }
    }

    public void LoadCakesState()
    {
        if (UIManager.instance == null || UIManager.instance.cakeList == null) return;

        // If SaveManager has loaded data, apply from it directly
        if (SaveManager.instance != null && SaveManager.instance.CurrentSave != null && SaveManager.instance.CurrentSave.unlockedCakeNames != null && SaveManager.instance.CurrentSave.unlockedCakeNames.Count > 0)
        {
            ApplyUnlockedCakes(SaveManager.instance.CurrentSave.unlockedCakeNames);
            return;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Application.persistentDataPath + "/" + saveFileName + ".json";
        }

        if (!File.Exists(filePath))
        {
            foreach (Cake cake in UIManager.instance.cakeList)
            {
                if (cake != null)
                {
                    if (cake.GetPrice() == 0) cake.Unlock();
                    else cake.Lock();
                }
            }
            SaveCakesState();
            return;
        }

        try
        {
            string saveFile = File.ReadAllText(filePath);
            DataSave ds = JsonUtility.FromJson<DataSave>(saveFile);
            if (ds == null) return;

            Dictionary<string, string> stateDict = new Dictionary<string, string>();
            if (ds.cakeEntries != null && ds.cakeEntries.Count > 0)
            {
                foreach (var entry in ds.cakeEntries)
                {
                    if (!string.IsNullOrEmpty(entry.cakeName) && !stateDict.ContainsKey(entry.cakeName))
                    {
                        stateDict[entry.cakeName] = entry.state;
                    }
                }
            }
            else if (ds.cakesStates != null && ds.cakesStates.Length > 0)
            {
                for (int i = 0; i < UIManager.instance.cakeList.Count && i < ds.cakesStates.Length; i++)
                {
                    Cake cake = UIManager.instance.cakeList[i];
                    if (cake != null)
                    {
                        stateDict[cake.name] = ds.cakesStates[i];
                    }
                }
            }

            foreach (Cake cake in UIManager.instance.cakeList)
            {
                if (cake == null) continue;

                if (stateDict.TryGetValue(cake.name, out string stateStr))
                {
                    if (stateStr == "Unlocked" || cake.GetPrice() == 0)
                    {
                        cake.Unlock();
                    }
                    else
                    {
                        cake.Lock();
                    }
                }
                else
                {
                    if (cake.GetPrice() == 0)
                    {
                        cake.Unlock();
                    }
                    else
                    {
                        cake.Lock();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileManager] Error loading cakes state: {ex.Message}");
        }
    }

    public void SaveCakesState()
    {
        if (UIManager.instance == null || UIManager.instance.cakeList == null) return;

        // Notify SaveManager to commit new unlocked cakes into JSON and cloud
        if (SaveManager.instance != null)
        {
            SaveManager.instance.Save();
        }

        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Application.persistentDataPath + "/" + saveFileName + ".json";
        }

        DataSave ds = new DataSave(UIManager.instance.cakeList.Count);
        for (int i = 0; i < UIManager.instance.cakeList.Count; i++)
        {
            Cake cake = UIManager.instance.cakeList[i];
            if (cake == null) continue;

            string stateStr = cake.GetState().ToString();
            ds.cakeEntries.Add(new CakeSaveEntry(cake.name, stateStr));
            ds.cakesStates[i] = stateStr;
        }

        try
        {
            string json = JsonUtility.ToJson(ds, true);
            File.WriteAllText(filePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileManager] Error saving cakes state: {ex.Message}");
        }
    }
}
