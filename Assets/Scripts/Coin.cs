using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    private Camera mainCamera;
    private Collider2D coinCollider;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;

    private void Awake()
    {
        coinCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 5;
        }
        originalScale = transform.localScale;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Clean up coin once it falls safely below the visible camera view
        if (transform.position.y < mainCamera.transform.position.y - 8.5f)
        {
            DespawnCoin();
        }
    }

    /// <summary>
    /// Resets coin state when retrieved from the object pool.
    /// </summary>
    public void ResetCoin()
    {
        if (coinCollider == null) coinCollider = GetComponent<Collider2D>();
        if (coinCollider != null) coinCollider.enabled = true;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 5;
        }

        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (TransferManager.instance != null)
            {
                TransferManager.instance.AddCoin();
            }
            if (UIGameManager.instance != null)
            {
                UIGameManager.instance.OnCoinCollectedThisRun();
            }
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayCoin();
            }
            if (VFXManager.instance != null)
            {
                VFXManager.instance.PlayCoinSparkle(transform.position);
                VFXManager.instance.PlayCoinPopup(transform.position + Vector3.up * 0.25f);
            }
            DespawnCoin();
        }
    }

    private void DespawnCoin()
    {
        if (GroundManager.instance != null)
        {
            GroundManager.instance.DespawnCoin(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
