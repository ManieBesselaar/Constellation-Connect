namespace Constellation.Gameplay
{
    using System;
    using Constellation.Data;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// The junction a bridge chunk links through, connecting every verse that shares its keyword.
    /// </summary>
    public class BridgeHubView : MonoBehaviour, IDropTarget
    {
        [Tooltip("Hub image swapped on fill.")]
        [SerializeField] private Image _hub;

        [Tooltip("Label showing the bridge keyword.")]
        [SerializeField] private TextMeshProUGUI _keywordLabel;

        [Tooltip("Sprite used once the bridge is completed.")]
        [SerializeField] private Sprite _completedSprite;

        private Color _restColor = Color.white;
        private Sprite _restSprite;

        /// <summary>Raised when a matching bridge chunk is placed, with the hub and the chunk.</summary>
        public event Action<BridgeHubView, PhraseChunk> Completed;

        /// <inheritdoc />
        public string VerseId { get; private set; }

        /// <inheritdoc />
        public PhraseChunk ExpectedChunk { get; private set; }

        /// <inheritdoc />
        public bool IsFilled { get; private set; }

        /// <inheritdoc />
        public bool AcceptsRepeats => true;

        /// <inheritdoc />
        public int AcceptedCount { get; private set; }

        /// <summary>The bridge keyword this hub represents.</summary>
        public string BridgeTag { get; private set; }

        private void Awake()
        {
            if (_hub != null)
            {
                _restColor = _hub.color;
                _restSprite = _hub.sprite;
            }
        }

        /// <summary>
        /// Binds the hub to a bridge chunk.
        /// </summary>
        /// <param name="verseId">Verse the bridge chunk belongs to.</param>
        /// <param name="chunk">The bridge chunk that satisfies this hub.</param>
        public void Bind(string verseId, PhraseChunk chunk)
        {
            VerseId = verseId;
            ExpectedChunk = chunk;
            BridgeTag = chunk != null ? chunk.bridgeTag : string.Empty;
            IsFilled = false;

            if (_keywordLabel != null) _keywordLabel.text = BridgeTag;
        }

        /// <inheritdoc />
        public bool TryAccept(PhraseChunk chunk)
        {
            if (chunk == null || ExpectedChunk == null) return false;

            // Every verse sharing a keyword carries its own bridge chunk, so the hub matches on the
            // keyword rather than on one specific chunk id.
            bool matches = chunk.chunkId == ExpectedChunk.chunkId
                || (!string.IsNullOrEmpty(BridgeTag) && chunk.bridgeTag == BridgeTag);

            if (!matches) return false;

            AcceptedCount++;

            // The hub lights up on its first chunk but stays open, so a second verse sharing the
            // keyword still has somewhere to place its own bridge chunk.
            if (!IsFilled)
            {
                IsFilled = true;
                if (_hub != null)
                {
                    if (_completedSprite != null) _hub.sprite = _completedSprite;
                    _hub.color = _restColor;
                }
            }

            Completed?.Invoke(this, chunk);
            return true;
        }

        /// <summary>
        /// Forces the hub colour, used for the victory tint.
        /// </summary>
        /// <param name="color">Colour to apply.</param>
        public void SetFillColor(Color color)
        {
            if (_hub != null) _hub.color = color;
        }

        /// <inheritdoc />
        public void Pulse()
        {
            var tween = GetComponent<Constellation.UI.UIImageTween>();
            if (tween != null) tween.Punch(0.14f, 0.5f);
        }
    }
}
