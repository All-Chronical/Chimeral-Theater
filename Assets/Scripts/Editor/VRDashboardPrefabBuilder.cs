using ChimeralTheater.Locomotion;
using ChimeralTheater.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChimeralTheater.Editor
{
    [InitializeOnLoad]
    public static class VRDashboardPrefabBuilder
    {
        private const string PrefabFolderPath = "Assets/Prefabs";
        private const string PrefabPath = "Assets/Prefabs/VRDashboard.prefab";
        private const string InputAssetPath = "Assets/Input/ChimeralInput.inputactions";

        static VRDashboardPrefabBuilder()
        {
            EditorApplication.delayCall += EnsurePrefabAndSceneSetup;
        }

        [MenuItem("Chimeral/Build VR Dashboard Prefab and Scene Instance")]
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
            {
                prefab = CreatePrefab();
            }

            SetupSceneInstance(prefab);
        }

        public static GameObject CreatePrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 24);

            GameObject root = new GameObject("VRDashboard");
            VRDashboard dash = root.AddComponent<VRDashboard>();

            // Panel Root with World Space Canvas
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800f, 500f);
            panel.transform.localScale = Vector3.one * 0.001f;

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.11f, 0.13f, 0.95f);
            bg.raycastTarget = true;

            // Header Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(40f, -30f);
            titleRect.sizeDelta = new Vector2(500f, 50f);

            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = "Settings";
            titleText.font = font;
            titleText.fontSize = 34;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.raycastTarget = false;

            // Settings Content
            GameObject settingsContent = new GameObject("Settings Content", typeof(RectTransform));
            settingsContent.transform.SetParent(panel.transform, false);
            RectTransform settingsRect = settingsContent.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0f, 0.2f);
            settingsRect.anchorMax = new Vector2(1f, 0.82f);
            settingsRect.offsetMin = new Vector2(40f, 0f);
            settingsRect.offsetMax = new Vector2(-40f, 0f);

            Slider locomotionSlider = CreateSettingSlider(settingsContent.transform, "Locomotion Speed", 0.3f, font, out Text locoValText);
            Slider rotationSlider = CreateSettingSlider(settingsContent.transform, "Rotation Speed", 0.7f, font, out Text rotValText);

            // Films Content
            GameObject filmsContent = new GameObject("Films Content", typeof(RectTransform));
            filmsContent.transform.SetParent(panel.transform, false);
            RectTransform filmsRect = filmsContent.GetComponent<RectTransform>();
            filmsRect.anchorMin = new Vector2(0f, 0.2f);
            filmsRect.anchorMax = new Vector2(1f, 0.82f);
            filmsRect.offsetMin = new Vector2(40f, 0f);
            filmsRect.offsetMax = new Vector2(-40f, 0f);
            filmsContent.SetActive(false);

            // Bottom Tabs
            float buttonWidth = 400f;
            float buttonHeight = 70f;
            Button settingsTabBtn = CreateTabButton(panel.transform, "Settings", new Vector2(0f, 0f), new Vector2(buttonWidth, buttonHeight), font, out Image settingsTabImg);
            Button filmsTabBtn = CreateTabButton(panel.transform, "Films", new Vector2(buttonWidth, 0f), new Vector2(buttonWidth, buttonHeight), font, out Image filmsTabImg);

            // Laser
            GameObject laserObj = new GameObject("Laser", typeof(LineRenderer));
            laserObj.transform.SetParent(root.transform, false);
            LineRenderer laser = laserObj.GetComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.startWidth = 0.005f;
            laser.endWidth = 0.0015f;

            Material laserMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/LaserMaterial.mat");
            if (laserMat != null)
                laser.sharedMaterial = laserMat;

            laser.startColor = new Color(0.1f, 0.85f, 1f, 0.9f);
            laser.endColor = new Color(0.1f, 0.85f, 1f, 0.2f);

            // Wire all references into VRDashboard component
            dash.Panel = panel;
            dash.Laser = laser;
            dash.TabTitleText = titleText;
            dash.SettingsContent = settingsContent;
            dash.FilmsContent = filmsContent;
            dash.SettingsTabButton = settingsTabBtn;
            dash.FilmsTabButton = filmsTabBtn;
            dash.SettingsTabImage = settingsTabImg;
            dash.FilmsTabImage = filmsTabImg;
            dash.LocomotionSlider = locomotionSlider;
            dash.RotationSlider = rotationSlider;
            dash.LocomotionValueText = locoValText;
            dash.RotationValueText = rotValText;

            // Visuals start disabled
            panel.SetActive(false);
            laserObj.SetActive(false);

            // Save Prefab Asset
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VRDashboardPrefabBuilder] Created prefab at {PrefabPath}");
            return savedPrefab;
        }

        private static Slider CreateSettingSlider(Transform parent, string label, float normalizedY, Font font, out Text valueLabel)
        {
            float anchorY = 1f - normalizedY;

            // Label
            GameObject labelObj = new GameObject(label + " Text", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(parent, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, anchorY);
            labelRect.anchorMax = new Vector2(0f, anchorY);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(300f, 40f);
            labelRect.anchoredPosition = Vector2.zero;

            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = label;
            labelText.font = font;
            labelText.fontSize = 24;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.raycastTarget = false;

            // Slider
            GameObject sliderObj = new GameObject(label + " Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, anchorY);
            sliderRect.anchorMax = new Vector2(0.82f, anchorY);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(0f, 28f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 1f;
            slider.maxValue = 5f;
            slider.value = 1f;
            slider.wholeNumbers = false;

            // Background Track
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            Stretch(bgRect);
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.25f, 0.27f, 0.32f, 1f);
            bgImg.raycastTarget = true;
            slider.targetGraphic = bgImg;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            Stretch(fillRect);
            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 0.95f, 1f);
            fillImg.raycastTarget = false;
            slider.fillRect = fillRect;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 36f);
            Image handleImg = handleObj.GetComponent<Image>();
            handleImg.color = Color.white;
            handleImg.raycastTarget = true;
            slider.handleRect = handleRect;

            // Value text
            GameObject valObj = new GameObject(label + " Value Text", typeof(RectTransform), typeof(Text));
            valObj.transform.SetParent(parent, false);
            RectTransform valRect = valObj.GetComponent<RectTransform>();
            valRect.anchorMin = new Vector2(1f, anchorY);
            valRect.anchorMax = new Vector2(1f, anchorY);
            valRect.pivot = new Vector2(1f, 0.5f);
            valRect.sizeDelta = new Vector2(90f, 40f);
            valRect.anchoredPosition = Vector2.zero;

            valueLabel = valObj.GetComponent<Text>();
            valueLabel.text = "1.0x";
            valueLabel.font = font;
            valueLabel.fontSize = 24;
            valueLabel.color = Color.white;
            valueLabel.alignment = TextAnchor.MiddleRight;
            valueLabel.raycastTarget = false;

            return slider;
        }

        private static Button CreateTabButton(Transform parent, string label, Vector2 position, Vector2 size, Font font, out Image buttonImage)
        {
            GameObject buttonObj = new GameObject(label + " Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            buttonImage.raycastTarget = true;

            Button button = buttonObj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            colors.highlightedColor = new Color(0.3f, 0.32f, 0.38f, 1f);
            colors.pressedColor = new Color(0.18f, 0.18f, 0.2f, 1f);
            button.colors = colors;

            GameObject textObj = new GameObject(label + " Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(buttonObj.transform, false);
            Stretch(textObj.GetComponent<RectTransform>());

            Text text = textObj.GetComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetupSceneInstance(GameObject prefab)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return;

            // Remove any VRDashboard script from XR Origin if present
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                VRDashboard oldOnXrOrigin = xrOrigin.GetComponent<VRDashboard>();
                if (oldOnXrOrigin != null)
                {
                    Object.DestroyImmediate(oldOnXrOrigin);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            // Find or instantiate VRDashboard prefab instance in scene
            VRDashboard sceneDash = Object.FindFirstObjectByType<VRDashboard>();
            if (sceneDash == null && prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "VRDashboard";
                sceneDash = instance.GetComponent<VRDashboard>();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (sceneDash != null)
            {
                // Wire inspector references
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
                VRLocomotion loco = Object.FindFirstObjectByType<VRLocomotion>();
                Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                GameObject leftHand = GameObject.Find("Left Hand");
                GameObject rightHand = GameObject.Find("Right Hand");

                sceneDash.InputActions = actions;
                sceneDash.Locomotion = loco;
                sceneDash.Hmd = cam;
                sceneDash.LeftHand = leftHand != null ? leftHand.transform : null;
                sceneDash.RightHand = rightHand != null ? rightHand.transform : null;

                EditorUtility.SetDirty(sceneDash);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[VRDashboardPrefabBuilder] Configured VRDashboard prefab instance in scene.");
            }
        }
    }
}
