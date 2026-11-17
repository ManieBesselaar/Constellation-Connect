namespace Constellation.EditorTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Constellation.Data;
    using Constellation.Gameplay;
    using Constellation.Menu;
    using Constellation.UI;
    using Lifestories.Core;
    using Lifestories.Navigation;
    using TMPro;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem.UI;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// Builds the two playable game scenes from the prefab library, repairs the framework
    /// bootstrap scene so it points at the game's own scenes, and orders the build settings.
    /// Every reference is wired from code, so the scenes can be rebuilt from scratch at any time.
    /// </summary>
    public static class SceneBootstrapper
    {
        /// <summary>Folder holding the game scenes.</summary>
        public const string SceneFolder = "Assets/_project/Scenes";

        /// <summary>Framework bootstrap scene, created once by hand and repaired here.</summary>
        public const string BootstrapScenePath = SceneFolder + "/00_Bootstrap.unity";

        /// <summary>Loading screen shown between scenes.</summary>
        public const string LoadingScenePath = SceneFolder + "/01_LoadingScreen.unity";

        /// <summary>Constellation selection map.</summary>
        public const string MainMenuScenePath = SceneFolder + "/02_MainMenu.unity";

        /// <summary>The three phase gameplay scene.</summary>
        public const string GameplayScenePath = SceneFolder + "/03_Gameplay.unity";

        private const string PrefabFolder = UiPrefabFactory.PrefabFolder;
        private const string DataFolder = "Assets/_project/Data";
        private const string GlyphLibraryPath = DataFolder + "/GlyphLibrary.asset";
        private const string ConstellationsFolder = DataFolder + "/Constellations";
        private const string ScenesDataFolder = DataFolder + "/Scenes";

        private const string LoadChannelPath = "Assets/_core game Framework/Demo/Assets/LoadSceneRequest.asset";
        private const string MusicFolder = "Assets/_project/Music";
        private const string SfxFolder = "Assets/_project/SFX";
        private const string BackgroundPath = "Assets/_project/Sprites/Backgrounds/Constellation bg.png";
        private const string UiSheet = "Assets/_project/Sprites/UI/Constellation UI.png";
        private const string IconSheet = "Assets/_project/Sprites/UI/Constellation UI icons.png";
        private const string ExtraSheet = "Assets/_project/Sprites/UI/Constellation UI additional icons.png";
        private const string SoundSheet = "Assets/_project/Sprites/UI/SoundIcons.png";

        private const string PillButtonPrefab = PrefabFolder + "/PFB_UiPillButton.prefab";
        private const string ThemeCardPrefab = PrefabFolder + "/PFB_UiThemeCard.prefab";
        private const string HexTilePrefab = PrefabFolder + "/PFB_UiHexTile.prefab";
        private const string AnchorNodePrefab = PrefabFolder + "/PFB_UiAnchorNode.prefab";
        private const string BridgeHubPrefab = PrefabFolder + "/PFB_UiBridgeHub.prefab";
        private const string ProgressBarPrefab = PrefabFolder + "/PFB_UiProgressBar.prefab";
        private const string ModalPanelPrefab = PrefabFolder + "/PFB_UiModalPanel.prefab";
        private const string StudyCardPrefab = PrefabFolder + "/PFB_UiStudyCard.prefab";
        private const string VictoryBloomPrefab = "Assets/_project/Prefabs/VFX/PFB_VFX_VictoryBloom.prefab";

        private static readonly Color Cyan = new Color(0.35f, 0.95f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.44f, 1f);
        private static readonly Color Ink = new Color(0.85f, 0.93f, 1f, 1f);
        private static readonly Color DimInk = new Color(0.6f, 0.7f, 0.82f, 1f);
        private static readonly Color DeepNavy = new Color(0.04f, 0.06f, 0.14f, 1f);
        private static readonly Color CameraClear = new Color(0.016f, 0.024f, 0.055f, 1f);

        /// <summary>Body copy of the how-to-play overlay.</summary>
        private const string HowToPlayBody =
            "1.  STUDY: Read the six verse cards and memorise their phrases before the timer ends.\n\n" +
            "2.  CONNECT: Drag each phrase tile onto the medallion of the verse it belongs to.\n\n" +
            "3.  BRIDGE: A hub joins two verses. Drop the phrase they share onto the hub.\n\n" +
            "4.  HINT: Costs 75 points. Finish with no hints to collect all three stars.";

        /// <summary>Maps the icon identifiers used by the verse data onto the glyph sheet slices.</summary>
        private static readonly Dictionary<string, string> GlyphBindings = new Dictionary<string, string>
        {
            { "icon_scroll", "glyph_scroll" },
            { "icon_dove", "glyph_dove" },
            { "icon_shield", "glyph_shield" },
            { "icon_anchor", "glyph_anchor" },
            { "icon_heart_cross", "glyph_heart_cross" },
            { "icon_crown", "glyph_crown" },
            { "icon_olive_branch", "glyph_olive_branch" },
            { "icon_lamp", "glyph_lamp" },
            { "icon_shepherd", "glyph_shepherd" },
            { "icon_mountain", "glyph_mountain" },
            { "icon_star", "glyph_star" },
            { "icon_open_bible", "glyph_open_bible" }
        };

        private static readonly string[] ThemePaths =
        {
            ConstellationsFolder + "/theme_faith_strength.asset",
            ConstellationsFolder + "/theme_peace_comfort.asset",
            ConstellationsFolder + "/theme_gods_love.asset",
            ConstellationsFolder + "/theme_wisdom_guidance.asset",
            ConstellationsFolder + "/theme_hope_purpose.asset"
        };

        /// <summary>Scene the objects are being created into while a scene is built.</summary>
        private static Scene _scene;

        // ------------------------------------------------------------------ entry points

        /// <summary>
        /// Rebuilds the glyph library, repairs the bootstrap scene, then rebuilds both game scenes.
        /// </summary>
        [MenuItem("Tools/Constellation/Build Game Scenes")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneBootstrapper] Exit play mode before building the game scenes.");
                return;
            }

            EnsureFolder(SceneFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(ScenesDataFolder);

            GlyphLibrary glyphs = EnsureGlyphLibrary();
            RepairBootstrapScene();

            BuildScene(MainMenuScenePath, scene => PopulateMainMenu(glyphs));
            BuildScene(GameplayScenePath, scene => PopulateGameplay(glyphs));

            ApplyBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneBootstrapper] Done: GlyphLibrary, 02_MainMenu and 03_Gameplay built, bootstrap repaired, build settings ordered.");
        }

        /// <summary>
        /// Reassigns the music and sound effect clips on the persistent AudioManager without
        /// rebuilding the game scenes.
        /// </summary>
        [MenuItem("Tools/Constellation/Wire Audio")]
        public static void WireAudioOnly()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneBootstrapper] Exit play mode before wiring audio.");
                return;
            }

            RepairBootstrapScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBootstrapper] Audio wiring pass finished.");
        }

        /// <summary>
        /// Walks every scene the game uses and reports any unassigned serialized object reference.
        /// </summary>
        [MenuItem("Tools/Constellation/Verify Game Scenes")]
        public static void VerifyScenes()
        {
            string[] paths = { BootstrapScenePath, LoadingScenePath, MainMenuScenePath, GameplayScenePath };

            foreach (string path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogError("[SceneBootstrapper] Missing scene: " + path);
                    continue;
                }

                Scene previousActive = SceneManager.GetActiveScene();
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                int roots = 0;
                int components = 0;
                int nulls = 0;

                try
                {
                    roots = scene.rootCount;
                    components = 0;
                    nulls = 0;

                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        {
                            foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>())
                            {
                                if (behaviour == null) continue;
                                components++;
                                nulls += ReportNullReferences(behaviour, path);
                            }
                        }
                    }
                }
                finally
                {
                    SceneManager.SetActiveScene(previousActive);
                    EditorSceneManager.CloseScene(scene, true);
                }

                Debug.Log($"[SceneBootstrapper] Verify {Path.GetFileName(path)}: roots={roots} monoBehaviours={components} unassignedRefs={nulls}");
            }
        }

        // ------------------------------------------------------------------ data assets

        private static GlyphLibrary EnsureGlyphLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<GlyphLibrary>(GlyphLibraryPath);

            if (library == null)
            {
                library = ScriptableObject.CreateInstance<GlyphLibrary>();
                AssetDatabase.CreateAsset(library, GlyphLibraryPath);
                Debug.Log("[SceneBootstrapper] Created " + GlyphLibraryPath);
            }

            var entries = new List<GlyphLibrary.Entry>();

            foreach (KeyValuePair<string, string> pair in GlyphBindings)
            {
                Sprite sprite = LoadSprite(IconSheet, pair.Value);

                if (sprite == null)
                {
                    Debug.LogWarning($"[SceneBootstrapper] Glyph '{pair.Key}' has no sprite '{pair.Value}'; the medallion will fall back to its base art.");
                    continue;
                }

                entries.Add(new GlyphLibrary.Entry { iconId = pair.Key, sprite = sprite });
            }

            library.SetEntries(entries);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneBootstrapper] GlyphLibrary holds {entries.Count} icon bindings.");
            return library;
        }

        // ------------------------------------------------------------------ bootstrap repair

        private static void RepairBootstrapScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) == null)
            {
                Debug.LogError("[SceneBootstrapper] Bootstrap scene missing: " + BootstrapScenePath);
                return;
            }

            var menuScene = AssetDatabase.LoadAssetAtPath<GameSceneSO>(ScenesDataFolder + "/Scene_GameMenu.asset");
            var loadingScene = AssetDatabase.LoadAssetAtPath<GameSceneSO>(ScenesDataFolder + "/Scene_GameLoading.asset");
            var loadChannel = AssetDatabase.LoadAssetAtPath<SceneLoadEventChannelSO>(LoadChannelPath);

            Scene previousActive = SceneManager.GetActiveScene();
            bool alreadyLoaded = SceneManager.GetSceneByPath(BootstrapScenePath).isLoaded;
            Scene scene = alreadyLoaded
                ? SceneManager.GetSceneByPath(BootstrapScenePath)
                : EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                SceneManager.SetActiveScene(scene);

                var initializer = FindInScene<BootstrapInitializer>(scene);
                var loader = FindInScene<SceneLoaderManager>(scene);

                if (initializer == null) Debug.LogError("[SceneBootstrapper] No BootstrapInitializer in the bootstrap scene.");
                else new Wiring().Set("_mainMenuSceneSO", menuScene).Set("_loadSceneRequestChannel", loadChannel).ApplyTo(initializer);

                if (loader == null) Debug.LogError("[SceneBootstrapper] No SceneLoaderManager in the bootstrap scene.");
                else new Wiring().Set("_loadingScreenScene", loadingScene).Set("_loadSceneRequestChannel", loadChannel).ApplyTo(loader);

                EnsureAudioManager(scene);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[SceneBootstrapper] Bootstrap: initial scene -> " + (menuScene != null ? menuScene.SceneName : "MISSING") +
                          ", loading scene -> " + (loadingScene != null ? loadingScene.SceneName : "MISSING") + ".");
            }
            finally
            {
                SceneManager.SetActiveScene(previousActive);

                if (!alreadyLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void EnsureAudioManager(Scene scene)
        {
            var manager = FindInScene<Constellation.Audio.AudioManager>(scene);

            if (manager == null)
            {
                var persistent = FindInScene<PersistentManager>(scene);
                Transform parent = persistent != null ? persistent.transform : null;

                var go = new GameObject("AudioManager", typeof(Constellation.Audio.AudioManager));

                if (parent != null)
                {
                    // Parenting already puts the object into the parent's scene; only a root can be moved explicitly.
                    go.transform.SetParent(parent, false);
                }
                else
                {
                    EditorSceneManager.MoveGameObjectToScene(go, scene);
                }

                Debug.Log("[SceneBootstrapper] Added AudioManager under the persistent root so it survives scene transitions.");
                manager = go.GetComponent<Constellation.Audio.AudioManager>();
            }

            WireAudio(manager);
        }

        /// <summary>
        /// Assigns both audio sources plus the music and sound effect clips, so the whole mix is
        /// rebuilt from code with the scenes instead of living in the serialized scene file.
        /// </summary>
        private static void WireAudio(Constellation.Audio.AudioManager manager)
        {
            if (manager == null)
            {
                Debug.LogError("[SceneBootstrapper] No AudioManager to wire; audio will stay silent.");
                return;
            }

            GameObject go = manager.gameObject;
            AudioSource[] sources = go.GetComponents<AudioSource>();

            AudioSource bgm = sources.Length > 0 ? sources[0] : go.AddComponent<AudioSource>();
            AudioSource sfx = sources.Length > 1 ? sources[1] : go.AddComponent<AudioSource>();

            bgm.playOnAwake = false;
            bgm.loop = true;
            bgm.spatialBlend = 0f;

            sfx.playOnAwake = false;
            sfx.loop = false;
            sfx.spatialBlend = 0f;

            new Wiring()
                .Set("_bgmSource", bgm)
                .Set("_sfxSource", sfx)
                .Set("_defaultBgm", LoadClip(MusicFolder, "BGM_CosmicMeditation_Loop"))
                .Set("_tilePickup", LoadClip(SfxFolder, "SFX_Tile_Pickup"))
                .Set("_tileSnapCorrect", LoadClip(SfxFolder, "SFX_Tile_Snap_Correct"))
                .Set("_tileRippleWrong", LoadClip(SfxFolder, "SFX_Tile_Ripple_Wrong"))
                .Set("_verseComplete", LoadClip(SfxFolder, "SFX_Verse_Complete"))
                .Set("_hintPulse", LoadClip(SfxFolder, "SFX_Hint_Pulse"))
                .Set("_timerTickWarning", LoadClip(SfxFolder, "SFX_Timer_Tick_Warning"))
                .Set("_constellationVictory", LoadClip(SfxFolder, "SFX_Constellation_Victory"))
                .ApplyTo(manager);

            Debug.Log("[SceneBootstrapper] AudioManager wired: music + 7 one-shot cues.");
        }

        /// <summary>
        /// Finds an audio clip by file name, ignoring the extension, so a re-exported .mp3 or .ogg
        /// still resolves.
        /// </summary>
        private static AudioClip LoadClip(string folder, string clipName)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileNameWithoutExtension(path) != clipName) continue;

                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            Debug.LogWarning($"[SceneBootstrapper] No audio clip named '{clipName}' under {folder}.");
            return null;
        }

        // ------------------------------------------------------------------ scene 02: menu

        private static void PopulateMainMenu(GlyphLibrary glyphs)
        {
            CreateCamera();
            CreateEventSystem();

            RectTransform canvas = CreateCanvas("MainMenuCanvas");
            var controller = canvas.gameObject.AddComponent<MainMenuViewController>();

            Image background = NewImage(canvas, "Background", LoadBackgroundSprite());
            StretchTo((RectTransform)background.transform, 0f, 0f, 0f, 0f);
            background.preserveAspect = false;

            TextMeshProUGUI title = Text(canvas, "Title", "Constellation Connect", 78f, Cyan);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, Top, Top, Top, new Vector2(0f, -46f), new Vector2(1400f, 100f));

            TextMeshProUGUI subtitle = Text(canvas, "Subtitle", "Choose the constellation you want to connect", 26f, Ink);
            Place(subtitle.rectTransform, Top, Top, Top, new Vector2(0f, -152f), new Vector2(1400f, 40f));

            GameObject settingsGo = InstantiatePrefab(PillButtonPrefab, canvas, "SettingsButton");
            Place((RectTransform)settingsGo.transform, TopRight, TopRight, TopRight, new Vector2(-42f, -40f), new Vector2(220f, 72f));
            SetButtonLabel(settingsGo, "SETTINGS", 26f);
            Button settingsButton = settingsGo.GetComponent<Button>();

            RectTransform grid = NewRect("CardGrid", canvas, Centre, Centre, Centre, new Vector2(0f, -50f), new Vector2(1380f, 660f));
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(300f, 190f);
            gridLayout.spacing = new Vector2(30f, 30f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            SettingsModalView modal = BuildSettingsModal(canvas);

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeCardPrefab);
            var cardView = cardPrefab != null ? cardPrefab.GetComponent<ConstellationCardView>() : null;

            new Wiring()
                .Set("_glyphs", glyphs)
                .Set("_cardPrefab", cardView)
                .Set("_cardContainer", grid)
                .Set("_titleLabel", title)
                .Set("_gameplayScene", AssetDatabase.LoadAssetAtPath<GameSceneSO>(ScenesDataFolder + "/Scene_GamePlay.asset"))
                .Set("_loadSceneRequestChannel", AssetDatabase.LoadAssetAtPath<SceneLoadEventChannelSO>(LoadChannelPath))
                .Set("_settingsModal", modal)
                .Set("_settingsButton", settingsButton)
                .SetArray("_constellations", LoadThemes())
                .ApplyTo(controller);
        }

        private static SettingsModalView BuildSettingsModal(Transform canvas)
        {
            GameObject root = InstantiatePrefab(ModalPanelPrefab, canvas, "SettingsModal");
            Place((RectTransform)root.transform, Centre, Centre, Centre, Vector2.zero, new Vector2(800f, 700f));

            var view = root.AddComponent<SettingsModalView>();
            Transform content = root.transform.Find("Content");

            if (content == null)
            {
                Debug.LogError("[SceneBootstrapper] The modal prefab has no 'Content' child.");
                return view;
            }

            TextMeshProUGUI heading = Text(content, "Heading", "Settings", 40f, Cyan);
            heading.fontStyle = FontStyles.Bold;
            heading.rectTransform.sizeDelta = new Vector2(0f, 52f);

            RectTransform timerRow = Row(content, "TimerRow", 74f);
            Button timer30 = NewPillButton(timerRow, "Timer30", "30 s");
            Button timer60 = NewPillButton(timerRow, "Timer60", "60 s");
            Button timer90 = NewPillButton(timerRow, "Timer90", "90 s");
            Button timerZen = NewPillButton(timerRow, "TimerZen", "ZEN");

            TextMeshProUGUI timerSummary = Text(content, "TimerSummary", "60 seconds", 24f, Ink);
            timerSummary.rectTransform.sizeDelta = new Vector2(0f, 30f);

            Slider master = SliderRow(content, "MasterVolume", "Master", 54f, LoadSprite(SoundSheet, "icon_master_volume"));
            Slider music = SliderRow(content, "MusicVolume", "Music", 54f, LoadSprite(SoundSheet, "icon_music_volume"));
            Slider sfx = SliderRow(content, "SfxVolume", "Sound", 54f, LoadSprite(SoundSheet, "icon_sfx_volume"));

            TextMeshProUGUI attribution = Text(content, "Attribution", string.Empty, 16f, DimInk);
            attribution.alignment = TextAlignmentOptions.TopLeft;
            attribution.rectTransform.sizeDelta = new Vector2(0f, 110f);

            Button close = NewPillButton(content, "CloseButton", "CLOSE");

            new Wiring()
                .Set("_panel", root)
                .Set("_closeButton", close)
                .Set("_timer30Button", timer30)
                .Set("_timer60Button", timer60)
                .Set("_timer90Button", timer90)
                .Set("_timerZenButton", timerZen)
                .Set("_timerSummary", timerSummary)
                .Set("_masterSlider", master)
                .Set("_musicSlider", music)
                .Set("_sfxSlider", sfx)
                .Set("_attributionLabel", attribution)
                .ApplyTo(view);

            return view;
        }

        // ------------------------------------------------------------------ scene 03: gameplay

        private static void PopulateGameplay(GlyphLibrary glyphs)
        {
            CreateCamera();
            CreateEventSystem();

            RectTransform canvas = CreateCanvas("GameplayCanvas");
            var machine = canvas.gameObject.AddComponent<GamePhaseStateMachine>();

            Image background = NewImage(canvas, "Background", LoadBackgroundSprite());
            StretchTo((RectTransform)background.transform, 0f, 0f, 0f, 0f);
            background.preserveAspect = false;

            // ---- phase 1 -------------------------------------------------------------
            RectTransform studyRoot = NewRect("StudyPhase", canvas, Centre, Centre, Centre, Vector2.zero, new Vector2(1920f, 1080f));
            StretchTo(studyRoot, 0f, 0f, 0f, 0f);
            var study = studyRoot.gameObject.AddComponent<StudyPhaseController>();

            RectTransform studyPanel = NewRect("StudyPanel", studyRoot, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(studyPanel, 0f, 0f, 0f, 0f);

            TextMeshProUGUI themeLabel = Text(studyPanel, "ThemeLabel", "Faith & Strength", 46f, Cyan);
            themeLabel.fontStyle = FontStyles.Bold;
            Place(themeLabel.rectTransform, Top, Top, Top, new Vector2(-140f, -40f), new Vector2(1200f, 60f));

            TextMeshProUGUI instructionLabel = Text(studyPanel, "InstructionLabel", "Memorise the verses. Starting early awards bonus points.", 24f, Ink);
            Place(instructionLabel.rectTransform, Top, Top, Top, new Vector2(0f, -106f), new Vector2(1200f, 40f));

            TextMeshProUGUI timerLabel = Text(studyPanel, "TimerLabel", "60s", 44f, Gold);
            Place(timerLabel.rectTransform, TopRight, TopRight, TopRight, new Vector2(-44f, -40f), new Vector2(240f, 60f));

            GameObject barGo = InstantiatePrefab(ProgressBarPrefab, studyPanel, "CountdownBar");
            Place((RectTransform)barGo.transform, Top, Top, Top, new Vector2(0f, -170f), new Vector2(700f, 44f));
            CountdownBar countdown = AttachCountdownBar(barGo);

            RectTransform cardGrid = NewRect("CardGrid", studyPanel, Centre, Centre, Centre, new Vector2(0f, -60f), new Vector2(1436f, 548f));
            var cardLayout = cardGrid.gameObject.AddComponent<GridLayoutGroup>();
            cardLayout.cellSize = new Vector2(460f, 260f);
            cardLayout.spacing = new Vector2(28f, 28f);
            cardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cardLayout.constraintCount = 3;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;

            GameObject startGo = InstantiatePrefab(PillButtonPrefab, studyPanel, "StartButton");
            Place((RectTransform)startGo.transform, Bottom, Bottom, Bottom, new Vector2(0f, 26f), new Vector2(460f, 84f));
            SetButtonLabel(startGo, "START CONSTELLATION", 26f);
            Button startButton = startGo.GetComponent<Button>();

            var studyCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StudyCardPrefab);

            new Wiring()
                .Set("_panel", studyPanel.gameObject)
                .Set("_grid", cardGrid)
                .Set("_cardPrefab", studyCardPrefab != null ? studyCardPrefab.GetComponent<StudyCardView>() : null)
                .Set("_glyphs", glyphs)
                .Set("_countdownBar", countdown)
                .Set("_timerLabel", timerLabel)
                .Set("_themeLabel", themeLabel)
                .Set("_instructionLabel", instructionLabel)
                .Set("_startButton", startButton)
                .ApplyTo(study);

            // ---- phase 2 -------------------------------------------------------------
            RectTransform connectRoot = NewRect("ConnectPhase", canvas, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(connectRoot, 0f, 0f, 0f, 0f);
            var connect = connectRoot.gameObject.AddComponent<ConnectPhaseController>();
            var linkRenderer = connectRoot.gameObject.AddComponent<StarLinkRenderer>();
            var hintService = connectRoot.gameObject.AddComponent<HintService>();

            RectTransform connectPanel = NewRect("ConnectPanel", connectRoot, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(connectPanel, 0f, 0f, 0f, 0f);

            TextMeshProUGUI scoreLabel = Text(connectPanel, "ScoreLabel", "0", 44f, Gold);
            scoreLabel.alignment = TextAlignmentOptions.Left;
            Place(scoreLabel.rectTransform, TopLeft, TopLeft, TopLeft, new Vector2(44f, -34f), new Vector2(420f, 60f));

            TextMeshProUGUI progressLabel = Text(connectPanel, "ProgressLabel", "0 / 6", 34f, Ink);
            progressLabel.alignment = TextAlignmentOptions.Right;
            Place(progressLabel.rectTransform, TopRight, TopRight, TopRight, new Vector2(-44f, -40f), new Vector2(320f, 50f));

            GameObject hintGo = InstantiatePrefab(PillButtonPrefab, connectPanel, "HintButton");
            Place((RectTransform)hintGo.transform, TopRight, TopRight, TopRight, new Vector2(-44f, -104f), new Vector2(240f, 70f));
            SetButtonLabel(hintGo, "HINT  -75", 22f);
            Button hintButton = hintGo.GetComponent<Button>();

            RectTransform boardRoot = NewRect("BoardRoot", connectPanel, Centre, Centre, Centre, new Vector2(0f, 110f), new Vector2(1500f, 700f));
            RectTransform linkRoot = NewRect("LinkRoot", boardRoot, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(linkRoot, 0f, 0f, 0f, 0f);

            RectTransform tileTray = NewRect("TileTray", connectPanel, BottomLeft, BottomRightSpan, BottomCentre, new Vector2(0f, 30f), new Vector2(-80f, 440f));
            var trayLayout = tileTray.gameObject.AddComponent<GridLayoutGroup>();
            // 250 x 140 over 7 columns: three rows fit the 440 high tray, and the hex stays close to
            // square so the 26pt phrase has room inside it.
            trayLayout.cellSize = new Vector2(250f, 140f);
            trayLayout.spacing = new Vector2(8f, 8f);
            trayLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            trayLayout.constraintCount = 7;
            trayLayout.childAlignment = TextAnchor.UpperCenter;

            var anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorNodePrefab);
            var hubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BridgeHubPrefab);
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HexTilePrefab);

            new Wiring()
                .Set("_panel", connectPanel.gameObject)
                .Set("_boardRoot", boardRoot)
                .Set("_linkRoot", linkRoot)
                .Set("_tileTray", tileTray)
                .Set("_anchorPrefab", anchorPrefab != null ? anchorPrefab.GetComponent<AnchorNodeView>() : null)
                .Set("_hubPrefab", hubPrefab != null ? hubPrefab.GetComponent<BridgeHubView>() : null)
                .Set("_tilePrefab", tilePrefab != null ? tilePrefab.GetComponent<PhraseTileView>() : null)
                .Set("_stardustPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_project/Prefabs/VFX/PFB_VFX_StardustBurst.prefab"))
                .Set("_ripplePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_project/Prefabs/VFX/PFB_VFX_StardustBurst.prefab"))
                .Set("_glyphs", glyphs)
                .Set("_cyanLinkSprite", LoadSprite(UiSheet, "link_cyan_arc_a"))
                .Set("_goldLinkSprite", LoadSprite(UiSheet, "link_gold_arc"))
                .Set("_scoreLabel", scoreLabel)
                .Set("_progressLabel", progressLabel)
                .Set("_hintService", hintService)
                .ApplyTo(connect);

            new Wiring()
                .Set("_linkRoot", linkRoot)
                .Set("_cyanLinkSprite", LoadSprite(UiSheet, "link_cyan_arc_a"))
                .Set("_goldLinkSprite", LoadSprite(UiSheet, "link_gold_arc"))
                .ApplyTo(linkRenderer);

            new Wiring()
                .Set("_hintButton", hintButton)
                .Set("_controller", connect)
                .ApplyTo(hintService);

            // ---- phase 3 -------------------------------------------------------------
            RectTransform victoryRoot = NewRect("VictoryPhase", canvas, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(victoryRoot, 0f, 0f, 0f, 0f);
            var victory = victoryRoot.gameObject.AddComponent<VictoryViewController>();

            RectTransform victoryPanel = NewRect("VictoryPanel", victoryRoot, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(victoryPanel, 0f, 0f, 0f, 0f);

            Image dim = NewImage(victoryPanel, "Dim", null);
            StretchTo((RectTransform)dim.transform, 0f, 0f, 0f, 0f);
            dim.color = new Color(0.01f, 0.02f, 0.05f, 0.78f);

            GameObject cardGo = InstantiatePrefab(ModalPanelPrefab, victoryPanel, "VictoryCard");
            Place((RectTransform)cardGo.transform, Centre, Centre, Centre, Vector2.zero, new Vector2(820f, 640f));

            Transform vContent = cardGo.transform.Find("Content");
            TextMeshProUGUI vTitle = Text(vContent, "Title", "Constellation Complete", 40f, Gold);
            vTitle.fontStyle = FontStyles.Bold;
            vTitle.rectTransform.sizeDelta = new Vector2(0f, 60f);

            RectTransform starsRow = Row(vContent, "Stars", 92f);
            ((HorizontalLayoutGroup)starsRow.GetComponent<HorizontalLayoutGroup>()).spacing = 16f;
            var starImages = new List<UnityEngine.Object>();

            for (int i = 0; i < 3; i++)
            {
                Image star = NewImage(starsRow, "Star" + (i + 1), LoadSprite(ExtraSheet, "star_empty"));
                ((RectTransform)star.transform).sizeDelta = new Vector2(78f, 78f);
                starImages.Add(star);
            }

            TextMeshProUGUI vScore = Text(vContent, "Score", "0", 32f, Ink);
            vScore.rectTransform.sizeDelta = new Vector2(0f, 44f);

            TextMeshProUGUI vTime = Text(vContent, "Time", "00:00", 28f, Ink);
            vTime.rectTransform.sizeDelta = new Vector2(0f, 44f);

            TextMeshProUGUI vAccuracy = Text(vContent, "Accuracy", "No hints used", 24f, DimInk);
            vAccuracy.rectTransform.sizeDelta = new Vector2(0f, 36f);

            RectTransform buttonsRow = Row(vContent, "Buttons", 78f);
            Button menuButton = NewPillButton(buttonsRow, "MenuButton", "CONSTELLATION MAP");
            Button replayButton = NewPillButton(buttonsRow, "ReplayButton", "PLAY AGAIN");

            ParticleSystem bloom = CreateVictoryBloom();

            new Wiring()
                .Set("_panel", victoryPanel.gameObject)
                .Set("_bloom", bloom)
                .Set("_titleLabel", vTitle)
                .Set("_scoreLabel", vScore)
                .Set("_timeLabel", vTime)
                .Set("_accuracyLabel", vAccuracy)
                .Set("_starGold", LoadSprite(ExtraSheet, "star_gold"))
                .Set("_starEmpty", LoadSprite(ExtraSheet, "star_empty"))
                .Set("_menuButton", menuButton)
                .Set("_replayButton", replayButton)
                .SetArray("_stars", starImages.ToArray())
                .ApplyTo(victory);

            // ---- how to play ---------------------------------------------------------
            // Its own canvas with a higher sorting order so it covers every phase panel, including
            // the victory overlay, and blocks clicks to the tiles underneath.
            RectTransform instructionCanvas = CreateCanvas("InstructionCanvas");
            instructionCanvas.gameObject.GetComponent<Canvas>().sortingOrder = 50;

            RectTransform instructionPanel = NewRect("InstructionPanel", instructionCanvas, Centre, Centre, Centre, Vector2.zero, Vector2.zero);
            StretchTo(instructionPanel, 0f, 0f, 0f, 0f);

            Image instructionDim = NewImage(instructionPanel, "Dim", null);
            StretchTo((RectTransform)instructionDim.transform, 0f, 0f, 0f, 0f);
            instructionDim.color = new Color(0.01f, 0.02f, 0.05f, 0.9f);
            instructionDim.raycastTarget = true;

            GameObject howToCard = InstantiatePrefab(ModalPanelPrefab, instructionPanel, "HowToCard");
            Place((RectTransform)howToCard.transform, Centre, Centre, Centre, Vector2.zero, new Vector2(980f, 660f));

            Transform howToContent = howToCard.transform.Find("Content");

            TextMeshProUGUI howToTitle = Text(howToContent, "Title", "HOW TO PLAY", 42f, Gold);
            howToTitle.fontStyle = FontStyles.Bold;
            howToTitle.rectTransform.sizeDelta = new Vector2(0f, 58f);

            TextMeshProUGUI howToBody = Text(howToContent, "Body", HowToPlayBody, 26f, Ink);
            howToBody.alignment = TextAlignmentOptions.TopLeft;
            howToBody.rectTransform.sizeDelta = new Vector2(0f, 340f);

            Button gotItButton = NewPillButton(howToContent, "DismissButton", "GOT IT");
            ((RectTransform)gotItButton.transform).sizeDelta = new Vector2(0f, 78f);

            var howTo = instructionCanvas.gameObject.AddComponent<HowToPlayView>();

            new Wiring()
                .Set("_panel", instructionPanel.gameObject)
                .Set("_dismissButton", gotItButton)
                .ApplyTo(howTo);

            // ---- phase machine -------------------------------------------------------
            UnityEngine.Object[] themes = LoadThemes();

            new Wiring()
                .Set("_fallbackConstellation", themes.Length > 0 ? themes[0] : null)
                .Set("_study", study)
                .Set("_connect", connect)
                .Set("_victory", victory)
                .Set("_mainMenuScene", AssetDatabase.LoadAssetAtPath<GameSceneSO>(ScenesDataFolder + "/Scene_GameMenu.asset"))
                .Set("_gameplayScene", AssetDatabase.LoadAssetAtPath<GameSceneSO>(ScenesDataFolder + "/Scene_GamePlay.asset"))
                .Set("_loadSceneRequestChannel", AssetDatabase.LoadAssetAtPath<SceneLoadEventChannelSO>(LoadChannelPath))
                .Set("_scoreLabel", scoreLabel)
                .Set("_instructions", howTo)
                .ApplyTo(machine);

            // ---- music mute ------------------------------------------------------------
            // Built last so it draws over the victory dim: the mute stays reachable in every phase.
            // The bottom-right corner is the only spot no phase panel uses at this canvas size.
            RectTransform muteRect = NewRect(
                "MusicMuteButton", canvas, BottomRight, BottomRight, BottomRight,
                new Vector2(-44f, 486f), new Vector2(92f, 92f));

            var muteImage = muteRect.gameObject.AddComponent<Image>();
            muteImage.sprite = LoadSprite(SoundSheet, "icon_music_on");
            muteImage.preserveAspect = true;
            muteImage.raycastTarget = true;

            var muteButton = muteRect.gameObject.AddComponent<Button>();
            muteButton.targetGraphic = muteImage;

            var muteToggle = muteRect.gameObject.AddComponent<MusicMuteToggleView>();

            new Wiring()
                .Set("_button", muteButton)
                .Set("_icon", muteImage)
                .Set("_musicOnSprite", LoadSprite(SoundSheet, "icon_music_on"))
                .Set("_musicOffSprite", LoadSprite(SoundSheet, "icon_music_off"))
                .ApplyTo(muteToggle);
        }

        private static CountdownBar AttachCountdownBar(GameObject barRoot)
        {
            var bar = barRoot.AddComponent<CountdownBar>();
            Transform fill = barRoot.transform.Find("Fill/FillImage");

            if (fill == null)
            {
                Debug.LogError("[SceneBootstrapper] The progress bar prefab has no 'Fill/FillImage' child.");
                return bar;
            }

            var fillRect = (RectTransform)fill;
            Vector2 anchorMax = fillRect.anchorMax;
            anchorMax.x = 1f;
            fillRect.anchorMax = anchorMax;

            var image = fill.GetComponent<Image>();

            if (image != null) image.color = Cyan;

            new Wiring().Set("_fill", fillRect).Set("_fillImage", image).ApplyTo(bar);
            return bar;
        }

        private static ParticleSystem CreateVictoryBloom()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VictoryBloomPrefab);

            if (prefab == null)
            {
                Debug.LogWarning("[SceneBootstrapper] No victory bloom prefab; the celebration will run without particles.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "VictoryBloom";
            EditorSceneManager.MoveGameObjectToScene(instance, _scene);
            instance.transform.position = Vector3.zero;

            var system = instance.GetComponent<ParticleSystem>();

            if (system == null) system = instance.GetComponentInChildren<ParticleSystem>(true);

            if (system != null)
            {
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = false;
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            return system;
        }

        // ------------------------------------------------------------------ build settings

        private static void ApplyBuildSettings()
        {
            var ordered = new List<EditorBuildSettingsScene>();
            string[] first = { BootstrapScenePath, LoadingScenePath, MainMenuScenePath, GameplayScenePath };

            foreach (string path in first)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogError("[SceneBootstrapper] Cannot add missing scene to the build settings: " + path);
                    continue;
                }

                ordered.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(existing.guid.ToString());

                if (string.IsNullOrEmpty(existingPath)) existingPath = existing.path;
                if (string.IsNullOrEmpty(existingPath)) continue;

                bool duplicate = false;
                foreach (EditorBuildSettingsScene added in ordered)
                {
                    if (added.path == existingPath)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate) ordered.Add(existing);
            }

            EditorBuildSettings.scenes = ordered.ToArray();

            var names = new List<string>();
            foreach (EditorBuildSettingsScene scene in ordered) names.Add((scene.enabled ? string.Empty : "(off) ") + Path.GetFileNameWithoutExtension(scene.path));
            Debug.Log("[SceneBootstrapper] Build settings: " + string.Join(", ", names));
        }

        // ------------------------------------------------------------------ scene plumbing

        private static void BuildScene(string path, Action<Scene> populate)
        {
            Scene previousActive = SceneManager.GetActiveScene();
            bool alreadyLoaded = SceneManager.GetSceneByPath(path).isLoaded;

            // A scene that is already open in the editor cannot be saved over, so it is reused and
            // emptied instead of being built a second time beside itself.
            Scene scene = alreadyLoaded
                ? SceneManager.GetSceneByPath(path)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            _scene = scene;
            SceneManager.SetActiveScene(scene);

            try
            {
                if (alreadyLoaded) ClearRoots(scene);

                populate(scene);

                if (!EditorSceneManager.SaveScene(scene, path))
                {
                    Debug.LogError("[SceneBootstrapper] Failed to save " + path);
                    return;
                }

                var roots = new List<string>();
                foreach (GameObject root in scene.GetRootGameObjects()) roots.Add(root.name);
                Debug.Log($"[SceneBootstrapper] Saved {path}: {string.Join(", ", roots)}.");
            }
            finally
            {
                _scene = default;
                SceneManager.SetActiveScene(previousActive);

                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Empties a scene that was already open, so a rebuild never leaves a second copy of the UI behind.
        /// </summary>
        private static void ClearRoots(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static RectTransform CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            EditorSceneManager.MoveGameObjectToScene(go, _scene);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return (RectTransform)go.transform;
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            EditorSceneManager.MoveGameObjectToScene(go, _scene);
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraClear;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.MoveGameObjectToScene(go, _scene);

            // The Input System module ships with no bindings until its default action asset is assigned.
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        private static int ReportNullReferences(MonoBehaviour behaviour, string scenePath)
        {
            // Only the game's own scripts are checked; TMP and UGUI expose plenty of legitimately
            // empty optional fields (materials, sprite swaps, navigation targets) that say nothing.
            string typeNamespace = behaviour.GetType().Namespace ?? string.Empty;

            if (!typeNamespace.StartsWith("Constellation")) return 0;

            var serialized = new SerializedObject(behaviour);
            SerializedProperty property = serialized.GetIterator();
            int nulls = 0;

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.name == "m_Script") continue;
                if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue != 0) continue;

                nulls++;
                Debug.LogWarning($"[SceneBootstrapper] {Path.GetFileName(scenePath)}: {behaviour.GetType().Name}.{property.name} on '{behaviour.gameObject.name}' is unassigned.");
            }

            return nulls;
        }

        // ------------------------------------------------------------------ ui builders

        private static RectTransform Row(Transform parent, string name, float height)
        {
            RectTransform row = NewRect(name, parent, Centre, Centre, Centre, Vector2.zero, new Vector2(0f, height));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static Button NewPillButton(Transform parent, string name, string label)
        {
            GameObject go = InstantiatePrefab(PillButtonPrefab, parent, name);
            ((RectTransform)go.transform).sizeDelta = new Vector2(160f, 70f);
            SetButtonLabel(go, label, 22f);
            return go.GetComponent<Button>();
        }

        private static void SetButtonLabel(GameObject buttonGo, string text, float fontSize)
        {
            Transform label = buttonGo.transform.Find("Label");

            if (label == null)
            {
                Debug.LogWarning("[SceneBootstrapper] No 'Label' child on " + buttonGo.name);
                return;
            }

            var tmp = label.GetComponent<TextMeshProUGUI>();

            if (tmp == null) return;

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = DeepNavy;
            EditorUtility.SetDirty(tmp);
        }

        private static Slider SliderRow(Transform parent, string name, string label, float height, Sprite icon)
        {
            RectTransform row = Row(parent, name + "Row", height);

            if (icon != null)
            {
                Image glyph = NewImage(row, "Icon", icon);
                ((RectTransform)glyph.transform).sizeDelta = new Vector2(46f, 46f);

                var glyphLayout = glyph.gameObject.AddComponent<LayoutElement>();
                glyphLayout.minWidth = 46f;
                glyphLayout.preferredWidth = 46f;
                glyphLayout.flexibleWidth = 0f;
            }

            TextMeshProUGUI caption = Text(row, "Caption", label, 24f, Ink);
            caption.alignment = TextAlignmentOptions.Left;
            caption.rectTransform.sizeDelta = new Vector2(0f, 34f);

            var captionLayout = caption.gameObject.AddComponent<LayoutElement>();
            captionLayout.flexibleWidth = 0.7f;
            captionLayout.minWidth = 150f;

            Slider slider = NewSlider(row, name + "Slider");
            var sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 2f;
            sliderLayout.minWidth = 240f;

            return slider;
        }

        private static Slider NewSlider(Transform parent, string name)
        {
            RectTransform root = NewRect(name, parent, Centre, Centre, Centre, Vector2.zero, new Vector2(420f, 30f));
            var slider = root.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;

            Image track = NewImage(root, "Background", LoadSprite(UiSheet, "bar_segment"));
            RectTransform trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(-8f, 10f);
            track.type = Image.Type.Sliced;
            track.color = new Color(0.2f, 0.28f, 0.42f, 0.9f);

            RectTransform fillArea = NewRect("Fill Area", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, 10f));

            Image fill = NewImage(fillArea, "Fill", LoadSprite(UiSheet, "bar_segment"));
            RectTransform fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fill.type = Image.Type.Sliced;
            fill.color = Cyan;

            RectTransform handleArea = NewRect("Handle Slide Area", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, 0f));

            Image handle = NewImage(handleArea, "Handle", LoadSprite(UiSheet, "bar_segment"));
            RectTransform handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(20f, 28f);
            handle.type = Image.Type.Sliced;
            handle.color = Gold;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;

            return slider;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = new Vector2(96f, 96f);
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string content, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError("[SceneBootstrapper] Missing prefab: " + path);
                var fallback = new GameObject(name, typeof(RectTransform));
                fallback.transform.SetParent(parent, false);
                return fallback;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            return instance;
        }

        private static void StretchTo(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static UnityEngine.Object[] LoadThemes()
        {
            var themes = new List<UnityEngine.Object>();

            foreach (string path in ThemePaths)
            {
                var theme = AssetDatabase.LoadAssetAtPath<ConstellationSO>(path);

                if (theme == null)
                {
                    Debug.LogError("[SceneBootstrapper] Missing constellation asset: " + path);
                    continue;
                }

                themes.Add(theme);
            }

            return themes.ToArray();
        }

        private static Sprite LoadSprite(string sheetPath, string spriteName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
            }

            Debug.LogError("[SceneBootstrapper] Sprite '" + spriteName + "' not found in " + sheetPath);
            return null;
        }

        private static Sprite LoadBackgroundSprite()
        {
            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;

            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                Debug.Log("[SceneBootstrapper] Reimported the nebula background as a Sprite.");
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

            if (sprite == null) Debug.LogError("[SceneBootstrapper] No sprite at " + BackgroundPath);

            return sprite;
        }

        // anchors
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRightSpan = new Vector2(1f, 0f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        // ------------------------------------------------------------------ wiring helper

        /// <summary>Collects serialized field assignments and applies them in one pass.</summary>
        private sealed class Wiring
        {
            private readonly List<KeyValuePair<string, UnityEngine.Object>> _fields = new List<KeyValuePair<string, UnityEngine.Object>>();
            private readonly List<KeyValuePair<string, UnityEngine.Object[]>> _arrays = new List<KeyValuePair<string, UnityEngine.Object[]>>();

            public Wiring Set(string field, UnityEngine.Object value)
            {
                _fields.Add(new KeyValuePair<string, UnityEngine.Object>(field, value));
                return this;
            }

            public Wiring SetArray(string field, params UnityEngine.Object[] values)
            {
                _arrays.Add(new KeyValuePair<string, UnityEngine.Object[]>(field, values));
                return this;
            }

            public void ApplyTo(Component component)
            {
                if (component == null) return;

                var serialized = new SerializedObject(component);

                foreach (KeyValuePair<string, UnityEngine.Object> pair in _fields)
                {
                    SerializedProperty property = serialized.FindProperty(pair.Key);

                    if (property == null)
                    {
                        Debug.LogError($"[SceneBootstrapper] Field '{pair.Key}' not found on {component.GetType().Name} ({component.gameObject.name}).");
                        continue;
                    }

                    if (pair.Value == null)
                    {
                        Debug.LogWarning($"[SceneBootstrapper] '{pair.Key}' on {component.GetType().Name} is being left empty.");
                    }

                    property.objectReferenceValue = pair.Value;
                }

                foreach (KeyValuePair<string, UnityEngine.Object[]> pair in _arrays)
                {
                    SerializedProperty property = serialized.FindProperty(pair.Key);

                    if (property == null)
                    {
                        Debug.LogError($"[SceneBootstrapper] Array field '{pair.Key}' not found on {component.GetType().Name} ({component.gameObject.name}).");
                        continue;
                    }

                    property.arraySize = pair.Value.Length;

                    for (int i = 0; i < pair.Value.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = pair.Value[i];
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }
        }

        // ------------------------------------------------------------------ folders

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string normalised = path.Replace('\\', '/').TrimEnd('/');
            string parent = Path.GetDirectoryName(normalised)?.Replace('\\', '/');

            if (string.IsNullOrEmpty(parent)) return;

            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(normalised));
        }
    }
}
