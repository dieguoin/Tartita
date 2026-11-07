using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SplashScreenController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Timing")]
    [Tooltip("Target duration in seconds for the video animation (plays faster to fit this duration).")]
    [SerializeField] private float targetPlayDuration = 0.5f;

    [Tooltip("Duration to stay frozen on the last frame before transitioning.")]
    [SerializeField] private float holdLastFrameDuration = 1.0f;

    [Tooltip("Fade out duration to black before loading the next scene.")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Zoom & Framing")]
    [Tooltip("Zoom factor to hide edge watermarks (e.g. 1.25 = 25% zoom).")]
    [SerializeField] private float zoomFactor = 1.25f;

    [Header("Navigation")]
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool allowTapToSkip = true;

    private bool isTransitioning = false;
    private RenderTexture renderTexture;

    private void Awake()
    {
        // Apply zoom to RawImage to hide any corner watermarks off-screen
        if (displayImage != null)
        {
            displayImage.rectTransform.localScale = new Vector3(zoomFactor, zoomFactor, 1f);

            // Automatically set up high quality RenderTexture if none assigned
            if (videoPlayer != null && videoPlayer.targetTexture == null)
            {
                renderTexture = new RenderTexture(1080, 1920, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                videoPlayer.targetTexture = renderTexture;
                displayImage.texture = renderTexture;
            }
        }
    }

    private void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        StartCoroutine(PlaySplashRoutine());
    }

    private void Update()
    {
        if (allowTapToSkip && !isTransitioning)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                StopAllCoroutines();
                StartCoroutine(TransitionToNextScene());
            }
        }
    }

    private IEnumerator PlaySplashRoutine()
    {
        if (videoPlayer == null)
        {
            yield return StartCoroutine(TransitionToNextScene());
            yield break;
        }

        // Wait for video player to prepare
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // Calculate playback speed: 3.0s clip in 0.5s -> speed = 6.0x
        double clipDuration = videoPlayer.length > 0 ? videoPlayer.length : 3.0;
        float speed = (float)(clipDuration / Mathf.Max(0.1f, targetPlayDuration));
        videoPlayer.playbackSpeed = speed;

        videoPlayer.Play();

        // Let the video play for targetPlayDuration
        float elapsed = 0f;
        while (elapsed < targetPlayDuration && videoPlayer.isPlaying)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Freeze on the last frame
        videoPlayer.Pause();

        // Hold static on last frame
        yield return new WaitForSeconds(holdLastFrameDuration);

        // Smooth fade out & change scene
        yield return StartCoroutine(TransitionToNextScene());
    }

    private IEnumerator TransitionToNextScene()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (fadeCanvasGroup != null && fadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}

