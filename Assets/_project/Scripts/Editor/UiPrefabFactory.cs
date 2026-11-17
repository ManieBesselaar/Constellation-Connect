namespace Constellation.EditorTools
{
    using System.Collections.Generic;
    using System.IO;
    using Constellation.Gameplay;
    using Constellation.Menu;
    using Constellation.UI;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Builds the reusable UI prefab library so the look can be reskinned by swapping
    /// sprites in one place rather than editing every scene.
    /// </summary>
    public static class UiPrefabFactory
    {
        /// <summary>Folder holding the generated UI prefabs.</summary>
        public const string PrefabFolder = "Assets/_project/Prefabs/UI";

        private const string UiSheet = "Assets/_project/Sprites/UI/Constellation UI.png";
        private const string IconSheet = "Assets/_project/Sprites/UI/Constellation UI icons.png";
        private const string ExtraSheet = "Assets/_project/Sprites/UI/Constellation UI additional icons.png";

        private static readonly Color Cyan = new Color(0.35f, 0.95f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.44f, 1f);
        private static readonly Color Ink = new Color(0.85f, 0.93f, 1f, 1f);

        /// <summary>
        /// Rebuilds every UI prefab in the library.
        /// </summary>
        [MenuItem("Tools/Constellation/Build UI Prefabs")]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);

            BuildPillButton();
            BuildThemeCard();
            BuildHexTile();
            BuildAnchorNode();
            BuildBridgeHub();
            BuildProgressBar();
            BuildStarRating();
            BuildModalPanel();
            BuildStatRow();
            BuildStudyCard();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiPrefabFactory] UI prefab library built in " + PrefabFolder);
        }

        // ---------------------------------------------------------------- prefabs

        private static void BuildPillButton()
        {
            var root = NewRect("PFB_UiPillButton");
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(230f, 74f);

            var background = root.AddComponent<Image>();
            background.sprite = LoadSprite(UiSheet, "pill_light");
            background.type = Image.Type.Sliced;
            background.color = Ink;

            var button = root.AddComponent<Button>();
            button.targetGraphic = background;

            var tween = root.AddComponent<UIImageTween>();

            var label = NewText(root.transform, "Label", "BUTTON", 30f);
            Stretch((RectTransform)label.transform, 12f, 12f, 8f, 8f);

            var group = root.AddComponent<CanvasGroup>();

            Save(root, PrefabFolder + "/PFB_UiPillButton.prefab");
        }

        private static void BuildThemeCard()
        {
            var root = NewRect("PFB_UiThemeCard");
            ((RectTransform)root.transform).sizeDelta = new Vector2(290f, 184f);

            var panel = root.AddComponent<Image>();
            panel.sprite = LoadSprite(ExtraSheet, "card_panel_study");
            panel.type = Image.Type.Sliced;
            panel.preserveAspect = false;

            var tween = root.AddComponent<UIImageTween>();

            var medallion = NewImage(root.transform, "Medallion", LoadSprite(UiSheet, "medallion_cyan_scroll"));
            SetAnchors((RectTransform)medallion.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            ((RectTransform)medallion.transform).pivot = new Vector2(0.5f, 0.5f);
            ((RectTransform)medallion.transform).anchoredPosition = new Vector2(78f, 22f);
            ((RectTransform)medallion.transform).sizeDelta = new Vector2(120f, 120f);

            var title = NewText(root.transform, "Title", "Theme Title", 28f);
            SetAnchors((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            ((RectTransform)title.transform).pivot = new Vector2(0.5f, 1f);
            ((RectTransform)title.transform).anchoredPosition = new Vector2(30f, -14f);
            ((RectTransform)title.transform).sizeDelta = new Vector2(-60f, 40f);
            title.alignment = TextAlignmentOptions.Left;

            var stars = NewRect("Stars");
            stars.transform.SetParent(root.transform, false);
            SetAnchors((RectTransform)stars.transform, new Vector2(0f, 0f), new Vector2(0f, 0f));
            ((RectTransform)stars.transform).pivot = new Vector2(0f, 0f);
            ((RectTransform)stars.transform).anchoredPosition = new Vector2(20f, 16f);
            ((RectTransform)stars.transform).sizeDelta = new Vector2(150f, 36f);

            var layout = stars.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                var star = NewImage(stars.transform, "Star" + (i + 1), LoadSprite(ExtraSheet, "star_empty"));
                ((RectTransform)star.transform).sizeDelta = new Vector2(34f, 34f);
            }

            var best = NewText(root.transform, "BestScore", "Best 0", 20f);
            SetAnchors((RectTransform)best.transform, new Vector2(1f, 0f), new Vector2(1f, 0f));
            ((RectTransform)best.transform).pivot = new Vector2(1f, 0f);
            ((RectTransform)best.transform).anchoredPosition = new Vector2(-18f, 16f);
            ((RectTransform)best.transform).sizeDelta = new Vector2(130f, 30f);
            best.alignment = TextAlignmentOptions.Right;

            var group = root.AddComponent<CanvasGroup>();

            var cardView = root.AddComponent<ConstellationCardView>();
            SetRefs(cardView, new Dictionary<string, Object>
            {
                { "_panel", panel },
                { "_medallion", medallion },
                { "_title", title },
                { "_bestScore", best },
                { "_starsRoot", stars.transform },
                { "_starEmpty", LoadSprite(ExtraSheet, "star_empty") },
                { "_starGold", LoadSprite(ExtraSheet, "star_gold") },
                { "_panelIncomplete", LoadSprite(ExtraSheet, "card_panel_study") },
                { "_panelComplete", LoadSprite(ExtraSheet, "card_panel_complete") },
                { "_tween", tween }
            });

            Save(root, PrefabFolder + "/PFB_UiThemeCard.prefab");
        }

        private static void BuildHexTile()
        {
            var root = NewRect("PFB_UiHexTile");
            ((RectTransform)root.transform).sizeDelta = new Vector2(250f, 140f);

            var background = root.AddComponent<Image>();
            background.sprite = LoadSprite(UiSheet, "hex_tile_cyan");
            background.type = Image.Type.Simple;
            background.raycastTarget = true;

            var canvasGroup = root.AddComponent<CanvasGroup>();

            var label = NewText(root.transform, "Phrase", "Phrase chunk", 26f);
            // The hexagon is pointed left and right, so its usable width shrinks towards the top and
            // bottom lines. Generous side insets keep a wrapped phrase inside the slanted edges.
            Stretch((RectTransform)label.transform, 40f, 40f, 14f, 14f);

            // The hex tile art is pale cyan, so white text disappears into it. Deep navy ink keeps a
            // hard contrast on the tile at rest, while held (gold) and placed (gold) tints.
            label.color = new Color(0.04f, 0.06f, 0.14f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;

            // Chunks run from two words to a full sentence; auto sizing keeps the long ones inside
            // the hex instead of spilling over their neighbours.
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 28f;

            var tween = root.AddComponent<UIImageTween>();

            var tileView = root.AddComponent<PhraseTileView>();
            SetRefs(tileView, new Dictionary<string, Object>
            {
                { "_label", label },
                { "_background", background }
            });

            Save(root, PrefabFolder + "/PFB_UiHexTile.prefab");
        }

        private static void BuildAnchorNode()
        {
            var root = NewRect("PFB_UiAnchorNode");
            ((RectTransform)root.transform).sizeDelta = new Vector2(132f, 132f);

            var medallion = root.AddComponent<Image>();
            medallion.sprite = LoadSprite(UiSheet, "medallion_cyan_scroll");
            medallion.raycastTarget = true;

            var glyph = NewImage(root.transform, "Glyph", LoadSprite(UiSheet, "medallion_cyan_scroll"));
            Stretch((RectTransform)glyph.transform, 22f, 22f, 22f, 22f);
            glyph.color = new Color(1f, 1f, 1f, 0.65f);

            var reference = NewText(root.transform, "Reference", "Reference", 20f);
            SetAnchors((RectTransform)reference.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            ((RectTransform)reference.transform).pivot = new Vector2(0.5f, 1f);
            ((RectTransform)reference.transform).anchoredPosition = new Vector2(0f, -6f);
            ((RectTransform)reference.transform).sizeDelta = new Vector2(200f, 30f);

            var tween = root.AddComponent<UIImageTween>();

            var anchorView = root.AddComponent<AnchorNodeView>();
            SetRefs(anchorView, new Dictionary<string, Object>
            {
                { "_medallion", medallion },
                { "_referenceLabel", reference }
            });

            Save(root, PrefabFolder + "/PFB_UiAnchorNode.prefab");
        }

        private static void BuildBridgeHub()
        {
            var root = NewRect("PFB_UiBridgeHub");
            ((RectTransform)root.transform).sizeDelta = new Vector2(120f, 120f);

            var hub = root.AddComponent<Image>();
            hub.sprite = LoadSprite(ExtraSheet, "bridge_hub_inactive");

            var tag = NewText(root.transform, "Tag", "KEYWORD", 18f);
            Stretch((RectTransform)tag.transform, 6f, 6f, 40f, 40f);
            tag.color = Gold;

            var hubView = root.AddComponent<BridgeHubView>();
            SetRefs(hubView, new Dictionary<string, Object>
            {
                { "_hub", hub },
                { "_keywordLabel", tag },
                { "_completedSprite", LoadSprite(ExtraSheet, "bridge_hub_completed") }
            });

            Save(root, PrefabFolder + "/PFB_UiBridgeHub.prefab");
        }

        private static void BuildProgressBar()
        {
            var root = NewRect("PFB_UiProgressBar");
            ((RectTransform)root.transform).sizeDelta = new Vector2(520f, 48f);

            var background = root.AddComponent<Image>();
            background.sprite = LoadSprite(UiSheet, "bar_segmented_long");
            background.type = Image.Type.Sliced;

            var fillRoot = NewRect("Fill");
            fillRoot.transform.SetParent(root.transform, false);
            Stretch((RectTransform)fillRoot.transform, 8f, 8f, 10f, 10f);

            var mask = fillRoot.AddComponent<Image>();
            mask.color = new Color(1f, 1f, 1f, 0f);

            var fill = NewImage(fillRoot.transform, "FillImage", LoadSprite(UiSheet, "bar_segment"));
            Stretch((RectTransform)fill.transform, 0f, 0f, 0f, 0f);
            fill.type = Image.Type.Sliced;
            fill.color = Cyan;

            var fillRect = (RectTransform)fill.transform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.sizeDelta = new Vector2(520f, 0f);

            Save(root, PrefabFolder + "/PFB_UiProgressBar.prefab");
        }

        private static void BuildStarRating()
        {
            var root = NewRect("PFB_UiStarRating");
            ((RectTransform)root.transform).sizeDelta = new Vector2(180f, 52f);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
            {
                var star = NewImage(root.transform, "Star" + (i + 1), LoadSprite(ExtraSheet, "star_empty"));
                ((RectTransform)star.transform).sizeDelta = new Vector2(48f, 48f);
            }

            Save(root, PrefabFolder + "/PFB_UiStarRating.prefab");
        }

        private static void BuildModalPanel()
        {
            var root = NewRect("PFB_UiModalPanel");
            ((RectTransform)root.transform).sizeDelta = new Vector2(620f, 460f);

            var panel = root.AddComponent<Image>();
            panel.sprite = LoadSprite(ExtraSheet, "card_panel_active");
            panel.type = Image.Type.Sliced;

            var group = root.AddComponent<CanvasGroup>();

            var content = NewRect("Content");
            content.transform.SetParent(root.transform, false);
            Stretch((RectTransform)content.transform, 32f, 32f, 28f, 28f);

            var vertical = content.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 18f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            Save(root, PrefabFolder + "/PFB_UiModalPanel.prefab");
        }

        private static void BuildStatRow()
        {
            var root = NewRect("PFB_UiStatRow");
            ((RectTransform)root.transform).sizeDelta = new Vector2(480f, 42f);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var label = NewText(root.transform, "Label", "Label", 24f);
            label.alignment = TextAlignmentOptions.Left;
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            var value = NewText(root.transform, "Value", "0", 24f);
            value.alignment = TextAlignmentOptions.Right;
            value.color = Cyan;
            var valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 0.4f;

            Save(root, PrefabFolder + "/PFB_UiStatRow.prefab");
        }

        private static void BuildStudyCard()
        {
            var root = NewRect("PFB_UiStudyCard");
            ((RectTransform)root.transform).sizeDelta = new Vector2(460f, 260f);

            var panel = root.AddComponent<Image>();
            panel.sprite = LoadSprite(ExtraSheet, "card_panel_study");
            panel.type = Image.Type.Sliced;

            var medallion = NewImage(root.transform, "Medallion", LoadSprite(UiSheet, "medallion_cyan_scroll"));
            var medRect = (RectTransform)medallion.transform;
            medRect.anchorMin = new Vector2(0f, 1f);
            medRect.anchorMax = new Vector2(0f, 1f);
            medRect.pivot = new Vector2(0f, 1f);
            medRect.anchoredPosition = new Vector2(18f, -16f);
            medRect.sizeDelta = new Vector2(76f, 76f);

            var reference = NewText(root.transform, "Reference", "Reference", 22f);
            var refRect = (RectTransform)reference.transform;
            refRect.anchorMin = new Vector2(0f, 1f);
            refRect.anchorMax = new Vector2(1f, 1f);
            refRect.pivot = new Vector2(0.5f, 1f);
            refRect.anchoredPosition = new Vector2(52f, -22f);
            refRect.sizeDelta = new Vector2(-120f, 32f);
            reference.alignment = TextAlignmentOptions.Left;
            reference.color = Cyan;

            var verseText = NewText(root.transform, "VerseText", "Verse text", 20f);
            Stretch((RectTransform)verseText.transform, 20f, 20f, 104f, 18f);
            verseText.alignment = TextAlignmentOptions.TopLeft;

            var cardView = root.AddComponent<StudyCardView>();
            SetRefs(cardView, new Dictionary<string, Object>
            {
                { "_medallion", medallion },
                { "_reference", reference },
                { "_verseText", verseText }
            });

            Save(root, PrefabFolder + "/PFB_UiStudyCard.prefab");
        }

        // ---------------------------------------------------------------- helpers

        private static void SetRefs(Component component, Dictionary<string, Object> values)
        {
            var serialized = new SerializedObject(component);

            foreach (KeyValuePair<string, Object> pair in values)
            {
                SerializedProperty property = serialized.FindProperty(pair.Key);

                if (property == null)
                {
                    Debug.LogError("[UiPrefabFactory] Field '" + pair.Key + "' not found on " + component.GetType().Name);
                    continue;
                }

                property.objectReferenceValue = pair.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string sheetPath, string spriteName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Debug.LogError("[UiPrefabFactory] Sprite '" + spriteName + "' not found in " + sheetPath);
            return null;
        }

        private static GameObject NewRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            return go;
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

        private static TextMeshProUGUI NewText(Transform parent, string name, string content, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private static void Save(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[UiPrefabFactory] Saved " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string normalised = path.Replace('\\', '/').TrimEnd('/');
            string parent = Path.GetDirectoryName(normalised).Replace('\\', '/');

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(normalised));
        }
    }
}
