namespace Constellation.Data
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Maps the verse <c>iconId</c> values used by the verse JSON onto actual sprite assets.
    /// Keeping the lookup in an asset rather than in code is what lets the whole game be
    /// reskinned by swapping sprites in one place.
    /// </summary>
    [CreateAssetMenu(menuName = "Constellation/Glyph Library", fileName = "GlyphLibrary")]
    public class GlyphLibrary : ScriptableObject
    {
        /// <summary>One iconId to sprite binding.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Identifier used by the verse data, e.g. icon_scroll.</summary>
            [Tooltip("Identifier used by the verse data, e.g. icon_scroll.")]
            public string iconId;

            /// <summary>Sprite drawn for this identifier.</summary>
            [Tooltip("Sprite drawn for this identifier.")]
            public Sprite sprite;
        }

        /// <summary>Every known icon binding.</summary>
        [Tooltip("Every known icon binding.")]
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// Resolves an icon identifier to its sprite.
        /// </summary>
        /// <param name="iconId">Identifier from the verse data.</param>
        /// <returns>The bound sprite, or null when the identifier is unknown.</returns>
        public Sprite Get(string iconId)
        {
            if (string.IsNullOrEmpty(iconId)) return null;

            foreach (Entry entry in _entries)
            {
                if (entry != null && entry.iconId == iconId) return entry.sprite;
            }

            return null;
        }

        /// <summary>
        /// Replaces the whole binding list. Used by the editor setup tool.
        /// </summary>
        /// <param name="entries">The new bindings.</param>
        public void SetEntries(List<Entry> entries)
        {
            _entries = entries ?? new List<Entry>();
        }
    }
}
