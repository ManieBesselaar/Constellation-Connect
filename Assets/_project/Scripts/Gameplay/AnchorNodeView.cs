namespace Constellation.Gameplay
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A verse's medallion. The chunk slots it owns are created under it by the connect phase and
    /// read left to right, so the medallion itself is display only.
    /// </summary>
    public class AnchorNodeView : MonoBehaviour
    {
        [Tooltip("Medallion image tinted on fill.")]
        [SerializeField] private Image _medallion;

        [Tooltip("Label showing the scripture reference.")]
        [SerializeField] private TextMeshProUGUI _referenceLabel;

        [Tooltip("Colour applied once every phrase chunk of the verse is placed.")]
        [SerializeField] private Color _filledColor = new Color(0.35f, 0.95f, 1f, 1f);

        private Color _restColor = Color.white;

        /// <summary>Identifier of the verse this node stands for.</summary>
        public string VerseId { get; private set; }

        private void Awake()
        {
            if (_medallion != null) _restColor = _medallion.color;
        }

        /// <summary>
        /// Binds the node to a verse.
        /// </summary>
        /// <param name="verseId">Identifier of the verse.</param>
        /// <param name="reference">Scripture reference shown under the node.</param>
        /// <param name="glyph">Medallion sprite for the verse.</param>
        public void Bind(string verseId, string reference, Sprite glyph)
        {
            VerseId = verseId;

            if (_referenceLabel != null) _referenceLabel.text = reference;
            if (_medallion != null && glyph != null) _medallion.sprite = glyph;
        }

        /// <summary>
        /// Lights or dims the medallion to show whether the verse has all its phrase chunks placed.
        /// </summary>
        /// <param name="lit">True once every phrase slot of this verse is filled.</param>
        public void SetLit(bool lit)
        {
            if (_medallion != null) _medallion.color = lit ? _filledColor : _restColor;
        }

        /// <summary>
        /// Forces the medallion colour, used for the victory tint.
        /// </summary>
        /// <param name="color">Colour to apply.</param>
        public void SetFillColor(Color color)
        {
            if (_medallion != null) _medallion.color = color;
        }

        /// <summary>
        /// Pulses the node to draw the player's eye.
        /// </summary>
        public void Pulse()
        {
            var tween = GetComponent<Constellation.UI.UIImageTween>();
            if (tween != null) tween.Punch(0.14f, 0.5f);
        }
    }
}
