using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

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

    private GameObject player;

    private void Start()
    {
        SetPlayer();
    }

    public void SetPlayer()
    {
        player = GameObject.Find("Player");
        if (player != null && TransferManager.instance != null && TransferManager.instance.GetImage() != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = TransferManager.instance.GetImage();
            }
        }
    }

    public void ChangeScene(string sceneName)
    {
        SetTime(1);
        if (SceneController.instance != null)
        {
            SceneController.instance.ChangeScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void StopTime()
    {
        SetTime(0);
    }

    public void SetTime(float time)
    {
        Time.timeScale = time;
    }
}
