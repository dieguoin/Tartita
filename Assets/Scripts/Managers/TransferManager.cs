using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransferManager : MonoBehaviour
{
    public static TransferManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
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

    private Sprite cakeImage;
    private int coins;
    public const int MAX_COINS = 9999;

    public void SetImage(Sprite image)
    {
        cakeImage = image;
    }

    public Sprite GetImage()
    {
        return cakeImage;
    }

    public void SetCoins(int amount, bool save = true)
    {
        coins = Mathf.Clamp(amount, 0, MAX_COINS);
        if (UIGameManager.instance != null)
        {
            UIGameManager.instance.UpdateNumber();
        }
        if (UIManager.instance != null)
        {
            UIManager.instance.PrintText();
        }
        if (save && SaveManager.instance != null)
        {
            SaveManager.instance.Save();
        }
    }

    public void AddCoin()
    {
        AddCoins(1);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins = Mathf.Min(MAX_COINS, coins + amount);
        if (UIGameManager.instance != null)
        {
            UIGameManager.instance.UpdateNumber();
        }
        if (UIManager.instance != null)
        {
            UIManager.instance.PrintText();
        }
        if (SaveManager.instance != null)
        {
            SaveManager.instance.Save();
        }
    }

    public int GetCoins()
    {
        return coins;
    }

    public void Pay(int price)
    {
        coins = Mathf.Max(0, coins - price);
        if (UIManager.instance != null)
        {
            UIManager.instance.PrintText();
        }
        if (SaveManager.instance != null)
        {
            SaveManager.instance.Save();
        }
    }
}
