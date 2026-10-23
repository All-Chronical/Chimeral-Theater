using ChimeralTheater.Films;
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
            else if (IsPrefabStale(prefab))
            {
                // The prefab predates the films UI or the floating follower: rebuild it at the
                // same path so the asset guid stays put, then replace the scene instance so no
                // stale overrides survive.
                prefab = CreatePrefab();
                ReplaceSceneInstance(prefab);
            }

            SetupSceneInstance(prefab);
        }

        private static bool IsPrefabStale(GameObject prefab)
        {
            if (prefab.GetComponent<VRDashboard>() == null)
                return true;

            if (prefab.GetComponentInChildren<FloatingUIFollower>(true) == null)
                return true;

            if (prefab.GetComponentInChildren<FilmsPanel>(true) == null)
                return true;

            return false;
        }

        private static void ReplaceSceneInstance(GameObject prefab)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return;

            VRDashboard existing = Object.FindFirstObjectByType<VRDashboard>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
            }
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

            // Floating panel behaviour shared by every head/hand-following panel
            FloatingUIFollower follower = panel.AddComponent<FloatingUIFollower>();
            follower.AnchorOffset = new Vector3(0f, 0.15f, 0.25f);
            follower.FollowSharpness = 8f;

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

            FilmsPanel filmsPanel = BuildFilmsUI(filmsContent.transform, font);

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
            dash.Follower = follower;

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

        private static FilmsPanel BuildFilmsUI(Transform filmsContent, Font font)
        {
            // The panel script rides on the Films Content object itself.
            FilmsPanel films = filmsContent.gameObject.AddComponent<FilmsPanel>();

            // --- Takes view: scroll list of take rows plus an add button ---
            GameObject takesView = new GameObject("Takes View", typeof(RectTransform));
            takesView.transform.SetParent(filmsContent, false);
            Stretch(takesView.GetComponent<RectTransform>());

            RectTransform takesViewport;
            BuildScrollList(takesView.transform, "Takes List", font, out takesViewport);

            GameObject takeTemplate = BuildTakeRowTemplate(takesViewport, font);
            GameObject addTake = BuildAddButton(takesView.transform, font, out Image _);
            films.TakesView = takesView;
            films.TakesListContent = takesViewport;
            films.TakeRowTemplate = takeTemplate;
            films.AddTakeButton = addTake.GetComponent<Button>();

            // --- Take detail view: back, title, act rows, record and play ---
            GameObject detailView = new GameObject("Take Detail View", typeof(RectTransform));
            detailView.transform.SetParent(filmsContent, false);
            Stretch(detailView.GetComponent<RectTransform>());

            Button back = BuildPlainButton(detailView.transform, "Back", font,
                new Vector2(0f, 1f), new Vector2(140f, 60f), new Vector2(0f, 1f), new Vector2(-30f, -10f));

            GameObject titleObj = new GameObject("Take Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(detailView.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 60f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);

            Text title = titleObj.GetComponent<Text>();
            title.text = "Take 1";
            title.font = font;
            title.fontSize = 30;
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            title.raycastTarget = false;
            films.TakeTitleText = title;

            RectTransform actsViewport;
            GameObject actsListRoot = BuildScrollList(detailView.transform, "Acts List", font, out actsViewport);
            // Leave room at the bottom for the record and play buttons.
            RectTransform actsRootRect = actsListRoot.GetComponent<RectTransform>();
            actsRootRect.anchorMax = new Vector2(1f, 0.82f);

            GameObject actTemplate = BuildActRowTemplate(actsViewport, font);
            GameObject addAct = BuildAddButton(detailView.transform, font, out Image _);

            Button record = BuildPlainButton(detailView.transform, "Record", font,
                new Vector2(0f, 0f), new Vector2(240f, 70f), new Vector2(0f, 0f), new Vector2(30f, 10f));
            Button play = BuildPlainButton(detailView.transform, "Play", font,
                new Vector2(1f, 0f), new Vector2(240f, 70f), new Vector2(1f, 0f), new Vector2(-30f, 10f));

            films.TakeDetailView = detailView;
            films.ActsListContent = actsViewport;
            films.ActRowTemplate = actTemplate;
            films.AddActButton = addAct.GetComponent<Button>();
            films.BackButton = back;
            films.RecordButton = record;
            films.PlayButton = play;

            // --- Recording view: one giant stop button ---
            GameObject recordingView = new GameObject("Recording View", typeof(RectTransform));
            recordingView.transform.SetParent(filmsContent, false);
            Stretch(recordingView.GetComponent<RectTransform>());

            Button stop = BuildPlainButton(recordingView.transform, "STOP RECORDING", font,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            RectTransform stopRect = stop.GetComponent<RectTransform>();
            stopRect.anchorMin = new Vector2(0.1f, 0.1f);
            stopRect.anchorMax = new Vector2(0.9f, 0.9f);
            Image stopImage = stop.GetComponent<Image>();
            stopImage.color = new Color(0.85f, 0.15f, 0.15f, 0.95f);

            films.RecordingView = recordingView;
            films.StopRecordingButton = stop;

            // Wire the panel to the director that the scene setup creates.
            films.Director = Object.FindFirstObjectByType<FilmDirector>();

            // The detail and recording views start hidden; the panel shows the right one itself.
            detailView.SetActive(false);
            recordingView.SetActive(false);

            return films;
        }

        private static GameObject BuildScrollList(Transform parent, string name, Font font, out RectTransform viewport)
        {
            GameObject listRoot = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            listRoot.transform.SetParent(parent, false);
            RectTransform listRect = listRoot.GetComponent<RectTransform>();
            Stretch(listRect);

            // Right-side scrollbar
            GameObject barObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barObject.transform.SetParent(listRoot.transform, false);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.sizeDelta = new Vector2(30f, 0f);

            Image barImage = barObject.GetComponent<Image>();
            barImage.color = new Color(0.15f, 0.16f, 0.18f, 0.9f);
            barImage.raycastTarget = true;

            GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(barObject.transform, false);
            RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(4f, 4f);
            slidingRect.offsetMax = new Vector2(-4f, -4f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            Stretch(handle.GetComponent<RectTransform>());
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.4f, 0.42f, 0.48f, 1f);
            handleImage.raycastTarget = true;

            Scrollbar bar = barObject.GetComponent<Scrollbar>();
            bar.targetGraphic = handleImage;
            bar.handleRect = handle.GetComponent<RectTransform>();
            bar.direction = Scrollbar.Direction.BottomToTop;

            // Viewport with content rows
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewportObject.transform.SetParent(listRoot.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.offsetMin = new Vector2(0f, 0f);
            viewportRect.offsetMax = new Vector2(-30f, 0f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.08f, 0.09f, 0.11f, 0.8f);
            viewportImage.raycastTarget = true;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = listRoot.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.verticalScrollbar = bar;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 20f;

            viewport = contentRect;
            return listRoot;
        }

        private static GameObject BuildTakeRowTemplate(RectTransform content, Font font)
        {
            GameObject row = new GameObject("Take Row Template", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(content, false);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;

            Image background = row.GetComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            background.raycastTarget = true;

            Button button = row.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(row.transform, false);
            Stretch(textObj.GetComponent<RectTransform>());
            Text label = textObj.GetComponent<Text>();
            label.text = "Take 1";
            label.font = font;
            label.fontSize = 26;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            row.SetActive(false); // template rows stay hidden until the panel clones them
            return row;
        }

        private static GameObject BuildActRowTemplate(RectTransform content, Font font)
        {
            GameObject row = new GameObject("Act Row Template", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(content, false);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;

            Image background = row.GetComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            background.raycastTarget = true;

            Button button = row.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(row.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMax = new Vector2(-80f, 0f);
            Text label = textObj.GetComponent<Text>();
            label.text = "Act 1";
            label.font = font;
            label.fontSize = 26;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;

            // Per-row delete button on the right edge
            GameObject deleteObj = new GameObject("Delete", typeof(RectTransform), typeof(Image), typeof(Button));
            deleteObj.transform.SetParent(row.transform, false);
            RectTransform deleteRect = deleteObj.GetComponent<RectTransform>();
            deleteRect.anchorMin = new Vector2(1f, 0.5f);
            deleteRect.anchorMax = new Vector2(1f, 0.5f);
            deleteRect.pivot = new Vector2(1f, 0.5f);
            deleteRect.sizeDelta = new Vector2(64f, 64f);
            deleteRect.anchoredPosition = new Vector2(0f, 0f);

            Image deleteImage = deleteObj.GetComponent<Image>();
            deleteImage.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);
            deleteImage.raycastTarget = true;

            Button deleteButton = deleteObj.GetComponent<Button>();
            deleteButton.transition = Selectable.Transition.None;

            GameObject deleteText = new GameObject("Label", typeof(RectTransform), typeof(Text));
            deleteText.transform.SetParent(deleteObj.transform, false);
            Stretch(deleteText.GetComponent<RectTransform>());
            Text deleteLabel = deleteText.GetComponent<Text>();
            deleteLabel.text = "X";
            deleteLabel.font = font;
            deleteLabel.fontSize = 28;
            deleteLabel.color = Color.white;
            deleteLabel.alignment = TextAnchor.MiddleCenter;
            deleteLabel.raycastTarget = false;

            row.SetActive(false);
            return row;
        }

        private static GameObject BuildAddButton(Transform parent, Font font, out Image image)
        {
            GameObject button = new GameObject("Add Button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-20f, 10f);
            rect.sizeDelta = new Vector2(160f, 60f);

            image = button.GetComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.25f, 0.95f);
            image.raycastTarget = true;

            Button add = button.GetComponent<Button>();
            add.transition = Selectable.Transition.None;

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(button.transform, false);
            Stretch(textObj.GetComponent<RectTransform>());
            Text label = textObj.GetComponent<Text>();
            label.text = "+ Add";
            label.font = font;
            label.fontSize = 26;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Button BuildPlainButton(Transform parent, string label, Font font,
            Vector2 anchorMin, Vector2 size, Vector2 pivot, Vector2 anchoredPosition)
        {
            GameObject buttonObj = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMin;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = buttonObj.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            image.raycastTarget = true;

            Button button = buttonObj.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            GameObject textObj = new GameObject(label + " Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(buttonObj.transform, false);
            Stretch(textObj.GetComponent<RectTransform>());
            Text text = textObj.GetComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return button;
        }

        public static void SetupSceneInstance(GameObject prefab)
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
