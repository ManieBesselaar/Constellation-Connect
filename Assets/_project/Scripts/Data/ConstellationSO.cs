using System;
using System.Collections.Generic;
using UnityEngine;

namespace Constellation.Data
{
    /// <summary>
    /// A single draggable phrase fragment belonging to a verse.
    /// </summary>
    [Serializable]
    public class PhraseChunk
    {
        /// <summary>Stable identifier of the chunk, e.g. PHIL_04_013_C1.</summary>
        [Tooltip("Stable identifier of the chunk, e.g. PHIL_04_013_C1.")]
        public string chunkId;

        /// <summary>Display order within the verse, 1-based.</summary>
        [Tooltip("Display order within the verse, 1-based.")]
        public int order;

        /// <summary>Localization key holding the chunk text.</summary>
        [Tooltip("Localization key holding the chunk text.")]
        public string localizationKey;

        /// <summary>Fallback English text used when no table is available.</summary>
        [Tooltip("Fallback English text used when no table is available.")]
        public string text;

        /// <summary>True when this chunk links verses across the constellation.</summary>
        [Tooltip("True when this chunk links verses across the constellation.")]
        public bool isBridge;

        /// <summary>Bridge keyword this chunk links on, e.g. STRENGTH.</summary>
        [Tooltip("Bridge keyword this chunk links on, e.g. STRENGTH.")]
        public string bridgeTag;
    }

    /// <summary>
    /// One scripture verse and the ordered chunks that make up its text.
    /// </summary>
    [Serializable]
    public class VerseDefinition
    {
        /// <summary>Stable identifier of the verse, e.g. PHIL_04_013.</summary>
        [Tooltip("Stable identifier of the verse, e.g. PHIL_04_013.")]
        public string verseId;

        /// <summary>Human readable reference, e.g. Philippians 4:13.</summary>
        [Tooltip("Human readable reference, e.g. Philippians 4:13.")]
        public string reference;

        /// <summary>Medallion glyph identifier, e.g. icon_scroll.</summary>
        [Tooltip("Medallion glyph identifier, e.g. icon_scroll.")]
        public string iconId;

        /// <summary>Unchunked complete verse text.</summary>
        [Tooltip("Unchunked complete verse text.")]
        public string fullText;

        /// <summary>Ordered phrase chunks that build the verse.</summary>
        [Tooltip("Ordered phrase chunks that build the verse.")]
        public List<PhraseChunk> chunks = new List<PhraseChunk>();
    }

    /// <summary>
    /// Authored definition of one playable constellation theme.
    /// </summary>
    [CreateAssetMenu(menuName = "Constellation/Constellation", fileName = "Constellation")]
    public class ConstellationSO : ScriptableObject
    {
        /// <summary>Stable identifier of the theme, e.g. theme_faith_strength.</summary>
        [Tooltip("Stable identifier of the theme, e.g. theme_faith_strength.")]
        public string constellationId;

        /// <summary>Localization key for the theme title.</summary>
        [Tooltip("Localization key for the theme title.")]
        public string themeKey;

        /// <summary>Fallback English theme title.</summary>
        [Tooltip("Fallback English theme title.")]
        public string themeTitle;

        /// <summary>The three bridge keywords this constellation links on.</summary>
        [Tooltip("The three bridge keywords this constellation links on.")]
        public List<string> bridgeKeywords = new List<string>();

        /// <summary>The six verses that make up this constellation.</summary>
        [Tooltip("The six verses that make up this constellation.")]
        public List<VerseDefinition> verses = new List<VerseDefinition>();
    }
}
