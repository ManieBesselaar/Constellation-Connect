namespace Constellation.Gameplay
{
    using System;
    using Constellation.Data;
    using Constellation.Localization;
    using Constellation.UI;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// One empty hex outline marking where a specific chunk belongs. A node owns one slot per chunk
    /// it needs, laid out left to right in the order the verse or bridge wants them.
    /// </summary>
    public class ChunkSlotView : MonoBehaviour, IDropTarget
    {
        [Tooltip("Hex outline drawn while the slot is waiting for its chunk.")]
        [SerializeField] private Image _outline;

        [Tooltip("Phrase shown once the chunk is placed.")]
        [SerializeField] private TextMeshProUGUI _label;

        [Tooltip("Outline colour while the slot is empty.")]
        [SerializeField] private Color _emptyColor = new Color(0.45f, 0.95f, 1f, 0.28f);

        [Tooltip("Outline colour once the chunk is placed.")]
        [SerializeField] private Color _filledColor = new Color(0.45f, 0.95f, 1f, 0.85f);

        /// <summary>Raised when the matching chunk is dropped into this slot.</summary>
        public event Action<ChunkSlotView> Filled;

        /// <inheritdoc />
        public string VerseId { get; private set; }

        /// <inheritdoc />
        public PhraseChunk ExpectedChunk { get; private set; }

        /// <inheritdoc />
        public bool IsFilled { get; private set; }

        /// <inheritdoc />
        public bool AcceptsRepeats => false;

        /// <inheritdoc />
        public int AcceptedCount { get; private set; }

        /// <summary>Position of this slot in its node's row, 0 for the first chunk.</summary>
        public int Order { get; private set; }

        private void Awake()
        {
            if (_outline != null) _outline.color = _emptyColor;
            if (_label != null) _label.text = string.Empty;
        }

        /// <summary>
        /// Binds the slot to the chunk the player owes it.
        /// </summary>
        /// <param name="verseId">Verse the chunk belongs to, or the bridge keyword for hub slots.</param>
        /// <param name="chunk">Chunk this slot accepts.</param>
        /// <param name="order">Index of the slot within its node's row.</param>
        public void Bind(string verseId, PhraseChunk chunk, int order)
        {
            VerseId = verseId;
            ExpectedChunk = chunk;
            Order = order;
            IsFilled = false;

            if (_outline != null) _outline.color = _emptyColor;
            if (_label != null) _label.text = string.Empty;
        }

        /// <inheritdoc />
        public bool TryAccept(PhraseChunk chunk)
        {
            if (IsFilled || chunk == null || ExpectedChunk == null) return false;
            if (chunk.chunkId != ExpectedChunk.chunkId) return false;

            AcceptedCount++;
            IsFilled = true;

            if (_outline != null) _outline.color = _filledColor;
            if (_label != null) _label.text = LocalizedText.Get(chunk.localizationKey, chunk.text);

            Filled?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Tints the slot for the victory beat.
        /// </summary>
        /// <param name="color">Colour to apply.</param>
        public void SetVictory(Color color)
        {
            if (_outline != null) _outline.color = color;
        }

        /// <inheritdoc />
        public void Pulse()
        {
            var tween = GetComponent<UIImageTween>();
            if (tween != null) tween.Punch(0.18f, 0.5f);
        }
    }
}
