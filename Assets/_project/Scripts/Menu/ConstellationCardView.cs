namespace Constellation.Menu
{
    using System;
    using Constellation.Data;
    using Constellation.Localization;
    using Constellation.Save;
    using Constellation.UI;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// One theme card on the constellation selection map: title, medallion, earned stars and best score.
    /// Hover raises the card to 1.05x with a cyan glow; clicking selects the theme.
    /// </summary>
    public class ConstellationCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Bindings")]
        [Tooltip("Panel image behind the card, swapped for the completed art once the theme is cleared.")]
        [SerializeField] private Image _panel;

        [Tooltip("Medallion image showing the theme's glyph.")]
        [SerializeField] private Image _medallion;

        [Tooltip("Label showing the theme title.")]
        [SerializeField] private TextMeshProUGUI _title;

        [Tooltip("Label showing the best score for this theme.")]
        [SerializeField] private TextMeshProUGUI _bestScore;

        [Tooltip("Parent holding exactly three star images.")]
        [SerializeField] private Transform _starsRoot;

        [Tooltip("Sprite used for a star that has not been earned.")]
        [SerializeField] private Sprite _starEmpty;

        [Tooltip("Sprite used for an earned star.")]
        [SerializeField] private Sprite _starGold;

        [Tooltip("Panel art shown before the theme is completed.")]
        [SerializeField] private Sprite _panelIncomplete;

        [Tooltip("Panel art shown once the theme is completed.")]
        [SerializeField] private Sprite _panelComplete;

        [Tooltip("Tween driving the hover scale and glow.")]
        [SerializeField] private UIImageTween _tween;

        private Image[] _stars;
        private CanvasGroup _canvasGroup;

        /// <summary>Raised when the card is clicked, carrying the theme it represents.</summary>
        public event Action<ConstellationSO> Selected;

        /// <summary>The theme this card represents.</summary>
        public ConstellationSO Constellation { get; private set; }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            CacheStars();
        }

        /// <summary>
        /// Fills the card from a theme and its saved progression.
        /// </summary>
        /// <param name="constellation">Theme to display.</param>
        /// <param name="progress">Saved progression for the theme, or null when never played.</param>
        /// <param name="glyphs">Library resolving the theme's glyph sprite.</param>
        public void Bind(ConstellationSO constellation, ThemeProgress progress, GlyphLibrary glyphs)
        {
            Constellation = constellation;
            if (constellation == null) return;

            if (_title != null)
            {
                _title.text = LocalizedText.Get(constellation.themeKey, constellation.themeTitle);
            }

            if (_medallion != null && glyphs != null && constellation.verses.Count > 0)
            {
                Sprite glyph = glyphs.Get(constellation.verses[0].iconId);
                if (glyph != null) _medallion.sprite = glyph;
            }

            int stars = progress != null ? progress.Stars : 0;
            int bestScore = progress != null ? progress.BestScore : 0;
            bool completed = progress != null && progress.Completed;

            if (_bestScore != null)
            {
                _bestScore.text = bestScore > 0 ? "Best " + bestScore : "Not played";
            }

            if (_panel != null)
            {
                Sprite wanted = completed && _panelComplete != null ? _panelComplete : _panelIncomplete;
                if (wanted != null) _panel.sprite = wanted;
            }

            CacheStars();

            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] == null) continue;

                bool earned = i < stars;
                Sprite wantedStar = earned ? _starGold : _starEmpty;
                if (wantedStar != null) _stars[i].sprite = wantedStar;
                _stars[i].color = Color.white;
            }
        }

        /// <summary>
        /// Enables or disables interaction, used while the board is transitioning.
        /// </summary>
        /// <param name="interactable">True to allow clicks.</param>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = interactable;
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tween != null) _tween.SetHovered(true);
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tween != null) _tween.SetHovered(false);
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Constellation == null) return;
            Debug.Log($"[ConstellationCardView] Theme selected: {Constellation.constellationId}");
            Selected?.Invoke(Constellation);
        }

        private void CacheStars()
        {
            if (_starsRoot == null)
            {
                _stars = Array.Empty<Image>();
                return;
            }

            var found = new System.Collections.Generic.List<Image>();
            for (int i = 0; i < _starsRoot.childCount; i++)
            {
                var image = _starsRoot.GetChild(i).GetComponent<Image>();
                if (image != null) found.Add(image);
            }

            _stars = found.ToArray();
        }
    }
}
