namespace Constellation.Gameplay
{
    using System;
    using Constellation.Audio;
    using Constellation.Data;
    using Constellation.Localization;
    using Constellation.UI;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Phase 1 of the loop: the six verses of the chosen constellation are shown on a 2x3 grid
    /// for one of 30, 60, 90 seconds or indefinitely in Zen mode. Starting early awards 5 points
    /// for every second left on the clock.
    /// </summary>
    public class StudyPhaseController : MonoBehaviour
    {
        /// <summary>Points awarded per whole second skipped by starting the constellation early.</summary>
        public const int SkipBonusPerSecond = 5;

        [Header("Roots")]
        [Tooltip("Panel shown while the study phase is active.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Grid the verse cards are parented into.")]
        [SerializeField] private RectTransform _grid;

        [Header("Prefabs")]
        [Tooltip("Verse card prefab.")]
        [SerializeField] private StudyCardView _cardPrefab;

        [Tooltip("Library mapping verse icon ids to sprites.")]
        [SerializeField] private GlyphLibrary _glyphs;

        [Header("HUD")]
        [Tooltip("Countdown bar driven by the remaining time.")]
        [SerializeField] private CountdownBar _countdownBar;

        [Tooltip("Label showing the remaining time.")]
        [SerializeField] private TextMeshProUGUI _timerLabel;

        [Tooltip("Label showing the active theme title.")]
        [SerializeField] private TextMeshProUGUI _themeLabel;

        [Tooltip("Instructional copy above the grid.")]
        [SerializeField] private TextMeshProUGUI _instructionLabel;

        [Tooltip("Button that starts the constellation early.")]
        [SerializeField] private Button _startButton;

        private const int WarningTickSeconds = 10;

        private float _remaining;
        private float _duration;
        private int _lastWarningSecond = -1;
        private bool _zen;
        private bool _running;

        /// <summary>Raised when the study phase ends, carrying the skip bonus earned.</summary>
        public event Action<int> Completed;

        /// <summary>Seconds the player actually spent studying.</summary>
        public float ElapsedSeconds { get; private set; }

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_startButton != null) _startButton.onClick.AddListener(HandleStartPressed);
        }

        private void OnDestroy()
        {
            if (_startButton != null) _startButton.onClick.RemoveListener(HandleStartPressed);
        }

        private void Update()
        {
            if (!_running || _zen) return;

            _remaining -= Time.deltaTime;
            _remaining = Mathf.Max(0f, _remaining);
            ElapsedSeconds += Time.deltaTime;

            _countdownBar.SetFraction(_duration > 0f ? _remaining / _duration : 0f);
            _countdownBar.TickPulse(Time.unscaledDeltaTime);

            if (_timerLabel != null) _timerLabel.text = Mathf.CeilToInt(_remaining) + "s";

            TickWarning();

            if (_remaining <= 0f)
            {
                Finish(0);
            }
        }

        /// <summary>
        /// Builds the verse grid and starts the countdown.
        /// </summary>
        /// <param name="constellation">Theme being played.</param>
        /// <param name="durationSeconds">Study time in seconds, or 0 for Zen mode.</param>
        public void Begin(ConstellationSO constellation, int durationSeconds)
        {
            if (constellation == null)
            {
                Debug.LogError("[StudyPhaseController] Cannot begin: no constellation supplied.");
                return;
            }

            ClearGrid();

            _zen = durationSeconds <= 0;
            _duration = Mathf.Max(1f, durationSeconds);
            _remaining = _duration;
            ElapsedSeconds = 0f;
            _lastWarningSecond = -1;
            _running = true;

            if (_panel != null) _panel.SetActive(true);

            if (_themeLabel != null)
            {
                _themeLabel.text = LocalizedText.Get(constellation.themeKey, constellation.themeTitle);
            }

            if (_instructionLabel != null)
            {
                _instructionLabel.text = _zen
                    ? "Study the verses, then start the constellation when you are ready."
                    : "Memorise the verses. Starting early awards bonus points.";
            }

            if (_timerLabel != null)
            {
                _timerLabel.text = _zen ? "ZEN" : Mathf.CeilToInt(_remaining) + "s";
            }

            if (_countdownBar != null)
            {
                _countdownBar.SetFraction(_zen ? 1f : 1f);
            }

            BuildGrid(constellation);

            Debug.Log($"[StudyPhaseController] Study phase started for '{constellation.constellationId}' " +
                      $"({(_zen ? "Zen" : durationSeconds + "s")}), {constellation.verses.Count} cards.");
        }

        private void BuildGrid(ConstellationSO constellation)
        {
            if (_cardPrefab == null || _grid == null)
            {
                Debug.LogError("[StudyPhaseController] Card prefab or grid is not assigned.");
                return;
            }

            foreach (VerseDefinition verse in constellation.verses)
            {
                StudyCardView card = Instantiate(_cardPrefab, _grid);

                Sprite glyph = _glyphs != null ? _glyphs.Get(verse.iconId) : null;
                string reference = LocalizedText.Get(verse.verseId + "_REF", verse.reference);
                string text = LocalizedText.Get(verse.verseId + "_FULL", verse.fullText);

                card.Bind(reference, text, glyph);
            }
        }

        /// <summary>
        /// Plays the gentle warning tick once per whole second over the last ten seconds of the timer.
        /// </summary>
        private void TickWarning()
        {
            if (_remaining <= 0f) return;

            int whole = Mathf.CeilToInt(_remaining);

            if (whole > WarningTickSeconds || whole == _lastWarningSecond) return;

            _lastWarningSecond = whole;
            AudioManager.PlayTimerTickWarning();
        }

        private void HandleStartPressed()
        {
            if (!_running) return;

            int remaining = _zen ? 0 : Mathf.FloorToInt(_remaining);
            Finish(remaining * SkipBonusPerSecond);
        }

        private void Finish(int bonus)
        {
            if (!_running) return;

            _running = false;

            if (_panel != null) _panel.SetActive(false);

            Debug.Log($"[StudyPhaseController] Study phase finished after {ElapsedSeconds:0.0}s, skip bonus {bonus}.");
            Completed?.Invoke(bonus);
        }

        private void ClearGrid()
        {
            if (_grid == null) return;

            for (int i = _grid.childCount - 1; i >= 0; i--)
            {
                Destroy(_grid.GetChild(i).gameObject);
            }
        }
    }
}
