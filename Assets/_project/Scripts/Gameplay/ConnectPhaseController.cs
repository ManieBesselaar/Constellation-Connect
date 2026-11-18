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
    /// Phase 2 of the loop: six anchor nodes, their bridge hubs and a tray of phrase tiles. Every
    /// node shows one empty hex outline per chunk it owes, in order; dropping a chunk on its outline
    /// fills it and draws a star link, while bridge chunks feed the keyword hubs.
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

        [Tooltip("Empty target outline placed under each anchor and hub.")]
        [SerializeField] private ChunkSlotView _slotPrefab;

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
        [Tooltip("Horizontal distance between anchor columns.")]
        [SerializeField] private float _anchorSpacing = 480f;

        [Tooltip("Vertical offset of the first anchor band, in canvas units.")]
        [SerializeField] private float _anchorTopY = 280f;

        [Tooltip("Vertical distance between anchor bands.")]
        [SerializeField] private float _anchorBandGap = 270f;

        [Tooltip("Most verse anchors placed in one row. Three verses play as a single band, six split into two.")]
        [SerializeField] private int _anchorsPerBand = 3;

        [Tooltip("Vertical offset of the bridge band, in canvas units.")]
        [SerializeField] private float _hubYOffset = -232f;

        [Tooltip("Horizontal distance between bridge hubs.")]
        [SerializeField] private float _hubSpacing = 480f;

        [Tooltip("Size of one empty target slot, matching the hex tile's shape.")]
        [SerializeField] private Vector2 _slotSize = new Vector2(104f, 60f);

        [Tooltip("Horizontal gap between target slots.")]
        [SerializeField] private float _slotGap = 6f;

        [Tooltip("Vertical offset of an anchor's slot row, measured from the medallion centre.")]
        [SerializeField] private float _anchorSlotY = -145f;

        [Tooltip("Vertical offset of a hub's slot row, measured from the gem centre.")]
        [SerializeField] private float _hubSlotY = -93f;

        /// <summary>One verse plus the runtime state of its anchor node.</summary>
        private class VerseRuntime
        {
            public VerseDefinition Definition;
            public AnchorNodeView Node;
            public readonly List<PhraseChunk> Queue = new List<PhraseChunk>();
            public readonly List<PhraseChunk> PendingBridges = new List<PhraseChunk>();
            public readonly List<ChunkSlotView> Slots = new List<ChunkSlotView>();
            public int AnchorsFilled;
            public bool AnchorDone;
            public bool Complete;
        }

        /// <summary>One bridge junction plus the verses it links.</summary>
        private class BridgeRuntime
        {
            public BridgeHubView Hub;
            public string Tag;
            public readonly List<PhraseChunk> Chunks = new List<PhraseChunk>();
            public readonly List<ChunkSlotView> Slots = new List<ChunkSlotView>();
            public readonly List<VerseRuntime> Participants = new List<VerseRuntime>();
            public int Filled;
            public bool Complete;
        }

        private readonly List<VerseRuntime> _verses = new List<VerseRuntime>();
        private readonly List<BridgeRuntime> _bridges = new List<BridgeRuntime>();
        private readonly List<PhraseTileView> _tiles = new List<PhraseTileView>();
        private readonly List<AnchorNodeView> _completionOrder = new List<AnchorNodeView>();
        private readonly Dictionary<ChunkSlotView, VerseRuntime> _verseSlots = new Dictionary<ChunkSlotView, VerseRuntime>();
        private readonly Dictionary<ChunkSlotView, BridgeRuntime> _bridgeSlots = new Dictionary<ChunkSlotView, BridgeRuntime>();

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
            BuildBridges();
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

                foreach (ChunkSlotView slot in verse.Slots)
                {
                    if (slot != null) slot.SetVictory(VictoryGold);
                }
            }

            foreach (BridgeRuntime bridge in _bridges)
            {
                if (bridge.Hub != null) bridge.Hub.SetFillColor(VictoryGold);

                foreach (ChunkSlotView slot in bridge.Slots)
                {
                    if (slot != null) slot.SetVictory(VictoryGold);
                }
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

            // The next chunk is the verse's first empty outline, or its first empty hub outline once
            // every phrase chunk has been placed.
            ChunkSlotView slot = null;

            foreach (ChunkSlotView candidate in target.Slots)
            {
                if (candidate != null && !candidate.IsFilled)
                {
                    slot = candidate;
                    break;
                }
            }

            if (slot == null && target.PendingBridges.Count > 0)
            {
                slot = FindSlotForChunk(target.PendingBridges[0]);
            }

            if (slot == null || slot.ExpectedChunk == null) return;

            PhraseChunk expected = slot.ExpectedChunk;

            _hintsUsed++;
            _score -= HintPenalty;
            ScoreChanged?.Invoke(_score);
            RefreshLabels();

            slot.Pulse();
            AudioManager.PlayHintPulse();

            foreach (PhraseTileView tile in _tiles)
            {
                if (tile == null || tile.Chunk == null || tile.Chunk.chunkId != expected.chunkId) continue;

                var tween = tile.GetComponent<UIImageTween>();
                if (tween != null) tween.Punch(0.18f, 0.6f);
                break;
            }

            string where = expected.isBridge ? $" at bridge '{expected.bridgeTag}'" : string.Empty;
            Debug.Log($"[ConnectPhaseController] Hint {_hintsUsed}: next chunk is '{expected.chunkId}' " +
                      $"for verse '{target.Definition.verseId}'{where} (score {_score}).");
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
                    if (chunk == null) continue;

                    // Bridge chunks belong to their keyword hub, so they never enter the anchor's
                    // queue; they are tracked separately because the verse is not connected until
                    // every one of them has been placed.
                    if (chunk.isBridge) runtime.PendingBridges.Add(chunk);
                    else runtime.Queue.Add(chunk);
                }

                runtime.Queue.Sort((a, b) => a.order.CompareTo(b.order));
                runtime.PendingBridges.Sort((a, b) => a.order.CompareTo(b.order));

                if (runtime.Queue.Count == 0 && runtime.PendingBridges.Count == 0)
                {
                    Debug.LogWarning($"[ConnectPhaseController] Verse '{definition.verseId}' has no chunks and was skipped.");
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

                node.Bind(definition.verseId, reference, glyph);

                runtime.Node = node;
                runtime.AnchorDone = runtime.Queue.Count == 0;

                // One empty outline per phrase chunk, in verse order, so the player can see how many
                // pieces the verse needs and which one is still missing.
                runtime.Slots.AddRange(CreateSlotRow(node.transform, definition.verseId, runtime.Queue, _anchorSlotY));

                foreach (ChunkSlotView slot in runtime.Slots)
                {
                    _verseSlots[slot] = runtime;
                }

                if (runtime.AnchorDone)
                {
                    Debug.Log($"[ConnectPhaseController] Verse '{definition.verseId}' has only bridge " +
                              "chunks; it completes through its hubs.");
                }

                _verses.Add(runtime);
                index++;
            }
        }

        private void BuildBridges()
        {
            // Collect the keywords first so the hubs can be spread evenly across the bridge band.
            var tags = new List<string>();

            foreach (VerseRuntime verse in _verses)
            {
                foreach (PhraseChunk chunk in verse.Definition.chunks)
                {
                    if (chunk == null || !chunk.isBridge || string.IsNullOrEmpty(chunk.bridgeTag)) continue;
                    if (!tags.Contains(chunk.bridgeTag)) tags.Add(chunk.bridgeTag);
                }
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];

                BridgeHubView hub = Instantiate(_hubPrefab, _boardRoot);
                hub.name = "Bridge_" + tag;

                var rect = (RectTransform)hub.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = HubPosition(i, tags.Count, AnchorBandCount(_verses.Count));

                hub.Bind(tag);

                var bridge = new BridgeRuntime { Hub = hub, Tag = tag };

                // Walking the verses in order means a hub's slots run from the earliest verse's
                // bridge chunk to the latest, which is the order the player reads them in.
                foreach (VerseRuntime verse in _verses)
                {
                    foreach (PhraseChunk chunk in verse.Definition.chunks)
                    {
                        if (chunk == null || !chunk.isBridge || chunk.bridgeTag != tag) continue;

                        bridge.Chunks.Add(chunk);
                        if (!bridge.Participants.Contains(verse)) bridge.Participants.Add(verse);
                    }
                }

                bridge.Slots.AddRange(CreateSlotRow(hub.transform, tag, bridge.Chunks, _hubSlotY));

                foreach (ChunkSlotView slot in bridge.Slots)
                {
                    _bridgeSlots[slot] = bridge;
                }

                _bridges.Add(bridge);
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

                // Bridge chunks go to the keyword hub rather than the verse anchor, so the tile
                // names the hub it belongs to, in deep red.
                if (chunk.isBridge && !string.IsNullOrEmpty(chunk.bridgeTag))
                {
                    text += "\n<size=70%><color=#8B0000>[ " + chunk.bridgeTag + " ]</color></size>";
                }

                tile.Bind(chunk);
                tile.SetLabel(text);

                tile.Dropped += HandleTileDropped;
                _tiles.Add(tile);
            }
        }

        private Vector2 AnchorPosition(int index, int total)
        {
            // The anchors fill as many bands as the run needs rather than a ring: a node needs room
            // underneath for its row of chunk outlines, and a ring puts two of them a medallion apart.
            int perBand = AnchorsPerBand(total);
            int band = index / perBand;
            int column = index % perBand;
            int inBand = Mathf.Min(perBand, total - band * perBand);

            return new Vector2(
                (column - (inBand - 1) * 0.5f) * _anchorSpacing,
                _anchorTopY - band * _anchorBandGap);
        }

        private int AnchorsPerBand(int verseCount)
        {
            return Mathf.Clamp(_anchorsPerBand, 1, Mathf.Max(1, verseCount));
        }

        private int AnchorBandCount(int verseCount)
        {
            return Mathf.Max(1, Mathf.CeilToInt(verseCount / (float)AnchorsPerBand(verseCount)));
        }

        private Vector2 HubPosition(int index, int total, int bandCount)
        {
            // The hubs share one band beneath the anchors, so a hub's slot row never crosses a
            // medallion on its way outwards. The band hangs the same drop below the last anchor band
            // as the six-verse board was tuned on, so a shorter run loses the empty band instead of
            // leaving a hole in the middle of the constellation.
            float bandDrop = (_anchorTopY - _anchorBandGap) - _hubYOffset;
            float lastBandY = _anchorTopY - (bandCount - 1) * _anchorBandGap;

            float x = total <= 1 ? 0f : (index - (total - 1) * 0.5f) * _hubSpacing;
            return new Vector2(x, lastBandY - bandDrop);
        }

        /// <summary>
        /// Builds one empty hex outline per chunk under a node, reading left to right.
        /// </summary>
        /// <param name="parent">Node the slots belong to.</param>
        /// <param name="ownerId">Verse id, or bridge keyword for hub rows.</param>
        /// <param name="chunks">Chunks the node owes, already ordered.</param>
        /// <param name="yOffset">Vertical offset of the row from the node's centre.</param>
        /// <returns>The slots that were created.</returns>
        private List<ChunkSlotView> CreateSlotRow(Transform parent, string ownerId, List<PhraseChunk> chunks, float yOffset)
        {
            var slots = new List<ChunkSlotView>();

            if (_slotPrefab == null || chunks == null || chunks.Count == 0) return slots;

            float step = _slotSize.x + _slotGap;

            for (int i = 0; i < chunks.Count; i++)
            {
                ChunkSlotView slot = Instantiate(_slotPrefab, parent);
                slot.name = "Slot_" + chunks[i].chunkId;

                var rect = (RectTransform)slot.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = _slotSize;
                rect.anchoredPosition = new Vector2((i - (chunks.Count - 1) * 0.5f) * step, yOffset);

                slot.Bind(ownerId, chunks[i], i);
                slot.Filled += HandleSlotFilled;

                slots.Add(slot);
            }

            return slots;
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

            if (target.IsFilled && !target.AcceptsRepeats) return;

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

        private void HandleSlotFilled(ChunkSlotView slot)
        {
            if (!_isRunning || slot == null) return;

            SpawnStardust(slot);
            AudioManager.PlayTileSnapCorrect();

            VerseRuntime verse;
            if (_verseSlots.TryGetValue(slot, out verse))
            {
                HandleVerseSlot(verse);
                return;
            }

            BridgeRuntime bridge;
            if (_bridgeSlots.TryGetValue(slot, out bridge))
            {
                HandleBridgeSlot(slot, bridge);
            }
        }

        private void HandleVerseSlot(VerseRuntime verse)
        {
            AddScore(CorrectSnapScore);

            verse.AnchorsFilled++;

            if (verse.AnchorsFilled < verse.Queue.Count)
            {
                Debug.Log($"[ConnectPhaseController] Verse '{verse.Definition.verseId}' awaiting chunk " +
                          $"{verse.AnchorsFilled + 1}/{verse.Queue.Count}.");
                return;
            }

            verse.AnchorDone = true;
            if (verse.Node != null) verse.Node.SetLit(true);

            if (verse.PendingBridges.Count > 0)
            {
                // Every phrase chunk is placed, but the verse's bridge chunks still belong on their
                // keyword hubs before the verse counts as connected.
                Debug.Log($"[ConnectPhaseController] Verse '{verse.Definition.verseId}' still needs " +
                          $"{verse.PendingBridges.Count} bridge chunk(s).");
                if (verse.Node != null) verse.Node.Pulse();
                return;
            }

            CompleteVerse(verse);
        }

        private void CompleteVerse(VerseRuntime verse)
        {
            if (verse.Complete || !verse.AnchorDone || verse.PendingBridges.Count > 0) return;

            verse.Complete = true;
            _versesComplete++;

            AnchorNodeView node = verse.Node;
            if (node != null)
            {
                _completionOrder.Add(node);

                if (_completionOrder.Count > 1)
                {
                    // Draw between the medallions themselves: the board root and the link root do not
                    // share an origin, so board-local positions would land the segment off the nodes.
                    RectTransform previous = (RectTransform)_completionOrder[_completionOrder.Count - 2].transform;
                    _linkRenderer.DrawLinkBetween((RectTransform)node.transform, previous, false);
                }
            }

            AudioManager.PlayVerseComplete();
            RefreshLabels();
            Debug.Log($"[ConnectPhaseController] Verse complete: {verse.Definition.verseId} " +
                      $"({_versesComplete}/{_verses.Count}).");

            if (_versesComplete >= _verses.Count)
            {
                CompleteConstellation();
            }
        }

        private void HandleBridgeSlot(ChunkSlotView slot, BridgeRuntime bridge)
        {
            bridge.Filled++;

            if (!bridge.Complete)
            {
                bridge.Complete = true;
                AddScore(BridgeScore);

                if (bridge.Hub != null)
                {
                    bridge.Hub.SetCompleteVisual();
                }

                Debug.Log($"[ConnectPhaseController] Bridge '{bridge.Tag}' completed, linking {bridge.Participants.Count} verses.");
            }
            else
            {
                // The hub keeps its remaining slots open for the other verses sharing the keyword.
                AddScore(CorrectSnapScore);
                Debug.Log($"[ConnectPhaseController] Bridge chunk '{slot.ExpectedChunk.chunkId}' placed on '{bridge.Tag}'.");
            }

            VerseRuntime owner = FindVerseByBridgeChunk(slot.ExpectedChunk);
            if (owner == null) return;

            // A bridge chunk joins the verse at the chunk outline beside it, not at the hub medallion.
            LinkBridgeToVerse(owner, slot);

            owner.PendingBridges.RemoveAll(pending => pending != null && pending.chunkId == slot.ExpectedChunk.chunkId);

            if (owner.PendingBridges.Count == 0 && owner.AnchorDone)
            {
                CompleteVerse(owner);
            }
        }

        private void CompleteConstellation()
        {
            _isRunning = false;

            if (_hintService != null) _hintService.SetInteractable(false);

            Debug.Log($"[ConnectPhaseController] Constellation '{_constellation.constellationId}' completed with {_score} points.");
            ConstellationCompleted?.Invoke(_score);
        }

        // ------------------------------------------------------------------ helpers

        private ChunkSlotView FindSlotForChunk(PhraseChunk chunk)
        {
            if (chunk == null) return null;

            foreach (KeyValuePair<ChunkSlotView, BridgeRuntime> pair in _bridgeSlots)
            {
                ChunkSlotView slot = pair.Key;
                if (slot == null || slot.IsFilled || slot.ExpectedChunk == null) continue;
                if (slot.ExpectedChunk.chunkId == chunk.chunkId) return slot;
            }

            return null;
        }

        private VerseRuntime FindVerseByBridgeChunk(PhraseChunk chunk)
        {
            if (chunk == null) return null;

            foreach (VerseRuntime verse in _verses)
            {
                foreach (PhraseChunk pending in verse.PendingBridges)
                {
                    if (pending != null && pending.chunkId == chunk.chunkId) return verse;
                }
            }

            return null;
        }

        /// <summary>
        /// Draws the gold star link for a placed bridge chunk: from the anchor outline of the verse
        /// chunk beside it to the hub outline that took the bridge chunk.
        /// </summary>
        /// <param name="verse">Verse the bridge chunk belongs to.</param>
        /// <param name="bridgeSlot">Hub outline the bridge chunk was placed in.</param>
        private void LinkBridgeToVerse(VerseRuntime verse, ChunkSlotView bridgeSlot)
        {
            if (_linkRenderer == null || verse == null || bridgeSlot == null) return;

            ChunkSlotView anchorSlot = FindNeighbourAnchorSlot(verse, bridgeSlot.ExpectedChunk);
            if (anchorSlot == null) return;

            _linkRenderer.DrawLinkBetween(
                (RectTransform)anchorSlot.transform,
                (RectTransform)bridgeSlot.transform,
                true);
        }

        /// <summary>
        /// Finds the verse's anchor outline that sits next to a bridge chunk: the nearest phrase chunk
        /// before it in the verse, or the nearest one after it when the bridge chunk opens the verse.
        /// </summary>
        /// <param name="verse">Verse the bridge chunk belongs to.</param>
        /// <param name="bridgeChunk">Bridge chunk that was placed.</param>
        /// <returns>The neighbouring anchor outline, or null when the verse has no anchor slots.</returns>
        private ChunkSlotView FindNeighbourAnchorSlot(VerseRuntime verse, PhraseChunk bridgeChunk)
        {
            if (verse == null || verse.Definition == null || bridgeChunk == null) return null;

            List<PhraseChunk> chunks = verse.Definition.chunks;
            if (chunks == null || chunks.Count == 0) return null;

            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i] == null || chunks[i].chunkId != bridgeChunk.chunkId) continue;

                // Step outwards from the bridge chunk, trying the chunk that precedes it first.
                for (int step = 1; step < chunks.Count; step++)
                {
                    int before = i - step;
                    if (before >= 0)
                    {
                        ChunkSlotView slot = FindAnchorSlotFor(verse, chunks[before]);
                        if (slot != null) return slot;
                    }

                    int after = i + step;
                    if (after < chunks.Count)
                    {
                        ChunkSlotView slot = FindAnchorSlotFor(verse, chunks[after]);
                        if (slot != null) return slot;
                    }
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Returns the verse's anchor outline bound to a chunk, or null when the chunk has none there
        /// (bridge chunks sit on a hub, not on the verse).
        /// </summary>
        /// <param name="verse">Verse whose outlines are searched.</param>
        /// <param name="chunk">Chunk the outline must expect.</param>
        private ChunkSlotView FindAnchorSlotFor(VerseRuntime verse, PhraseChunk chunk)
        {
            if (verse == null || chunk == null) return null;

            foreach (ChunkSlotView slot in verse.Slots)
            {
                if (slot == null || slot.ExpectedChunk == null) continue;
                if (slot.ExpectedChunk.chunkId == chunk.chunkId) return slot;
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
                foreach (ChunkSlotView slot in verse.Slots)
                {
                    if (slot == null) continue;
                    slot.Filled -= HandleSlotFilled;
                }

                if (verse.Node == null) continue;

                // The slot row is parented to the node, so destroying the node clears it too.
                Destroy(verse.Node.gameObject);
            }

            foreach (BridgeRuntime bridge in _bridges)
            {
                foreach (ChunkSlotView slot in bridge.Slots)
                {
                    if (slot == null) continue;
                    slot.Filled -= HandleSlotFilled;
                }

                if (bridge.Hub == null) continue;
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
            _verseSlots.Clear();
            _bridgeSlots.Clear();

            if (_linkRenderer != null) _linkRenderer.ClearLinks();
            if (_hintService != null) _hintService.SetInteractable(true);
        }
    }
}
