namespace Constellation.Gameplay
{
    using System;
    using System.Collections.Generic;
    using Constellation.Audio;
    using Constellation.Core;
    using Constellation.Data;
    using Constellation.Save;
    using Lifestories.Navigation;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Drives the three-phase loop: study the verses, connect the constellation, celebrate.
    /// Owns scoring and the save round-trip, and routes the player back to the menu or a replay.
    /// </summary>
    public class GamePhaseStateMachine : MonoBehaviour
    {
        /// <summary>The phase the gameplay scene is currently showing.</summary>
        public enum Phase
        {
            /// <summary>Phase 1: the run's verses are displayed for the study timer.</summary>
            Study,

            /// <summary>Phase 2: tiles are dragged onto anchors and bridge hubs.</summary>
            Connect,

            /// <summary>Phase 3: victory summary.</summary>
            Victory
        }

        [Header("Content")]
        [Tooltip("Theme used when the scene is opened directly from the editor, with nothing chosen on the menu.")]
        [SerializeField] private ConstellationSO _fallbackConstellation;

        [Tooltip("How many of the theme's verses this run is built around. They are drawn at random on every run.")]
        [Range(1, 6)]
        [SerializeField] private int _versesPerRun = 3;

        [Header("Phase Controllers")]
        [Tooltip("Phase 1 controller.")]
        [SerializeField] private StudyPhaseController _study;

        [Tooltip("Phase 2 controller.")]
        [SerializeField] private ConnectPhaseController _connect;

        [Tooltip("Phase 3 controller.")]
        [SerializeField] private VictoryViewController _victory;

        [Header("Navigation")]
        [Tooltip("Scene asset for the constellation map.")]
        [SerializeField] private GameSceneSO _mainMenuScene;

        [Tooltip("Scene asset for this gameplay scene, used for replays.")]
        [SerializeField] private GameSceneSO _gameplayScene;

        [Tooltip("Channel listened to by the framework SceneLoaderManager.")]
        [SerializeField] private SceneLoadEventChannelSO _loadSceneRequestChannel;

        [Header("HUD")]
        [Tooltip("Score readout shown during the connect phase.")]
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [Header("Instructions")]
        [Tooltip("How-to-play overlay. When assigned, the study phase waits until the player closes it.")]
        [SerializeField] private HowToPlayView _instructions;

        private ConstellationSO _sourceConstellation;
        private ConstellationSO _constellation;
        private float _runStartTime;
        private int _score;
        private bool _isNavigating;

        /// <summary>The phase currently being played.</summary>
        public Phase CurrentPhase { get; private set; }

        private void Awake()
        {
            if (_study != null) _study.Completed += HandleStudyCompleted;
            if (_connect != null)
            {
                _connect.ConstellationCompleted += HandleConstellationCompleted;
                _connect.ScoreChanged += HandleScoreChanged;
            }

            if (_victory != null)
            {
                _victory.ReturnToMenuRequested += HandleReturnToMenu;
                _victory.ReplayRequested += HandleReplay;
            }
        }

        private void OnDestroy()
        {
            if (_study != null) _study.Completed -= HandleStudyCompleted;
            if (_connect != null)
            {
                _connect.ConstellationCompleted -= HandleConstellationCompleted;
                _connect.ScoreChanged -= HandleScoreChanged;
            }

            if (_victory != null)
            {
                _victory.ReturnToMenuRequested -= HandleReturnToMenu;
                _victory.ReplayRequested -= HandleReplay;
            }

            if (_instructions != null) _instructions.Dismissed -= HandleInstructionsDismissed;
        }

        private void Start()
        {
            _sourceConstellation = GameSession.SelectedConstellation != null
                ? GameSession.SelectedConstellation
                : _fallbackConstellation;

            if (_sourceConstellation == null)
            {
                Debug.LogError("[GamePhaseStateMachine] No constellation selected and no fallback assigned.");
                return;
            }

            _constellation = BuildRunConstellation(_sourceConstellation, _versesPerRun);

            Debug.Log($"[GamePhaseStateMachine] Starting run for '{_constellation.constellationId}': " +
                      $"{_constellation.verses.Count} of {_sourceConstellation.verses.Count} verses.");

            // Hold the run on the how-to overlay: the study countdown must not start while the
            // player is still reading the controls.
            if (_instructions != null)
            {
                _instructions.Dismissed += HandleInstructionsDismissed;
                _instructions.Show();
                Debug.Log("[GamePhaseStateMachine] Waiting for the how-to-play overlay.");
                return;
            }

            EnterStudy();
        }

        /// <summary>
        /// Narrows a theme down to the verses this run is played on. The verses are drawn at random,
        /// then kept in their authored order so the board still reads the way the theme was designed.
        /// </summary>
        /// <param name="source">Theme chosen on the menu.</param>
        /// <param name="verseCount">How many verses to play this run.</param>
        /// <returns>The trimmed runtime theme, or the source itself when no trimming is needed.</returns>
        private ConstellationSO BuildRunConstellation(ConstellationSO source, int verseCount)
        {
            if (source == null || verseCount <= 0 || source.verses.Count <= verseCount) return source;

            // Partial Fisher-Yates: the first verseCount entries of the index list become the draw.
            var indices = new List<int>(source.verses.Count);
            for (int i = 0; i < source.verses.Count; i++) indices.Add(i);

            for (int i = 0; i < verseCount; i++)
            {
                int j = UnityEngine.Random.Range(i, indices.Count);
                int swap = indices[i];
                indices[i] = indices[j];
                indices[j] = swap;
            }

            indices.Sort(0, verseCount, Comparer<int>.Default);

            // A runtime copy keeps the authored asset untouched and carries the same identity, so the
            // save records, replays and the menu card all still refer to the theme that was chosen.
            ConstellationSO run = ScriptableObject.CreateInstance<ConstellationSO>();
            run.name = source.name + "_Run";
            run.constellationId = source.constellationId;
            run.themeKey = source.themeKey;
            run.themeTitle = source.themeTitle;
            run.bridgeKeywords = new List<string>(source.bridgeKeywords);
            run.verses = new List<VerseDefinition>(verseCount);

            for (int i = 0; i < verseCount; i++)
            {
                run.verses.Add(source.verses[indices[i]]);
            }

            return run;
        }

        private void HandleInstructionsDismissed()
        {
            if (_instructions != null) _instructions.Dismissed -= HandleInstructionsDismissed;
            EnterStudy();
        }

        private void EnterStudy()
        {
            CurrentPhase = Phase.Study;
            _score = 0;
            _runStartTime = Time.unscaledTime;

            UpdateScoreLabel();

            int duration = SaveService.Data.ZenMode ? 0 : SaveService.Data.StudyDurationSeconds;
            if (duration <= 0 && !SaveService.Data.ZenMode) duration = 60;

            Debug.Log($"[GamePhaseStateMachine] Phase 1 (Study), timer {(duration <= 0 ? "Zen" : duration + "s")}.");
            _study.Begin(_constellation, duration);
        }

        private void HandleStudyCompleted(int skipBonus)
        {
            _score = skipBonus;
            UpdateScoreLabel();

            Debug.Log($"[GamePhaseStateMachine] Phase 2 (Connect) with starting score {_score}.");
            CurrentPhase = Phase.Connect;
            _connect.Begin(_constellation, _score);
        }

        private void HandleScoreChanged(int score)
        {
            _score = score;
            UpdateScoreLabel();
        }

        private void HandleConstellationCompleted(int score)
        {
            _score = score;

            float elapsed = Time.unscaledTime - _runStartTime;
            int stars = ComputeStars();
            int hints = _connect.HintsUsed;

            Debug.Log($"[GamePhaseStateMachine] Phase 3 (Victory): score={score} time={elapsed:0.0}s stars={stars} hints={hints}.");

            CurrentPhase = Phase.Victory;
            _connect.ApplyVictoryTint();
            AudioManager.PlayConstellationVictory();
            SaveService.RecordResult(_constellation.constellationId, stars, score, elapsed);
            _victory.Show(stars, score, elapsed, hints);
        }

        private int ComputeStars()
        {
            int hints = _connect != null ? _connect.HintsUsed : 0;

            if (hints == 0) return 3;
            if (hints <= 2) return 2;
            return 1;
        }

        private void HandleReturnToMenu()
        {
            RequestScene(_mainMenuScene);
        }

        private void HandleReplay()
        {
            // Hand the theme back untouched so the replay draws a fresh set of verses.
            GameSession.SelectedConstellation = _sourceConstellation;
            RequestScene(_gameplayScene);
        }

        private void RequestScene(GameSceneSO scene)
        {
            if (_isNavigating)
            {
                Debug.LogWarning("[GamePhaseStateMachine] Scene request ignored: a load is already in progress.");
                return;
            }

            if (scene == null || _loadSceneRequestChannel == null)
            {
                Debug.LogError("[GamePhaseStateMachine] Cannot navigate: scene asset or load channel is not assigned.");
                return;
            }

            _isNavigating = true;
            Debug.Log($"[GamePhaseStateMachine] Requesting scene '{scene.SceneName}'.");
            _loadSceneRequestChannel.RaiseEvent(scene);
        }

        private void UpdateScoreLabel()
        {
            if (_scoreLabel != null) _scoreLabel.text = _score.ToString("N0");
        }
    }
}
