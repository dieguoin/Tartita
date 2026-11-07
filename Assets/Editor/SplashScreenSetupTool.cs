#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public static class SplashScreenSetupTool
{
    private const string VideoPath = "Assets/Videos/kling_20261108_VIDEO_Vertical_9_1358_0.mp4";
    private const string SplashScenePath = "Assets/Scenes/SplashScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Tartita/Setup Splash Screen Scene")]
    public static void SetupSplashScene()
    {
        Debug.Log("[SplashScreenSetup] Starting automated Splash Scene creation...");

        // Ensure folders exist
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // 1. Create a new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera Setup
        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        Camera cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cameraGO.AddComponent<AudioListener>();

        // 3. EventSystem Setup
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        // 4. Canvas Setup
        GameObject canvasGO = new GameObject("SplashCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // 5. Black Background Image
        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgGO.GetComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = false;

        // 6. Video Display RawImage (with zoom to crop out watermarks)
        GameObject videoDisplayGO = new GameObject("VideoDisplay", typeof(RectTransform), typeof(RawImage));
        videoDisplayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform displayRect = videoDisplayGO.GetComponent<RectTransform>();
        displayRect.anchorMin = Vector2.zero;
        displayRect.anchorMax = Vector2.one;
        displayRect.sizeDelta = Vector2.zero;
        displayRect.localScale = new Vector3(1.25f, 1.25f, 1f); // 25% zoom hides corner watermarks
        RawImage rawImage = videoDisplayGO.GetComponent<RawImage>();
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        // 7. Video Player & Controller Setup
        GameObject controllerGO = new GameObject("SplashController");
        VideoPlayer videoPlayer = controllerGO.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        // Load VideoClip
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        if (clip != null)
        {
            videoPlayer.clip = clip;
            Debug.Log("[SplashScreenSetup] Loaded video clip: " + VideoPath);
        }
        else
        {
            Debug.LogWarning("[SplashScreenSetup] Video clip not found or still importing at: " + VideoPath);
        }

        SplashScreenController splashController = controllerGO.AddComponent<SplashScreenController>();

        // Wire references via SerializedObject
        SerializedObject so = new SerializedObject(splashController);
        so.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        so.FindProperty("displayImage").objectReferenceValue = rawImage;
        so.FindProperty("fadeCanvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("targetPlayDuration").floatValue = 0.5f;
        so.FindProperty("holdLastFrameDuration").floatValue = 1.0f;
        so.FindProperty("fadeDuration").floatValue = 0.2f;
        so.FindProperty("zoomFactor").floatValue = 1.25f;
        so.FindProperty("nextSceneName").stringValue = "MainMenu";
        so.FindProperty("allowTapToSkip").boolValue = true;
        so.ApplyModifiedProperties();

        // 8. Save Scene
        EditorSceneManager.SaveScene(scene, SplashScenePath);
        Debug.Log("[SplashScreenSetup] Saved scene to " + SplashScenePath);

        // 9. Update Build Settings (put SplashScene at index 0)
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SplashScreenSetup] Splash Scene setup completed successfully!");
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // 0. Splash Scene (first scene right after Unity logo)
        scenes.Add(new EditorBuildSettingsScene(SplashScenePath, true));

        // 1. MainMenu
        if (File.Exists(MainMenuScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
        }

        // 2. SampleScene
        if (File.Exists(SampleScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(SampleScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SplashScreenSetup] Updated EditorBuildSettings.scenes: SplashScene is now Scene 0.");
    }
}
#endif

