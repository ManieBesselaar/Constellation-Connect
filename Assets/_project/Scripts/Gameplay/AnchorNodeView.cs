namespace Constellation.Gameplay
{
    using System;
    using Constellation.Data;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// An anchor slot that accepts the phrase chunk belonging to its verse.
    /// </summary>
    public class AnchorNodeView : MonoBehaviour, IDropTarget
    {
        [Tooltip("Medallion image tinted on fill.")]
        [SerializeField] private Image _medallion;

        [Tooltip("Label showing the scripture reference.")]
        [SerializeField] private TextMeshProUGUI _referenceLabel;

        [Tooltip("Colour applied once the correct chunk is placed.")]
        [SerializeField] private Color _filledColor = new Color(0.35f, 0.95f, 1f, 1f);

        private Color _restColor = Color.white;

        /// <summary>Raised when a correct chunk is placed, with the verse id.</summary>
        public event Action<AnchorNodeView> Filled;

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

        private void Awake()
        {
            if (_medallion != null) _restColor = _medallion.color;
        }

        /// <summary>
        /// Binds the node to a verse and the chunk it expects.
        /// </summary>
        /// <param name="verseId">Identifier of the verse.</param>
        /// <param name="reference">Scripture reference shown under the node.</param>
        /// <param name="expectedChunk">The chunk that satisfies this node.</param>
        /// <param name="glyph">Medallion sprite for the verse.</param>
        public void Bind(string verseId, string reference, PhraseChunk expectedChunk, Sprite glyph)
        {
            VerseId = verseId;
            ExpectedChunk = expectedChunk;
            IsFilled = false;

            if (_referenceLabel != null) _referenceLabel.text = reference;
            if (_medallion != null && glyph != null) _medallion.sprite = glyph;
        }

        /// <inheritdoc />
        public bool TryAccept(PhraseChunk chunk)
        {
            if (IsFilled || chunk == null || ExpectedChunk == null) return false;
            if (chunk.chunkId != ExpectedChunk.chunkId) return false;

            AcceptedCount++;
            IsFilled = true;
            if (_medallion != null) _medallion.color = _filledColor;

            Filled?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Forces the medallion colour, used for the victory tint.
        /// </summary>
        /// <param name="color">Colour to apply.</param>
        public void SetFillColor(Color color)
        {
            if (_medallion != null) _medallion.color = color;
        }

        /// <inheritdoc />
        public void Pulse()
        {
            var tween = GetComponent<Constellation.UI.UIImageTween>();
            if (tween != null) tween.Punch(0.14f, 0.5f);
        }
    }
}
