namespace Constellation.Gameplay
{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Phase 3 of the loop: the victory bloom plus a summary of time, score and stars.
    /// </summary>
    public class VictoryViewController : MonoBehaviour
    {
        [Header("Roots")]
        [Tooltip("Panel shown when the constellation is completed.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Celebratory bloom played on show.")]
        [SerializeField] private ParticleSystem _bloom;

        [Header("Labels")]
        [Tooltip("Headline label.")]
        [SerializeField] private TextMeshProUGUI _titleLabel;

        [Tooltip("Final score.")]
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [Tooltip("Completion time.")]
        [SerializeField] private TextMeshProUGUI _timeLabel;

        [Tooltip("Accuracy note.")]
        [SerializeField] private TextMeshProUGUI _accuracyLabel;

        [Header("Stars")]
        [Tooltip("Exactly three star images.")]
        [SerializeField] private Image[] _stars = new Image[3];

        [Tooltip("Sprite used for an earned star.")]
        [SerializeField] private Sprite _starGold;

        [Tooltip("Sprite used for a star that was not earned.")]
        [SerializeField] private Sprite _starEmpty;

        [Header("Buttons")]
        [Tooltip("Returns to the constellation map.")]
        [SerializeField] private Button _menuButton;

        [Tooltip("Replays the same constellation.")]
        [SerializeField] private Button _replayButton;

        /// <summary>Raised when the player asks to go back to the menu.</summary>
        public event Action ReturnToMenuRequested;

        /// <summary>Raised when the player asks to replay the same theme.</summary>
        public event Action ReplayRequested;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_menuButton != null) _menuButton.onClick.AddListener(HandleMenuPressed);
            if (_replayButton != null) _replayButton.onClick.AddListener(HandleReplayPressed);
        }

        private void OnDestroy()
        {
            if (_menuButton != null) _menuButton.onClick.RemoveListener(HandleMenuPressed);
            if (_replayButton != null) _replayButton.onClick.RemoveListener(HandleReplayPressed);
        }

        /// <summary>
        /// Shows the summary and plays the bloom.
        /// </summary>
        /// <param name="stars">Stars earned, 1-3.</param>
        /// <param name="score">Final score.</param>
        /// <param name="timeSeconds">Completion time in seconds.</param>
        /// <param name="hintsUsed">Number of hints taken.</param>
        public void Show(int stars, int score, float timeSeconds, int hintsUsed)
        {
            if (_panel != null) _panel.SetActive(true);

            if (_titleLabel != null) _titleLabel.text = "Constellation Complete";

            if (_scoreLabel != null) _scoreLabel.text = score.ToString("N0");

            if (_timeLabel != null)
            {
                int minutes = Mathf.FloorToInt(timeSeconds / 60f);
                int seconds = Mathf.FloorToInt(timeSeconds % 60f);
                _timeLabel.text = $"{minutes:00}:{seconds:00}";
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = hintsUsed == 0 ? "No hints used" : $"{hintsUsed} hint{(hintsUsed == 1 ? string.Empty : "s")} used";
            }

            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] == null) continue;

                Sprite wanted = i < stars ? _starGold : _starEmpty;
                if (wanted != null) _stars[i].sprite = wanted;
                _stars[i].color = Color.white;
            }

            if (_bloom != null)
            {
                _bloom.Clear();
                _bloom.Play();
            }

            Debug.Log($"[VictoryViewController] Victory shown: stars={stars} score={score} time={timeSeconds:0.0}s hints={hintsUsed}.");
        }

        private void HandleMenuPressed()
        {
            Debug.Log("[VictoryViewController] Returning to the constellation map.");
            ReturnToMenuRequested?.Invoke();
        }

        private void HandleReplayPressed()
        {
            Debug.Log("[VictoryViewController] Replaying the constellation.");
            ReplayRequested?.Invoke();
        }
    }
}
