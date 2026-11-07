#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VFXSetupTool
{
    [MenuItem("Tools/Tartita/Setup VFX and Resize Platforms")]
    public static void RunSetup()
    {
        Debug.Log("Starting VFX Setup and Platform Resizing...");
        
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VFX"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");
        }

        // Materials
        Material matStar = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_VFX_Star.mat");
        Material matCrumb = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_VFX_Crumb.mat");
        Material matJelly = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_VFX_JellyDrop.mat");
        Material matCookie = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_VFX_CookieChunk.mat");

        // 1. Coin Sparkle Prefab
        GameObject coinVfx = CreateParticleSystemGO("VFX_CoinSparkle", matStar, ps =>
        {
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 0.95f, 0.6f, 1f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        });
        string coinPath = "Assets/Prefabs/VFX/VFX_CoinSparkle.prefab";
        GameObject coinPrefab = PrefabUtility.SaveAsPrefabAsset(coinVfx, coinPath);
        Object.DestroyImmediate(coinVfx);

        // 2. Jump Puff Prefab
        GameObject jumpVfx = CreateParticleSystemGO("VFX_JumpPuff", matCrumb, ps =>
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.95f, 0.85f), new Color(1f, 0.95f, 0.85f, 0.6f));
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.6f);
            sizeCurve.AddKey(1f, 1.2f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        });
        string jumpPath = "Assets/Prefabs/VFX/VFX_JumpPuff.prefab";
        GameObject jumpPrefab = PrefabUtility.SaveAsPrefabAsset(jumpVfx, jumpPath);
        Object.DestroyImmediate(jumpVfx);

        // 3. Jelly Splash Prefab
        GameObject jellyVfx = CreateParticleSystemGO("VFX_JellySplash", matJelly, ps =>
        {
            var main = ps.main;
            main.duration = 0.7f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.gravityModifier = 1.6f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.65f, 1f), new Color(1f, 0.6f, 0.8f, 1f));
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;
        });
        string jellyPath = "Assets/Prefabs/VFX/VFX_JellySplash.prefab";
        GameObject jellyPrefab = PrefabUtility.SaveAsPrefabAsset(jellyVfx, jellyPath);
        Object.DestroyImmediate(jellyVfx);

        // 4. Crumble Dust Prefab
        GameObject crumbleVfx = CreateParticleSystemGO("VFX_CrumbleDust", matCookie, ps =>
        {
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            main.gravityModifier = 2.2f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.62f, 0.38f, 1f), new Color(0.68f, 0.45f, 0.25f, 1f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.8f, 0.15f, 0.1f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-180f * Mathf.Deg2Rad, 180f * Mathf.Deg2Rad);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;
        });
        string crumblePath = "Assets/Prefabs/VFX/VFX_CrumbleDust.prefab";
        GameObject crumblePrefab = PrefabUtility.SaveAsPrefabAsset(crumbleVfx, crumblePath);
        Object.DestroyImmediate(crumbleVfx);

        Debug.Log("4 VFX Prefabs created successfully.");

        // 3. Scale Platform Prefabs down to 0.62
        string[] platformPrefabPaths = new string[]
        {
            "Assets/Prefabs/Floor.prefab",
            "Assets/Prefabs/Floor_Bouncy.prefab",
            "Assets/Prefabs/Floor_Moving.prefab",
            "Assets/Prefabs/Floor_Crumbling.prefab"
        };

        foreach (string pPath in platformPrefabPaths)
        {
            if (File.Exists(pPath))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(pPath);
                root.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                PrefabUtility.SaveAsPrefabAsset(root, pPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"Scaled {pPath} to (0.62, 0.62, 1.0)");
            }
        }

        // 4. Update SampleScene
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find or create VFXManager
        VFXManager vfxMgr = Object.FindFirstObjectByType<VFXManager>();
        if (vfxMgr == null)
        {
            GameObject vfxGO = new GameObject("VFXManager");
            vfxMgr = vfxGO.AddComponent<VFXManager>();
            Undo.RegisterCreatedObjectUndo(vfxGO, "Create VFXManager");
        }

        // Assign prefabs using SerializedObject
        SerializedObject soVfx = new SerializedObject(vfxMgr);
        soVfx.FindProperty("coinSparklePrefab").objectReferenceValue = coinPrefab;
        soVfx.FindProperty("jumpPuffPrefab").objectReferenceValue = jumpPrefab;
        soVfx.FindProperty("jellySplashPrefab").objectReferenceValue = jellyPrefab;
        soVfx.FindProperty("crumbleDustPrefab").objectReferenceValue = crumblePrefab;
        soVfx.ApplyModifiedProperties();

        // Scale initial platforms in scene
        string[] initialNames = new string[] { "1", "1 (1)", "1 (2)", "2" };
        GameObject[] allGOs = scene.GetRootGameObjects();
        foreach (var go in allGOs)
        {
            foreach (var initName in initialNames)
            {
                if (go.name == initName)
                {
                    go.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                    EditorUtility.SetDirty(go);
                    Debug.Log($"Scaled scene platform '{go.name}' to (0.62, 0.62, 1.0)");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("VFX Setup and Platform Resizing completed successfully!");
    }

    private static GameObject CreateParticleSystemGO(string name, Material mat, System.Action<ParticleSystem> configure)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && mat != null)
        {
            renderer.material = mat;
            renderer.sortingOrder = 10;
        }
        configure?.Invoke(ps);
        return go;
    }
}
#endif
