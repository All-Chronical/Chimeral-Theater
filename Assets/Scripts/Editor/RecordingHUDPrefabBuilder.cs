using ChimeralTheater.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChimeralTheater.Editor
{
    /// <summary>
    /// Builds the RecordingHUD prefab: a small floating panel with a single large text, plus a
    /// FloatingUIFollower set to ride in front of the head. Only creates the prefab when missing,
    /// like the dashboard builder. The HUD binds itself to the scene's FilmDirector at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class RecordingHUDPrefabBuilder
    {
        private const string PrefabFolderPath = "Assets/Prefabs";
        private const string PrefabPath = "Assets/Prefabs/RecordingHUD.prefab";

        static RecordingHUDPrefabBuilder()
        {
            EditorApplication.delayCall += EnsurePrefabAndSceneSetup;
        }

        [MenuItem("Chimeral/Build Recording HUD Prefab and Scene Instance")]
        public static void BuildManual()
        {
            EnsurePrefabAndSceneSetup();
        }

        private static void EnsurePrefabAndSceneSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                prefab = CreatePrefab();

            SetupSceneInstance(prefab);
        }

        public static GameObject CreatePrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolderPath))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 24);

            GameObject root = new GameObject("RecordingHUD");
            RecordingHUD hud = root.AddComponent<RecordingHUD>();

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400f, 160f);
            panel.transform.localScale = Vector3.one * 0.001f;

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            bg.raycastTarget = false;

            FloatingUIFollower follower = panel.AddComponent<FloatingUIFollower>();
            follower.AnchorOffset = new Vector3(0f, 0.2f, 1.2f);
            follower.FollowSharpness = 8f;

            GameObject textObj = new GameObject("Display Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.GetComponent<Text>();
            text.text = "3";
            text.font = font;
            text.fontSize = 110;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            hud.Panel = panel;
            hud.DisplayText = text;
            hud.BackgroundImage = bg;
            hud.Follower = follower;

            panel.SetActive(false);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RecordingHUDPrefabBuilder] Created prefab at {PrefabPath}");
            return savedPrefab;
        }

        private static void SetupSceneInstance(GameObject prefab)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return;

            // The HUD binds to the director at runtime; the scene instance only needs to exist.
            RecordingHUD sceneHud = Object.FindFirstObjectByType<RecordingHUD>();
            if (sceneHud == null && prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "RecordingHUD";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[RecordingHUDPrefabBuilder] Added RecordingHUD instance to scene.");
            }
        }
    }
}
