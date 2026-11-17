using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Constellation.Data;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Constellation.EditorTools
{
    /// <summary>
    /// Creates the Localization settings, the en-WEB locale and the ConstellationStrings
    /// collection, then fills it with every key produced by <see cref="ConstellationImporter"/>.
    /// </summary>
    public static class LocalizationSetup
    {
        private const string LocalizationFolder = "Assets/_project/Localization";
        private const string SettingsPath = LocalizationFolder + "/LocalizationSettings.asset";
        private const string LocalePath = LocalizationFolder + "/en-WEB.asset";
        private const string TableName = "ConstellationStrings";
        private const string JsonPath = "Assets/_project/Data/JSON/constellation_connect_verses_web.json";

        /// <summary>
        /// Builds the localization assets and populates the string table from the verse JSON.
        /// </summary>
        [MenuItem("Tools/Constellation/Setup Localization")]
        public static void SetupLocalization()
        {
            var sb = new StringBuilder();

            try
            {
                EnsureFolder(LocalizationFolder);

                // 1. Settings asset. LocalizationEditorSettings finds the first one in the project.
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                    AssetDatabase.SaveAssets();
                    sb.AppendLine("CREATED settings at " + SettingsPath);
                }
                else
                {
                    sb.AppendLine("EXISTING settings at " + AssetDatabase.GetAssetPath(settings));
                }

                // 2. en-WEB locale.
                Locale locale = null;
                foreach (var existing in LocalizationEditorSettings.GetLocales())
                {
                    if (existing.Identifier.Code == "en-WEB")
                    {
                        locale = existing;
                        break;
                    }
                }

                if (locale == null)
                {
                    locale = Locale.CreateLocale(new LocaleIdentifier("en-WEB"));
                    AssetDatabase.CreateAsset(locale, LocalePath);
                    LocalizationEditorSettings.AddLocale(locale);
                    sb.AppendLine("CREATED locale en-WEB at " + LocalePath);
                }
                else
                {
                    sb.AppendLine("EXISTING locale en-WEB");
                }

                // 3. String table collection.
                var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
                if (collection == null)
                {
                    collection = LocalizationEditorSettings.CreateStringTableCollection(
                        TableName, LocalizationFolder, new List<Locale> { locale });
                    sb.AppendLine("CREATED collection " + TableName + " tables=" + collection.StringTables.Count);
                }
                else
                {
                    sb.AppendLine("EXISTING collection " + TableName + " tables=" + collection.StringTables.Count);
                }

                StringTable table = null;
                foreach (var candidate in collection.StringTables)
                {
                    if (candidate.LocaleIdentifier.Code == "en-WEB")
                    {
                        table = candidate;
                        break;
                    }
                }

                if (table == null && collection.StringTables.Count > 0)
                {
                    table = collection.StringTables[0];
                }

                if (table == null)
                {
                    sb.AppendLine("FAILED: no string table available in the collection.");
                    Debug.LogError("[LocalizationSetup] No string table in " + TableName);
                    return;
                }

                // 4. Entries.
                int added = 0;
                string json = File.ReadAllText(JsonPath);
                var payload = JsonUtility.FromJson<VerseDataWrapper>(json);

                Action<string, string> put = (key, value) =>
                {
                    if (string.IsNullOrEmpty(key)) return;
                    collection.SharedData.AddKey(key);
                    table.AddEntry(key, value ?? string.Empty);
                    added++;
                };

                put("GAME_TITLE", payload.gameTitle);
                put("TRANSLATION_ATTRIBUTION",
                    "Scripture quotations taken from the World English Bible (WEB), Public Domain.");

                foreach (var constellation in payload.constellations)
                {
                    put(constellation.themeKey, constellation.themeTitle);

                    foreach (var verse in constellation.verses)
                    {
                        put(verse.verseId + "_REF", verse.reference);
                        put(verse.verseId + "_FULL", verse.fullText);

                        foreach (var chunk in verse.chunks)
                        {
                            put(chunk.chunkId, chunk.text);
                        }
                    }
                }

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(collection.SharedData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                sb.AppendLine("ENTRIES ADDED: " + added);
                sb.AppendLine("SHARED KEYS: " + collection.SharedData.Entries.Count);
                sb.AppendLine("TABLE KEYS: " + table.Count);
            }
            catch (Exception ex)
            {
                sb.AppendLine("EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
                sb.AppendLine(ex.StackTrace);
                Debug.LogError("[LocalizationSetup] " + ex);
            }

            File.WriteAllText("step3_localization.txt", sb.ToString());
            Debug.Log("[LocalizationSetup]\n" + sb);
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
