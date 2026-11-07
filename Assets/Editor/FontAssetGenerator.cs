#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class FontAssetGenerator
{
    static FontAssetGenerator()
    {
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        GenerateFontAssets();
        ConfigureAndroidSettings();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Tartita/Configure Android Settings (Google Play Ready)")]
    public static void ConfigureAndroidSettings()
    {
        PlayerSettings.companyName = "Dieguoin";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.dieguoin.tartita");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        EditorUserBuildSettings.buildAppBundle = true;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/cakes/Cake_Fresa.png");
        if (icon != null)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Any);
        }

        Debug.Log("Configured Android PlayerSettings: Company 'Dieguoin', ID 'com.dieguoin.tartita', Target SDK 34, IL2CPP + ARM64, Portrait Orientation, Cake_Fresa Icon set!");
    }

    [MenuItem("Tools/Tartita/Generate Font Assets")]
    public static void GenerateFontAssets()
    {
        string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" });
        foreach (string guid in fontGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".ttf") && !path.EndsWith(".otf")) continue;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null) continue;

            string assetPath = path.Substring(0, path.LastIndexOf('.')) + " SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && existing.atlasTexture != null)
            {
                Debug.Log($"Font asset already exists: {assetPath}");
                continue;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
            if (fontAsset != null)
            {
                AssetDatabase.CreateAsset(fontAsset, assetPath);

                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        Texture2D tex = fontAsset.atlasTextures[i];
                        if (tex != null)
                        {
                            tex.name = $"{font.name} Atlas {i}";
                            AssetDatabase.AddObjectToAsset(tex, fontAsset);
                        }
                    }
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = $"{font.name} Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Successfully created TMP Font Asset: {assetPath}");
            }
        }
    }
}
#endif
