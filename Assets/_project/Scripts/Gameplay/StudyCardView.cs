namespace Constellation.Gameplay
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A single verse preview card in the study grid: medallion, reference and the complete verse text.
    /// </summary>
    public class StudyCardView : MonoBehaviour
    {
        [Tooltip("Medallion image for the verse glyph.")]
        [SerializeField] private Image _medallion;

        [Tooltip("Scripture reference label.")]
        [SerializeField] private TextMeshProUGUI _reference;

        [Tooltip("Complete verse text.")]
        [SerializeField] private TextMeshProUGUI _verseText;

        /// <summary>
        /// Fills the card from a verse.
        /// </summary>
        /// <param name="reference">Localized scripture reference.</param>
        /// <param name="text">Localized complete verse text.</param>
        /// <param name="glyph">Medallion sprite, may be null.</param>
        public void Bind(string reference, string text, Sprite glyph)
        {
            if (_medallion != null && glyph != null) _medallion.sprite = glyph;
            if (_reference != null) _reference.text = reference;
            if (_verseText != null) _verseText.text = text;
        }
    }
}
