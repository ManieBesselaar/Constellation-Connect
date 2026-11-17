using System;
using System.Collections.Generic;

namespace Constellation.Data
{
    /// <summary>
    /// Root of the imported verse JSON payload.
    /// </summary>
    [Serializable]
    public class VerseDataWrapper
    {
        /// <summary>Schema version of the source document.</summary>
        public string schemaVersion;

        /// <summary>Human readable game title.</summary>
        public string gameTitle;

        /// <summary>Every constellation theme in the document.</summary>
        public List<ConstellationPayload> constellations;
    }

    /// <summary>
    /// One constellation theme as it appears in the source JSON.
    /// </summary>
    [Serializable]
    public class ConstellationPayload
    {
        /// <summary>Stable identifier of the theme.</summary>
        public string constellationId;

        /// <summary>Localization key for the theme title.</summary>
        public string themeKey;

        /// <summary>Fallback English theme title.</summary>
        public string themeTitle;

        /// <summary>The three bridge keywords this constellation links on.</summary>
        public List<string> bridgeKeywords;

        /// <summary>The verses that make up this constellation.</summary>
        public List<VersePayload> verses;
    }

    /// <summary>
    /// One verse as it appears in the source JSON.
    /// </summary>
    [Serializable]
    public class VersePayload
    {
        /// <summary>Stable identifier of the verse.</summary>
        public string verseId;

        /// <summary>Human readable scripture reference.</summary>
        public string reference;

        /// <summary>Medallion glyph identifier.</summary>
        public string iconId;

        /// <summary>Unchunked complete verse text.</summary>
        public string fullText;

        /// <summary>Ordered phrase chunks that build the verse.</summary>
        public List<ChunkPayload> chunks;
    }

    /// <summary>
    /// One draggable phrase chunk as it appears in the source JSON.
    /// </summary>
    [Serializable]
    public class ChunkPayload
    {
        /// <summary>Stable identifier of the chunk.</summary>
        public string chunkId;

        /// <summary>Display order within the verse, 1-based.</summary>
        public int order;

        /// <summary>Phrase text.</summary>
        public string text;

        /// <summary>True when this chunk links verses across the constellation.</summary>
        public bool isBridge;

        /// <summary>Bridge keyword this chunk links on.</summary>
        public string bridgeTag;
    }
}
