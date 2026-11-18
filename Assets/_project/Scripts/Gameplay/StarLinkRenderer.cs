namespace Constellation.Gameplay
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Draws the star links between placed chunk outlines as rotated UI segments: cyan for
    /// verse-to-verse connections, and gold with an additive bloom halo and a travelling pulse for
    /// the links between an anchor chunk and its bridge chunk.
    /// </summary>
    public class StarLinkRenderer : MonoBehaviour
    {
        [Tooltip("Parent that receives the generated link segments.")]
        [SerializeField] private RectTransform _linkRoot;

        [Tooltip("Sprite used for ordinary connections.")]
        [SerializeField] private Sprite _cyanLinkSprite;

        [Tooltip("Sprite used for bridge connections.")]
        [SerializeField] private Sprite _goldLinkSprite;

        [Tooltip("Thickness of a generated link segment.")]
        [SerializeField] private float _thickness = 14f;

        [Tooltip("Colour applied to ordinary links.")]
        [SerializeField] private Color _cyanTint = new Color(0.35f, 0.95f, 1f, 1f);

        [Tooltip("Colour applied to bridge links.")]
        [SerializeField] private Color _goldTint = new Color(1f, 0.84f, 0.44f, 1f);

        [Tooltip("Additive material that makes the bridge halo and pulse read as bloom.")]
        [SerializeField] private Material _glowMaterial;

        [Tooltip("How many times thicker than the link the bloom halo is drawn.")]
        [SerializeField] private float _glowThicknessScale = 3.5f;

        [Tooltip("Alpha of the bloom halo.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowAlpha = 0.45f;

        [Tooltip("Pulse length along the link, as a multiple of the line's radius.")]
        [SerializeField] private float _pulseLengthRadiusScale = 5f;

        [Tooltip("Pulse width across the link, as a multiple of the line width.")]
        [SerializeField] private float _pulseWidthScale = 2f;

        [Tooltip("Seconds a pulse takes to travel from the start of its link to the end.")]
        [SerializeField] private float _pulseCycleSeconds = 10f;

        [Tooltip("Random bow given to every anchor-to-bridge link, as a fraction of its length.")]
        [SerializeField] private float _curveAmount = 0.12f;

        [Tooltip("Fewest waves a link may swing through.")]
        [SerializeField] private int _curveMinWaves = 1;

        [Tooltip("Most waves a link may swing through.")]
        [SerializeField] private int _curveMaxWaves = 4;

        [Tooltip("Step by which the bow deepens while a link hunts for a path around a node.")]
        [SerializeField] private float _curveGrowth = 0.6f;

        [Tooltip("Deepest bow a link may take, as a fraction of its length.")]
        [SerializeField] private float _curveMaxBend = 0.32f;

        [Tooltip("How far a link keeps from the nodes it steers around, in board units.")]
        [SerializeField] private float _curveClearance = 16f;

        [Tooltip("Bows tried on each side before a link settles for its first guess.")]
        [SerializeField] private int _curveAttempts = 10;

        [Tooltip("Fewest points a curved link is drawn from; wider waves add more of their own.")]
        [SerializeField] private int _curveSegments = 16;

        private const int GlowSpriteSize = 128;

        private const int MaxCurveSegments = 96;

        private const string PreviewName = "PreviewLink";

        private readonly List<GameObject> _segments = new List<GameObject>();
        private readonly List<Rect> _curveObstacles = new List<Rect>();

        private Sprite _glowSprite;

        private GameObject _preview;

        /// <summary>
        /// Draws a link between two anchored UI positions.
        /// </summary>
        /// <param name="from">Start point in the link root's local space.</param>
        /// <param name="to">End point in the link root's local space.</param>
        /// <param name="isBridge">True to render the gold bridge style.</param>
        public void DrawLink(Vector2 from, Vector2 to, bool isBridge)
        {
            if (_linkRoot == null)
            {
                Debug.LogWarning("[StarLinkRenderer] No link root assigned.");
                return;
            }

            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) return;

            var go = new GameObject("Link", typeof(RectTransform));
            go.transform.SetParent(_linkRoot, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = from;
            rect.sizeDelta = new Vector2(length, _thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = go.AddComponent<Image>();
            image.sprite = isBridge ? _goldLinkSprite : _cyanLinkSprite;
            image.color = isBridge ? _goldTint : _cyanTint;
            image.raycastTarget = false;
            if (image.sprite != null) image.type = Image.Type.Sliced;

            _segments.Add(go);
        }

        /// <summary>
        /// Draws a link between two rects, converting their centres into the link root's local space so
        /// callers can pass chunk outlines wherever they sit in the hierarchy.
        /// </summary>
        /// <param name="from">Rect the link starts at, usually a chunk outline.</param>
        /// <param name="to">Rect the link ends at, usually another chunk outline.</param>
        /// <param name="isBridge">True to render the gold bridge style.</param>
        public void DrawLinkBetween(RectTransform from, RectTransform to, bool isBridge)
        {
            if (_linkRoot == null || from == null || to == null) return;

            DrawLink(
                _linkRoot.InverseTransformPoint(from.TransformPoint(from.rect.center)),
                _linkRoot.InverseTransformPoint(to.TransformPoint(to.rect.center)),
                isBridge);
        }

        /// <summary>
        /// Draws the glowing link between an anchor chunk outline and its bridge chunk outline, bowed
        /// into a gentle random curve that steers around the nodes passed in, with a pulse that runs
        /// from the start point to the end point along the curve and then starts over.
        /// </summary>
        /// <param name="from">Outline the pulse leaves, i.e. the chunk that comes first in the verse.</param>
        /// <param name="to">Outline the pulse reaches, i.e. the chunk that comes next.</param>
        /// <param name="avoid">Node visuals the curve has to keep clear of. The two outlines are skipped.</param>
        public void DrawBridgeLink(RectTransform from, RectTransform to, IList<RectTransform> avoid = null)
        {
            if (_linkRoot == null || from == null || to == null) return;

            Vector2 start = _linkRoot.InverseTransformPoint(from.TransformPoint(from.rect.center));
            Vector2 end = _linkRoot.InverseTransformPoint(to.TransformPoint(to.rect.center));

            CollectCurveObstacles(avoid, from, to);

            GameObject link = DrawCurvedLink(start, end);
            if (link != null) _segments.Add(link);
        }

        /// <summary>
        /// Draws one throwaway link clear across the board, from its left edge to its right edge, so
        /// the line settings can be tuned without starting a game. Invoke it again after changing a
        /// setting: the previous preview is replaced.
        /// </summary>
        [ContextMenu("Preview Link")]
        public void PreviewLink()
        {
            if (_linkRoot == null)
            {
                Debug.LogWarning("[StarLinkRenderer] No link root assigned.");
                return;
            }

            ClearPreview();

            // Nothing is placed outside play mode, so the preview shows the character curve alone.
            _curveObstacles.Clear();

            Rect area = _linkRoot.rect;
            float inset = Mathf.Min(60f, area.width * 0.1f);

            _preview = DrawCurvedLink(
                new Vector2(area.xMin + inset, area.center.y),
                new Vector2(area.xMax - inset, area.center.y));

            if (_preview == null) return;

            _preview.name = PreviewName;
            HideFromScene(_preview);

            // The canvas only rebuilds a dirty graphic when something renders it, so push the new
            // mesh through now: the preview is on screen the moment the menu item is picked.
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Removes the link drawn by Preview Link.
        /// </summary>
        [ContextMenu("Clear Preview")]
        public void ClearPreview()
        {
            if (_preview != null) DestroyPreview(_preview);
            _preview = null;

            // A preview is never serialised, so a script reload drops the reference to one that is
            // still standing in the scene: sweep the link root rather than trust the field alone.
            if (_linkRoot == null) return;

            for (int i = _linkRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _linkRoot.GetChild(i);
                if (child != null && child.name == PreviewName) DestroyPreview(child.gameObject);
            }
        }

        private static void DestroyPreview(GameObject preview)
        {
            if (preview == null) return;

            if (Application.isPlaying) Destroy(preview);
            else DestroyImmediate(preview);
        }

        /// <summary>
        /// Draws one curved bridge link between two points in link root space.
        /// </summary>
        /// <param name="start">Start point in link root space.</param>
        /// <param name="end">End point in link root space.</param>
        /// <returns>The root that holds the ribbons and the pulse, or null when the points coincide.</returns>
        private GameObject DrawCurvedLink(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 1f) return null;

            Vector2 normal = new Vector2(-delta.y, delta.x) / length;
            List<Vector2> points = PickCurve(start, end, normal, length);

            var go = new GameObject("BridgeLink", typeof(RectTransform));
            go.transform.SetParent(_linkRoot, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            // Bloom halo first so it sits behind the crisp core.
            Color halo = _goldTint;
            halo.a = _glowAlpha * _goldTint.a;
            CreateRibbon(rect, "Bloom", points, _thickness * _glowThicknessScale, halo);

            CreateRibbon(rect, "Core", points, _thickness, _goldTint);

            CreatePulse(rect, points);

            return go;
        }

        /// <summary>
        /// Flags an object and its direct children so the scene never serialises them.
        /// </summary>
        /// <param name="root">Object to hide and keep out of the scene file.</param>
        private static void HideFromScene(GameObject root)
        {
            root.hideFlags = HideFlags.HideAndDontSave;

            foreach (Transform child in root.transform)
            {
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        /// <summary>
        /// Removes every drawn link.
        /// </summary>
        public void ClearLinks()
        {
            foreach (GameObject segment in _segments)
            {
                if (segment != null) Destroy(segment);
            }

            _segments.Clear();
            ClearPreview();
        }

        /// <summary>
        /// Bounds of a rect, expressed in the link root's own coordinates.
        /// </summary>
        /// <param name="rect">Rect to measure.</param>
        /// <returns>The rect's world bounds, converted into the link root's space.</returns>
        public Rect BoundsInLinkSpace(RectTransform rect)
        {
            if (_linkRoot == null || rect == null) return new Rect();

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Vector3 min = _linkRoot.InverseTransformPoint(corners[0]);
            Vector3 max = min;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 local = _linkRoot.InverseTransformPoint(corners[i]);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// Collects the node visuals a curve has to steer around, in link root space, once per link.
        /// </summary>
        /// <param name="avoid">Node visuals to keep clear of.</param>
        /// <param name="from">Outline the link starts at.</param>
        /// <param name="to">Outline the link ends at.</param>
        private void CollectCurveObstacles(IList<RectTransform> avoid, RectTransform from, RectTransform to)
        {
            _curveObstacles.Clear();
            if (avoid == null) return;

            Transform fromNode = NodeOf(from);
            Transform toNode = NodeOf(to);

            foreach (RectTransform obstacle in avoid)
            {
                if (obstacle == null || obstacle == _linkRoot) continue;

                // The two nodes the link joins are never obstacles: the line has to reach their outlines.
                if (IsInside(obstacle, fromNode) || IsInside(obstacle, toNode)) continue;

                Rect bounds = BoundsInLinkSpace(obstacle);
                if (bounds.width < 1f || bounds.height < 1f) continue;

                _curveObstacles.Add(bounds);
            }
        }

        /// <summary>Walks up from a chunk outline to the anchor or hub node that owns it.</summary>
        /// <param name="rect">Rect to trace, usually a chunk outline.</param>
        /// <returns>The Anchor_ or Bridge_ node above it, or the rect itself when there is none.</returns>
        private static Transform NodeOf(RectTransform rect)
        {
            if (rect == null) return null;

            Transform node = rect;

            while (node.parent != null && !IsNodeName(node.name))
            {
                node = node.parent;
            }

            return node;
        }

        private static bool IsNodeName(string name)
        {
            return name.StartsWith("Anchor_") || name.StartsWith("Bridge_");
        }

        /// <summary>True when a transform is that node or sits underneath it.</summary>
        private static bool IsInside(Transform candidate, Transform node)
        {
            return node != null && (candidate == node || candidate.IsChildOf(node));
        }

        /// <summary>
        /// Picks the shape a link takes: a random wave count between the configured bounds, then the
        /// wave and bow that keep clear of every node. When no shape is fully clear, the one that
        /// crosses the least is used.
        /// </summary>
        /// <param name="start">Start point in link root space.</param>
        /// <param name="end">End point in link root space.</param>
        /// <param name="normal">Unit vector across the straight line.</param>
        /// <param name="length">Distance between the two points.</param>
        /// <returns>The points the ribbon and the pulse follow.</returns>
        private List<Vector2> PickCurve(Vector2 start, Vector2 end, Vector2 normal, float length)
        {
            int fewest = Mathf.Max(1, _curveMinWaves);
            int most = Mathf.Max(fewest, _curveMaxWaves);
            int waves = UnityEngine.Random.Range(fewest, most + 1);

            // More waves share the same character between them, so a busy line ripples instead of
            // swinging wildly.
            float amplitude = UnityEngine.Random.Range(_curveAmount * 0.35f, _curveAmount) * length / Mathf.Sqrt(waves);

            // A second, slower wave leans the whole link to one side. It stays smaller than the ripples
            // so the waves still read as waves, and the search below deepens it when a node sits square
            // on the straight line.
            float bow = waves > 1 ? UnityEngine.Random.Range(-0.6f, 0.6f) * amplitude : 0f;

            List<Vector2> best = BuildCurve(start, end, normal, waves, amplitude, bow);
            int bestCrossings = CountCrossings(best);
            if (bestCrossings == 0) return best;

            // Deepen either wave, and flip either one, until the path slips past everything in its way.
            float cap = length * _curveMaxBend;

            for (int step = 1; step <= _curveAttempts; step++)
            {
                float scale = 1f + _curveGrowth * step;
                float wider = Mathf.Min(amplitude * scale, cap);
                float swung = Mathf.Clamp(bow * scale, -cap, cap);

                for (int side = 0; side < 4; side++)
                {
                    float waveSign = (side & 1) == 0 ? 1f : -1f;
                    float bowSign = (side & 2) == 0 ? 1f : -1f;

                    List<Vector2> candidate = BuildCurve(start, end, normal, waves, wider * waveSign, swung * bowSign);
                    int crossings = CountCrossings(candidate);

                    if (crossings == 0) return candidate;
                    if (crossings < bestCrossings) { bestCrossings = crossings; best = candidate; }
                }
            }

            return best;
        }

        /// <summary>
        /// Builds a link's path: the straight chord plus two sine waves, one fast and one slow. Both
        /// vanish at the two ends, so the curve still leaves and reaches its chunks exactly.
        /// </summary>
        /// <param name="start">Start point in link root space.</param>
        /// <param name="end">End point in link root space.</param>
        /// <param name="normal">Unit vector across the straight line.</param>
        /// <param name="waves">How many times the path swings across the straight line.</param>
        /// <param name="amplitude">Peak offset of the fast wave from the straight line.</param>
        /// <param name="bow">Peak offset of the slow wave, which leans the whole path to one side.</param>
        /// <returns>Points from start to end.</returns>
        private List<Vector2> BuildCurve(Vector2 start, Vector2 end, Vector2 normal, int waves, float amplitude, float bow)
        {
            // Every wave needs enough samples of its own to stay smooth.
            int segments = Mathf.Clamp(Mathf.Max(_curveSegments, waves * 8), 4, MaxCurveSegments);
            Vector2 chord = end - start;
            var points = new List<Vector2>(segments + 1);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float offset = amplitude * Mathf.Sin(Mathf.PI * waves * t) + bow * Mathf.Sin(Mathf.PI * t);
                points.Add(start + chord * t + normal * offset);
            }

            return points;
        }

        /// <summary>
        /// Counts the points of a path that land on a node the link has to keep clear of.
        /// </summary>
        /// <param name="points">Path to test, in link root space.</param>
        /// <returns>Number of points inside a node, zero when the whole path is clear.</returns>
        private int CountCrossings(List<Vector2> points)
        {
            if (_curveObstacles.Count == 0) return 0;

            int crossings = 0;

            // The two ends sit on the chunks themselves, so only the path between them is judged.
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 point = points[i];

                foreach (Rect bounds in _curveObstacles)
                {
                    if (point.x < bounds.xMin - _curveClearance) continue;
                    if (point.x > bounds.xMax + _curveClearance) continue;
                    if (point.y < bounds.yMin - _curveClearance) continue;
                    if (point.y > bounds.yMax + _curveClearance) continue;

                    crossings++;
                    break;
                }
            }

            return crossings;
        }

        /// <summary>
        /// Adds one soft ribbon that follows the curve of a link.
        /// </summary>
        /// <param name="parent">Link root the ribbon belongs to.</param>
        /// <param name="name">Name given to the ribbon object.</param>
        /// <param name="points">Curve the ribbon follows, in link root space.</param>
        /// <param name="width">Ribbon width across the curve.</param>
        /// <param name="color">Tint applied to the ribbon.</param>
        private void CreateRibbon(RectTransform parent, string name, List<Vector2> points, float width, Color color)
        {
            // The CanvasRenderer is added up front: Unity only honours RequireComponent declared on the
            // concrete type, so a MaskableGraphic subclass gets none and would never reach the canvas.
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var ribbon = go.AddComponent<LinkRibbonGraphic>();
            ribbon.raycastTarget = false;
            ribbon.color = color;
            if (_glowMaterial != null) ribbon.material = _glowMaterial;
            ribbon.SetPath(points, width);
        }

        /// <summary>
        /// Adds the glowing orb that travels along a bridge link, following its curve.
        /// </summary>
        /// <param name="parent">Link root the pulse belongs to.</param>
        /// <param name="points">Curve the pulse walks, in link root space.</param>
        private void CreatePulse(RectTransform parent, List<Vector2> points)
        {
            var go = new GameObject("Pulse", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            // Longer along the line than across it, so the orb reads as travelling in one direction.
            float pulseLength = _thickness * 0.5f * _pulseLengthRadiusScale;
            float pulseWidth = _thickness * _pulseWidthScale;
            rect.sizeDelta = new Vector2(pulseLength, pulseWidth);

            var image = go.AddComponent<Image>();
            image.sprite = GetGlowSprite();
            image.color = _goldTint;
            image.raycastTarget = false;
            if (_glowMaterial != null) image.material = _glowMaterial;

            var pulse = go.AddComponent<LinkPulseView>();
            pulse.Configure(points.ToArray(), _pulseCycleSeconds);
        }

        /// <summary>
        /// Builds the soft-edged orb sprite the pulse is drawn with, once per renderer.
        /// </summary>
        /// <returns>A white radial glow sprite with a transparent rim.</returns>
        private Sprite GetGlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;

            var texture = new Texture2D(GlowSpriteSize, GlowSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "StarLinkGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[GlowSpriteSize * GlowSpriteSize];
            float half = (GlowSpriteSize - 1) * 0.5f;

            for (int y = 0; y < GlowSpriteSize; y++)
            {
                for (int x = 0; x < GlowSpriteSize; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);

                    // Solid middle, soft rim, so the orb reads as a glow instead of a flat disc.
                    float alpha = Mathf.Clamp01((1f - radius) / 0.55f);
                    alpha = alpha * alpha * (3f - 2f * alpha);

                    pixels[y * GlowSpriteSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            _glowSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, GlowSpriteSize, GlowSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f);
            _glowSprite.name = "StarLinkGlow";
            _glowSprite.hideFlags = HideFlags.HideAndDontSave;

            return _glowSprite;
        }
    }
}
