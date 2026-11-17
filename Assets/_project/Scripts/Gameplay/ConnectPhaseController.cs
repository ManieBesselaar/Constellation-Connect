namespace Constellation.Gameplay
{
    using System;
    using System.Collections.Generic;
    using Constellation.Audio;
    using Constellation.Data;
    using Constellation.Localization;
    using Constellation.UI;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Phase 2 of the loop: six anchor nodes and a tray of phrase tiles. Dragging a chunk onto the
    /// anchor whose verse is waiting for it draws a star link; bridge chunks instead feed the gold
    /// junction hubs that connect every verse sharing a keyword.
    /// </summary>
    public class ConnectPhaseController : MonoBehaviour
    {
        /// <summary>Score awarded for a correct phrase placement.</summary>
        public const int CorrectSnapScore = 100;

        /// <summary>Score awarded for completing a bridge junction.</summary>
        public const int BridgeScore = 150;

        /// <summary>Score deducted when the player takes a hint.</summary>
        public const int HintPenalty = 75;

        /// <summary>Gold applied to the whole board on victory.</summary>
        public static readonly Color VictoryGold = new Color(1f, 0.84f, 0.44f, 1f);

        [Header("Roots")]
        [Tooltip("Panel shown while this phase is active.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Rect the anchor nodes and bridge hubs are placed into.")]
        [SerializeField] private RectTransform _boardRoot;

        [Tooltip("Rect the generated star links are parented to. Must share the board's centre.")]
        [SerializeField] private RectTransform _linkRoot;

        [Tooltip("Rect holding the draggable phrase tiles.")]
        [SerializeField] private RectTransform _tileTray;

        [Header("Prefabs")]
        [Tooltip("Anchor node prefab.")]
        [SerializeField] private AnchorNodeView _anchorPrefab;

        [Tooltip("Bridge hub prefab.")]
        [SerializeField] private BridgeHubView _hubPrefab;

        [Tooltip("Draggable phrase tile prefab.")]
        [SerializeField] private PhraseTileView _tilePrefab;

        [Tooltip("Optional stardust burst spawned on a correct snap.")]
        [SerializeField] private GameObject _stardustPrefab;

        [Tooltip("Optional cosmic ripple spawned on a rejected drop.")]
        [SerializeField] private GameObject _ripplePrefab;

        [Header("Art")]
        [Tooltip("Library mapping verse icon ids to medallion sprites.")]
        [SerializeField] private GlyphLibrary _glyphs;

        [Tooltip("Sprite used for ordinary verse links.")]
        [SerializeField] private Sprite _cyanLinkSprite;

        [Tooltip("Sprite used for bridge links.")]
        [SerializeField] private Sprite _goldLinkSprite;

        [Header("Labels")]
        [Tooltip("Running score.")]
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [Tooltip("Verse completion counter.")]
        [SerializeField] private TextMeshProUGUI _progressLabel;

        [Tooltip("Hint service that forwards button presses to this controller.")]
        [SerializeField] private HintService _hintService;

        [Header("Layout")]
        [Tooltip("Horizontal and vertical radius of the anchor ring.")]
        [SerializeField] private Vector2 _anchorRadius = new Vector2(330f, 175f);

        [Tooltip("Radius of the bridge hub ring, measured from the board centre.")]
        [SerializeField] private float _hubRadius = 455f;

        [Tooltip("Vertical offset of the whole constellation, in canvas units.")]
        [SerializeField] private float _boardYOffset = 95f;

        /// <summary>One verse plus the runtime state of its anchor node.</summary>
        private class VerseRuntime
        {
            public VerseDefinition Definition;
            public AnchorNodeView Node;
            public readonly List<PhraseChunk> Queue = new List<PhraseChunk>();
            public int Placed;
            public bool Complete;
        }

        /// <summary>One bridge junction plus the verses it links.</summary>
        private class BridgeRuntime
        {
            public BridgeHubView Hub;
            public string Tag;
            public readonly List<VerseRuntime> Participants = new List<VerseRuntime>();
            public bool Complete;
        }

        private readonly List<VerseRuntime> _verses = new List<VerseRuntime>();
        private readonly List<BridgeRuntime> _bridges = new List<BridgeRuntime>();
        private readonly List<PhraseTileView> _tiles = new List<PhraseTileView>();
        private readonly List<AnchorNodeView> _completionOrder = new List<AnchorNodeView>();

        private ConstellationSO _constellation;
        private StarLinkRenderer _linkRenderer;
        private int _score;
        private int _versesComplete;
        private int _hintsUsed;
        private bool _isRunning;

        /// <summary>Raised when the sixth verse is completed, carrying the final score.</summary>
        public event Action<int> ConstellationCompleted;

        /// <summary>Raised whenever the score changes.</summary>
        public event Action<int> ScoreChanged;

        /// <summary>Current score for this run.</summary>
        public int Score => _score;

        /// <summary>Number of hints taken this run.</summary>
        public int HintsUsed => _hintsUsed;

        /// <summary>Number of completed verses.</summary>
        public int VersesComplete => _versesComplete;

        /// <summary>Total number of verses in the active constellation.</summary>
        public int VerseCount => _verses.Count;

        private void Awake()
        {
            _linkRenderer = GetComponent<StarLinkRenderer>();

            if (_linkRenderer == null)
            {
                _linkRenderer = gameObject.AddComponent<StarLinkRenderer>();
            }

            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>
        /// Builds the board for a constellation and starts accepting drops.
        /// </summary>
        /// <param name="constellation">Theme being played.</param>
        /// <param name="startingScore">Score carried over from the study phase.</param>
        public void Begin(ConstellationSO constellation, int startingScore)
        {
            if (constellation == null)
            {
                Debug.LogError("[ConnectPhaseController] Cannot begin: no constellation supplied.");
                return;
            }

            ClearBoard();

            _constellation = constellation;
            _score = startingScore;
            _versesComplete = 0;
            _hintsUsed = 0;
            _isRunning = true;

            if (_panel != null) _panel.SetActive(true);

            BuildVerses(constellation);
            BuildBridges(constellation);
            BuildTiles(constellation);

            _linkRenderer.ClearLinks();
            RefreshLabels();

            Debug.Log($"[ConnectPhaseController] Board built for '{constellation.constellationId}': " +
                      $"{_verses.Count} verses, {_bridges.Count} bridge hubs, {_tiles.Count} tiles.");
        }

        /// <summary>
        /// Stops accepting drops. Called when the phase ends.
        /// </summary>
        public void StopAccepting()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Turns every node and hub gold for the victory beat.
        /// </summary>
        public void ApplyVictoryTint()
        {
            foreach (VerseRuntime verse in _verses)
            {
                if (verse.Node != null) verse.Node.SetFillColor(VictoryGold);
            }

            foreach (BridgeRuntime bridge in _bridges)
            {
                if (bridge.Hub != null) bridge.Hub.SetFillColor(VictoryGold);
            }
        }

        /// <summary>
        /// Highlights the tile and node the player should place next, at the cost of a score penalty.
        /// </summary>
        public void RequestHint()
        {
            if (!_isRunning)
            {
                Debug.LogWarning("[ConnectPhaseController] Hint ignored: the connect phase is not running.");
                return;
            }

            VerseRuntime target = null;
            foreach (VerseRuntime verse in _verses)
            {
                if (!verse.Complete)
                {
                    target = verse;
                    break;
                }
            }

            if (target == null) return;

            PhraseChunk expected = target.Queue[target.Placed];
            _hintsUsed++;
            _score -= HintPenalty;
            ScoreChanged?.Invoke(_score);
            RefreshLabels();

            target.Node.Pulse();
            AudioManager.PlayHintPulse();

            foreach (PhraseTileView tile in _tiles)
            {
                if (tile == null || tile.Chunk == null || tile.Chunk.chunkId != expected.chunkId) continue;

                var tween = tile.GetComponent<UIImageTween>();
                if (tween != null) tween.Punch(0.18f, 0.6f);
                break;
            }

            Debug.Log($"[ConnectPhaseController] Hint {_hintsUsed}: next chunk is '{expected.chunkId}' " +
                      $"for verse '{target.Definition.verseId}' (score {_score}).");
        }

        // ------------------------------------------------------------------ build

        private void BuildVerses(ConstellationSO constellation)
        {
            int index = 0;
            int verseCount = constellation.verses.Count;

            foreach (VerseDefinition definition in constellation.verses)
            {
                var runtime = new VerseRuntime { Definition = definition };

                foreach (PhraseChunk chunk in definition.chunks)
                {
                    if (chunk == null || chunk.isBridge) continue;
                    runtime.Queue.Add(chunk);
                }

                runtime.Queue.Sort((a, b) => a.order.CompareTo(b.order));

                if (runtime.Queue.Count == 0)
                {
                    Debug.LogWarning($"[ConnectPhaseController] Verse '{definition.verseId}' has no non-bridge chunks and was skipped.");
                    continue;
                }

                AnchorNodeView node = Instantiate(_anchorPrefab, _boardRoot);
                node.name = "Anchor_" + definition.verseId;

                var rect = (RectTransform)node.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = AnchorPosition(index, verseCount);

                Sprite glyph = _glyphs != null ? _glyphs.Get(definition.iconId) : null;
                string reference = LocalizedText.Get(definition.verseId + "_REF", definition.reference);
                node.Bind(definition.verseId, reference, runtime.Queue[0], glyph);
                node.Filled += HandleAnchorFilled;

                runtime.Node = node;
                _verses.Add(runtime);
                index++;
            }
        }

        private void BuildBridges(ConstellationSO constellation)
        {
            var byTag = new Dictionary<string, BridgeRuntime>();

            foreach (VerseRuntime verse in _verses)
            {
                foreach (PhraseChunk chunk in verse.Definition.chunks)
                {
                    if (chunk == null || !chunk.isBridge || string.IsNullOrEmpty(chunk.bridgeTag)) continue;

                    BridgeRuntime bridge;
                    if (!byTag.TryGetValue(chunk.bridgeTag, out bridge))
                    {
                        BridgeHubView hub = Instantiate(_hubPrefab, _boardRoot);
                        hub.name = "Bridge_" + chunk.bridgeTag;

                        var rect = (RectTransform)hub.transform;
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = HubPosition(byTag.Count);

                        hub.Bind(chunk.chunkId, chunk);
                        hub.Completed += HandleBridgeCompleted;

                        bridge = new BridgeRuntime { Hub = hub, Tag = chunk.bridgeTag };
                        byTag.Add(chunk.bridgeTag, bridge);
                        _bridges.Add(bridge);
                    }

                    bridge.Participants.Add(verse);
                }
            }
        }

        private void BuildTiles(ConstellationSO constellation)
        {
            var shuffled = new List<PhraseChunk>();

            foreach (VerseDefinition definition in constellation.verses)
            {
                foreach (PhraseChunk chunk in definition.chunks)
                {
                    if (chunk != null) shuffled.Add(chunk);
                }
            }

            // Deterministic-looking shuffle so every run of a theme differs but stays reproducible per play.
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                PhraseChunk temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            foreach (PhraseChunk chunk in shuffled)
            {
                PhraseTileView tile = Instantiate(_tilePrefab, _tileTray);
                tile.name = "Tile_" + chunk.chunkId;

                string text = LocalizedText.Get(chunk.localizationKey, chunk.text);
                tile.Bind(chunk);
                tile.SetLabel(text);

                tile.Dropped += HandleTileDropped;
                _tiles.Add(tile);
            }
        }

        private Vector2 AnchorPosition(int index, int total)
        {
            float angle = Mathf.PI * 0.5f + (Mathf.PI * 2f * index / Mathf.Max(1, total));
            return new Vector2(
                Mathf.Cos(angle) * _anchorRadius.x,
                Mathf.Sin(angle) * _anchorRadius.y + _boardYOffset);
        }

        private Vector2 HubPosition(int index)
        {
            // Hubs sit between anchors, so offset by half a slot.
            float angle = Mathf.PI * 0.5f + Mathf.PI / 6f + (Mathf.PI * 2f * index / 3f);
            return new Vector2(Mathf.Cos(angle) * _hubRadius, Mathf.Sin(angle) * _hubRadius * 0.55f + _boardYOffset);
        }

        // ------------------------------------------------------------------ interaction

        private void HandleTileDropped(PhraseTileView tile, IDropTarget target)
        {
            if (!_isRunning || tile == null) return;

            if (target == null)
            {
                // Dropped on empty space: the tile springs back with no penalty.
                return;
            }

            if (target.IsFilled) return;

            if (!target.TryAccept(tile.Chunk))
            {
                SpawnRipple(tile);
                AudioManager.PlayTileRippleWrong();
                Debug.Log($"[ConnectPhaseController] Rejected '{tile.Chunk.chunkId}' on '{target.VerseId}'.");
                return;
            }

            // Accepted: the target raised its own event, which already scored and advanced the board.
            _tiles.Remove(tile);
            Destroy(tile.gameObject);
        }

        private void HandleAnchorFilled(AnchorNodeView node)
        {
            VerseRuntime verse = FindVerse(node);
            if (verse == null) return;

            verse.Placed++;
            AddScore(CorrectSnapScore);
            SpawnStardust(node);
            AudioManager.PlayTileSnapCorrect();

            if (verse.Placed < verse.Queue.Count)
            {
                // More chunks to place: rebinding resets IsFilled so the node accepts the next one.
                PhraseChunk next = verse.Queue[verse.Placed];
                string reference = LocalizedText.Get(verse.Definition.verseId + "_REF", verse.Definition.reference);
                Sprite glyph = _glyphs != null ? _glyphs.Get(verse.Definition.iconId) : null;
                node.Bind(verse.Definition.verseId, reference, next, glyph);

                Debug.Log($"[ConnectPhaseController] Verse '{verse.Definition.verseId}' awaiting chunk {verse.Placed + 1}/{verse.Queue.Count}.");
                return;
            }

            verse.Complete = true;
            _versesComplete++;
            _completionOrder.Add(node);
            AudioManager.PlayVerseComplete();

            if (_completionOrder.Count > 1)
            {
                RectTransform previous = (RectTransform)_completionOrder[_completionOrder.Count - 2].transform;
                _linkRenderer.DrawLink(
                    ((RectTransform)node.transform).anchoredPosition,
                    previous.anchoredPosition,
                    false);
            }

            RefreshLabels();
            Debug.Log($"[ConnectPhaseController] Verse complete: {verse.Definition.verseId} " +
                      $"({_versesComplete}/{_verses.Count}).");

            if (_versesComplete >= _verses.Count)
            {
                CompleteConstellation();
            }
        }

        private void HandleBridgeCompleted(BridgeHubView hub)
        {
            BridgeRuntime bridge = null;
            foreach (BridgeRuntime candidate in _bridges)
            {
                if (candidate.Hub == hub)
                {
                    bridge = candidate;
                    break;
                }
            }

            if (bridge == null) return;

            bridge.Complete = true;
            AddScore(BridgeScore);
            AudioManager.PlayTileSnapCorrect();

            var hubRect = (RectTransform)hub.transform;

            foreach (VerseRuntime verse in bridge.Participants)
            {
                if (verse.Node == null) continue;

                _linkRenderer.DrawLink(
                    hubRect.anchoredPosition,
                    ((RectTransform)verse.Node.transform).anchoredPosition,
                    true);
            }

            Debug.Log($"[ConnectPhaseController] Bridge '{bridge.Tag}' completed, linking {bridge.Participants.Count} verses.");
        }

        private void CompleteConstellation()
        {
            _isRunning = false;

            if (_hintService != null) _hintService.SetInteractable(false);

            Debug.Log($"[ConnectPhaseController] Constellation '{_constellation.constellationId}' completed with {_score} points.");
            ConstellationCompleted?.Invoke(_score);
        }

        // ------------------------------------------------------------------ helpers

        private VerseRuntime FindVerse(AnchorNodeView node)
        {
            foreach (VerseRuntime verse in _verses)
            {
                if (verse.Node == node) return verse;
            }

            return null;
        }

        private void AddScore(int amount)
        {
            _score += amount;
            ScoreChanged?.Invoke(_score);
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_scoreLabel != null) _scoreLabel.text = _score.ToString("N0");
            if (_progressLabel != null) _progressLabel.text = $"{_versesComplete} / {_verses.Count}";
        }

        private void SpawnStardust(Component at)
        {
            if (_stardustPrefab == null || at == null) return;

            GameObject burst = Instantiate(_stardustPrefab, at.transform.position, Quaternion.identity, _boardRoot);
            Destroy(burst, 2.5f);
        }

        private void SpawnRipple(PhraseTileView tile)
        {
            if (_ripplePrefab == null || tile == null) return;

            GameObject ripple = Instantiate(_ripplePrefab, tile.transform.position, Quaternion.identity, _boardRoot);
            Destroy(ripple, 1.5f);
        }

        private void ClearBoard()
        {
            foreach (VerseRuntime verse in _verses)
            {
                if (verse.Node == null) continue;
                verse.Node.Filled -= HandleAnchorFilled;
                Destroy(verse.Node.gameObject);
            }

            foreach (BridgeRuntime bridge in _bridges)
            {
                if (bridge.Hub == null) continue;
                bridge.Hub.Completed -= HandleBridgeCompleted;
                Destroy(bridge.Hub.gameObject);
            }

            foreach (PhraseTileView tile in _tiles)
            {
                if (tile == null) continue;
                tile.Dropped -= HandleTileDropped;
                Destroy(tile.gameObject);
            }

            _verses.Clear();
            _bridges.Clear();
            _tiles.Clear();
            _completionOrder.Clear();

            if (_linkRenderer != null) _linkRenderer.ClearLinks();
            if (_hintService != null) _hintService.SetInteractable(true);
        }
    }
}
