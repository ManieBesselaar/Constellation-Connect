namespace Constellation.Menu
{
    using System.Collections.Generic;
    using Constellation.Core;
    using Constellation.Data;
    using Constellation.Localization;
    using Constellation.Save;
    using Lifestories.Navigation;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Builds the constellation selection map from the theme assets and routes the player
    /// into the gameplay scene for the theme they pick.
    /// </summary>
    public class MainMenuViewController : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("The five playable constellations, in display order.")]
        [SerializeField] private List<ConstellationSO> _constellations = new List<ConstellationSO>();

        [Tooltip("Library mapping verse icon ids to sprites.")]
        [SerializeField] private GlyphLibrary _glyphs;

        [Header("Layout")]
        [Tooltip("Card prefab instantiated once per constellation.")]
        [SerializeField] private ConstellationCardView _cardPrefab;

        [Tooltip("Grid the cards are parented into.")]
        [SerializeField] private RectTransform _cardContainer;

        [Tooltip("Title label at the top of the menu.")]
        [SerializeField] private TextMeshProUGUI _titleLabel;

        [Header("Navigation")]
        [Tooltip("Gameplay scene requested when a theme is chosen.")]
        [SerializeField] private GameSceneSO _gameplayScene;

        [Tooltip("Channel listened to by the framework SceneLoaderManager.")]
        [SerializeField] private SceneLoadEventChannelSO _loadSceneRequestChannel;

        [Header("Settings")]
        [Tooltip("Settings modal opened by the gear button.")]
        [SerializeField] private SettingsModalView _settingsModal;

        [Tooltip("Button that opens the settings modal.")]
        [SerializeField] private Button _settingsButton;

        private readonly List<ConstellationCardView> _cards = new List<ConstellationCardView>();
        private bool _isNavigating;

        private void Start()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = LocalizedText.Get("GAME_TITLE", "Constellation Connect");
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OpenSettings);
            }

            BuildCards();
        }

        private void OnDestroy()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OpenSettings);
            }
        }

        private void BuildCards()
        {
            if (_cardPrefab == null || _cardContainer == null)
            {
                Debug.LogError("[MainMenuViewController] Card prefab or card container is not assigned.");
                return;
            }

            foreach (ConstellationSO constellation in _constellations)
            {
                if (constellation == null) continue;

                ConstellationCardView card = Instantiate(_cardPrefab, _cardContainer);
                card.name = "Card_" + constellation.constellationId;
                card.Bind(constellation, SaveService.GetTheme(constellation.constellationId), _glyphs);
                card.Selected += HandleThemeSelected;
                _cards.Add(card);
            }

            Debug.Log($"[MainMenuViewController] Built {_cards.Count} theme cards.");
        }

        private void OpenSettings()
        {
            if (_settingsModal == null) return;
            _settingsModal.Open();
        }

        private void HandleThemeSelected(ConstellationSO constellation)
        {
            if (_isNavigating)
            {
                Debug.LogWarning("[MainMenuViewController] Selection ignored: a scene load is already in progress.");
                return;
            }

            if (_loadSceneRequestChannel == null || _gameplayScene == null)
            {
                Debug.LogError("[MainMenuViewController] Cannot start gameplay: scene asset or load channel is not assigned.");
                return;
            }

            _isNavigating = true;
            GameSession.SelectedConstellation = constellation;

            foreach (ConstellationCardView card in _cards)
            {
                card.SetInteractable(false);
            }

            Debug.Log($"[MainMenuViewController] Loading gameplay for '{constellation.constellationId}'.");
            _loadSceneRequestChannel.RaiseEvent(_gameplayScene);
        }
    }
}
