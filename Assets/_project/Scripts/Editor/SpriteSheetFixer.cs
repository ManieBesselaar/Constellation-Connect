namespace Constellation.EditorTools
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.U2D.Sprites;
    using UnityEngine;

    /// <summary>
    /// Applies the semantic sub-sprite naming to the Constellation art sheets.
    /// Re-slicing a sheet in the Sprite Editor resets its sub-sprite names to
    /// "&lt;Texture&gt;_N", which silently breaks every prefab that binds a sprite by name,
    /// so this runs both from the menu and once automatically after every asset import pass.
    /// Names are matched by the sheet's own auto-generated names, never by array index,
    /// because deleted slices leave gaps in the index sequence.
    /// </summary>
    public static class SpriteSheetFixer
    {
        /// <summary>Main sheet: 18 medallions, 3 hex tiles, 6 star links, 5 pills, bars/gears/cursor.</summary>
        private const string UiSheet = "Assets/_project/Sprites/UI/Constellation UI.png";

        /// <summary>Glyph sheet: rows 1-2 are the used set, rows 3-4 are identical duplicates kept as spares.</summary>
        private const string IconSheet = "Assets/_project/Sprites/UI/Constellation UI icons.png";

        /// <summary>Extra sheet: star ratings, bridge hubs and card panels.</summary>
        private const string ExtraSheet = "Assets/_project/Sprites/UI/Constellation UI additional icons.png";

        private static readonly Dictionary<string, string> MainNames = new Dictionary<string, string>
        {
            { "Constellation UI_0", "medallion_navy_scroll" },
            { "Constellation UI_1", "medallion_navy_dove" },
            { "Constellation UI_2", "medallion_navy_shield" },
            { "Constellation UI_3", "medallion_cyan_scroll" },
            { "Constellation UI_39", "medallion_cyan_dove" },
            { "Constellation UI_38", "medallion_cyan_shield" },
            { "Constellation UI_4", "medallion_gold_scroll" },
            { "Constellation UI_41", "medallion_gold_dove" },
            { "Constellation UI_42", "medallion_gold_shield" },
            { "Constellation UI_32", "medallion_navy_anchor" },
            { "Constellation UI_6", "medallion_navy_heart_cross" },
            { "Constellation UI_33", "medallion_navy_heart" },
            { "Constellation UI_7", "medallion_cyan_anchor" },
            { "Constellation UI_37", "medallion_cyan_heart_cross" },
            { "Constellation UI_40", "medallion_cyan_heart" },
            { "Constellation UI_43", "medallion_gold_anchor" },
            { "Constellation UI_44", "medallion_gold_heart_cross" },
            { "Constellation UI_45", "medallion_gold_heart" },

            { "Constellation UI_5", "hex_tile_navy" },
            { "Constellation UI_46", "hex_tile_cyan" },
            { "Constellation UI_47", "hex_tile_gold" },

            { "Constellation UI_8", "link_cyan_arc_a" },
            { "Constellation UI_9", "link_cyan_arc_b" },
            { "Constellation UI_10", "link_cyan_loop" },
            { "Constellation UI_11", "link_gold_s_curve" },
            { "Constellation UI_12", "link_gold_short" },
            { "Constellation UI_13", "link_gold_arc" },

            { "Constellation UI_15", "pill_light" },
            { "Constellation UI_16", "pill_navy" },
            { "Constellation UI_22", "pill_navy_wide" },
            { "Constellation UI_14", "pill_navy_tall" },
            { "Constellation UI_35", "pill_navy_flat" },

            { "Constellation UI_23", "bar_segmented_long" },
            { "Constellation UI_24", "bar_segment" },
            { "Constellation UI_25", "misc_fragment" },
            { "Constellation UI_28", "icon_gear_cyan" },
            { "Constellation UI_29", "icon_gear_gold" },
            { "Constellation UI_30", "cursor_hand" }
        };

        private static readonly Dictionary<string, string> IconNames = new Dictionary<string, string>
        {
            { "Constellation UI icons_7", "glyph_scroll" },
            { "Constellation UI icons_0", "glyph_dove" },
            { "Constellation UI icons_1", "glyph_shield" },
            { "Constellation UI icons_2", "glyph_anchor" },
            { "Constellation UI icons_3", "glyph_heart_cross" },
            { "Constellation UI icons_4", "glyph_crown" },
            { "Constellation UI icons_9", "glyph_olive_branch" },
            { "Constellation UI icons_21", "glyph_lamp" },
            { "Constellation UI icons_22", "glyph_shepherd" },
            { "Constellation UI icons_23", "glyph_mountain" },
            { "Constellation UI icons_24", "glyph_star" },
            { "Constellation UI icons_25", "glyph_open_bible" },
            { "Constellation UI icons_26", "glyph_scroll_gold" },
            { "Constellation UI icons_27", "glyph_dove_inactive" },

            { "Constellation UI icons_30", "glyph_scroll_alt" },
            { "Constellation UI icons_31", "glyph_dove_alt" },
            { "Constellation UI icons_32", "glyph_shield_alt" },
            { "Constellation UI icons_33", "glyph_anchor_alt" },
            { "Constellation UI icons_34", "glyph_heart_cross_alt" },
            { "Constellation UI icons_35", "glyph_crown_alt" },
            { "Constellation UI icons_38", "glyph_olive_branch_alt" },
            { "Constellation UI icons_56", "glyph_lamp_alt" },
            { "Constellation UI icons_57", "glyph_shepherd_alt" },
            { "Constellation UI icons_58", "glyph_mountain_alt" },
            { "Constellation UI icons_59", "glyph_star_alt" },
            { "Constellation UI icons_60", "glyph_open_bible_alt" },
            { "Constellation UI icons_61", "glyph_scroll_gold_alt" },
            { "Constellation UI icons_64", "glyph_dove_inactive_alt" }
        };

        private static readonly Dictionary<string, string> ExtraNames = new Dictionary<string, string>
        {
            { "Constellation UI additional icons_0", "star_empty" },
            { "Constellation UI additional icons_1", "star_bronze" },
            { "Constellation UI additional icons_2", "star_silver" },
            { "Constellation UI additional icons_3", "star_gold" },
            { "Constellation UI additional icons_12", "bridge_hub_inactive" },
            { "Constellation UI additional icons_13", "bridge_hub_active" },
            { "Constellation UI additional icons_14", "bridge_hub_completed" },
            { "Constellation UI additional icons_18", "card_panel_study" },
            { "Constellation UI additional icons_19", "card_panel_active" },
            { "Constellation UI additional icons_20", "card_panel_complete" }
        };

        /// <summary>
        /// Reapplies every semantic sub-sprite name, skipping sprites that already carry one.
        /// </summary>
        [MenuItem("Tools/Constellation/Fix Sprite Names")]
        public static void FixSpriteNames()
        {
            int renamed = 0;
            renamed += Apply(UiSheet, MainNames);
            renamed += Apply(IconSheet, IconNames);
            renamed += Apply(ExtraSheet, ExtraNames);

            if (renamed == 0)
            {
                Debug.Log("[SpriteSheetFixer] All sub-sprite names already correct.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpriteSheetFixer] Renamed {renamed} sub-sprites across the three art sheets.");
        }

        private static int Apply(string sheetPath, Dictionary<string, string> table)
        {
            var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[SpriteSheetFixer] No TextureImporter at " + sheetPath);
                return 0;
            }

            // TextureImporter.spritesheet is a no-op in Unity 6, so slice names must be edited
            // through the sprite data provider or the write is silently discarded.
            var factory = new SpriteDataProviderFactories();
            factory.Init();

            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                Debug.LogError("[SpriteSheetFixer] No sprite data provider for " + sheetPath);
                return 0;
            }

            provider.InitSpriteEditorDataProvider();

            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();

            SpriteRect[] rects = provider.GetSpriteRects();
            var taken = new HashSet<string>();
            foreach (SpriteRect rect in rects)
            {
                taken.Add(rect.name);
            }

            int renamed = 0;

            foreach (SpriteRect rect in rects)
            {
                string wanted;

                // A slice whose semantic name is already taken was renamed by an earlier pass;
                // renaming it again would create a duplicate key in the sheet.
                if (table.TryGetValue(rect.name, out wanted) && !taken.Contains(wanted))
                {
                    rect.name = wanted;
                    renamed++;
                }
            }

            if (renamed == 0) return 0;

            // Rewrites the canonical slice data, keeping every existing pivot and 9-slice border.
            provider.SetSpriteRects(rects);
            provider.Apply();

            // The name records only reach the Sprite sub-assets when the texture is reimported.
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[SpriteSheetFixer] {System.IO.Path.GetFileName(sheetPath)}: renamed {renamed} of {rects.Length} slices (nameProvider={nameProvider != null}).");
            return renamed;
        }
    }
}
