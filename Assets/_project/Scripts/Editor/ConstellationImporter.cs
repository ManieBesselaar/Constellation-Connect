using System.Collections.Generic;
using System.IO;
using Constellation.Data;
using UnityEditor;
using UnityEngine;

namespace Constellation.EditorTools
{
    /// <summary>
    /// Imports the repaired verse JSON into one <see cref="ConstellationSO"/> per theme.
    /// </summary>
    public static class ConstellationImporter
    {
        private const string JsonPath = "Assets/_project/Data/JSON/constellation_connect_verses_web.json";
        private const string OutputFolder = "Assets/_project/Data/Constellations";

        /// <summary>
        /// Rebuilds every ConstellationSO from the verse JSON, overwriting existing assets.
        /// </summary>
        [MenuItem("Tools/Constellation/Import Verse Data")]
        public static void ImportVerseData()
        {
            if (!File.Exists(JsonPath))
            {
                Debug.LogError("[ConstellationImporter] Verse JSON not found at " + JsonPath);
                return;
            }

            string json = File.ReadAllText(JsonPath);
            var payload = JsonUtility.FromJson<VerseDataWrapper>(json);

            if (payload == null || payload.constellations == null || payload.constellations.Count == 0)
            {
                Debug.LogError("[ConstellationImporter] Verse JSON parsed to zero constellations.");
                return;
            }

            EnsureFolder(OutputFolder);

            int created = 0;
            int totalVerses = 0;
            int totalChunks = 0;

            foreach (var source in payload.constellations)
            {
                string assetPath = OutputFolder + "/" + source.constellationId + ".asset";

                var constellation = AssetDatabase.LoadAssetAtPath<ConstellationSO>(assetPath);
                bool isNew = constellation == null;

                if (isNew)
                {
                    constellation = ScriptableObject.CreateInstance<ConstellationSO>();
                    AssetDatabase.CreateAsset(constellation, assetPath);
                }

                constellation.constellationId = source.constellationId;
                constellation.themeKey = source.themeKey;
                constellation.themeTitle = source.themeTitle;
                constellation.bridgeKeywords = new List<string>(source.bridgeKeywords ?? new List<string>());
                constellation.verses = new List<VerseDefinition>();

                foreach (var verseSource in source.verses)
                {
                    var verse = new VerseDefinition
                    {
                        verseId = verseSource.verseId,
                        reference = verseSource.reference,
                        iconId = verseSource.iconId,
                        fullText = verseSource.fullText,
                        chunks = new List<PhraseChunk>()
                    };

                    foreach (var chunkSource in verseSource.chunks)
                    {
                        verse.chunks.Add(new PhraseChunk
                        {
                            chunkId = chunkSource.chunkId,
                            order = chunkSource.order,
                            localizationKey = chunkSource.chunkId,
                            text = chunkSource.text,
                            isBridge = chunkSource.isBridge,
                            bridgeTag = chunkSource.bridgeTag
                        });
                        totalChunks++;
                    }

                    constellation.verses.Add(verse);
                    totalVerses++;
                }

                EditorUtility.SetDirty(constellation);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(string.Format(
                "[ConstellationImporter] Imported {0} constellations, {1} verses, {2} chunks from {3}.",
                created, totalVerses, totalChunks, JsonPath));
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
