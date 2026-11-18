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

        private const int GlowSpriteSize = 128;

        private readonly List<GameObject> _segments = new List<GameObject>();

        private Sprite _glowSprite;

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
        /// Draws the glowing link between an anchor chunk outline and its bridge chunk outline, with a
        /// pulse that runs from the start point to the end point and then starts over.
        /// </summary>
        /// <param name="from">Outline the pulse leaves, i.e. the chunk that comes first in the verse.</param>
        /// <param name="to">Outline the pulse reaches, i.e. the chunk that comes next.</param>
        public void DrawBridgeLink(RectTransform from, RectTransform to)
        {
            if (_linkRoot == null || from == null || to == null) return;

            Vector2 start = _linkRoot.InverseTransformPoint(from.TransformPoint(from.rect.center));
            Vector2 end = _linkRoot.InverseTransformPoint(to.TransformPoint(to.rect.center));

            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < 1f) return;

            var go = new GameObject("BridgeLink", typeof(RectTransform));
            go.transform.SetParent(_linkRoot, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(length, _thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            // Bloom halo first so it sits behind the crisp core.
            Color halo = _goldTint;
            halo.a = _glowAlpha * _goldTint.a;
            CreateBar(rect, "Bloom", length, _thickness * _glowThicknessScale, halo);

            CreateBar(rect, "Core", length, _thickness, _goldTint);

            CreatePulse(rect, length);

            _segments.Add(go);
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
        }

        /// <summary>
        /// Adds one horizontal emissive bar of a link, starting at the link's start point.
        /// </summary>
        /// <param name="parent">Link rect the bar belongs to.</param>
        /// <param name="name">Name given to the bar object.</param>
        /// <param name="length">Bar length, matching the link.</param>
        /// <param name="height">Bar thickness.</param>
        /// <param name="color">Tint applied to the bar.</param>
        private void CreateBar(RectTransform parent, string name, float length, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(length, height);

            var image = go.AddComponent<Image>();
            image.sprite = _goldLinkSprite;
            image.color = color;
            image.raycastTarget = false;
            if (_glowMaterial != null) image.material = _glowMaterial;
            if (image.sprite != null) image.type = Image.Type.Sliced;
        }

        /// <summary>
        /// Adds the glowing orb that travels along a bridge link.
        /// </summary>
        /// <param name="parent">Link rect the pulse belongs to.</param>
        /// <param name="length">Link length the pulse travels.</param>
        private void CreatePulse(RectTransform parent, float length)
        {
            var go = new GameObject("Pulse", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
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
            pulse.Configure(length, _pulseCycleSeconds);
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
