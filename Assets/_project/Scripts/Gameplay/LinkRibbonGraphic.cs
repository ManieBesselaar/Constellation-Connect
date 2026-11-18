namespace Constellation.Gameplay
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Draws a soft-edged ribbon along a polyline inside a canvas, so a link that curves still reads as
    /// one continuous beam instead of a chain of separate bars.
    /// </summary>
    public class LinkRibbonGraphic : MaskableGraphic
    {
        private const int ProfileWidth = 4;
        private const int ProfileHeight = 64;

        /// <summary>Portion of the path at each end that fades out, so the ribbon finishes softly.</summary>
        private const float EndFadeFraction = 0.08f;

        private static Texture2D _profile;

        private readonly List<Vector2> _points = new List<Vector2>();

        [SerializeField] private float _width = 14f;

        /// <summary>
        /// The soft profile the ribbon is textured with: opaque down the middle, clear at both edges.
        /// </summary>
        public override Texture mainTexture => GetProfile();

        /// <summary>
        /// Replaces the polyline the ribbon follows.
        /// </summary>
        /// <param name="points">Points in this graphic's local space.</param>
        /// <param name="width">Ribbon width, measured across the polyline.</param>
        public void SetPath(List<Vector2> points, float width)
        {
            _points.Clear();
            if (points != null) _points.AddRange(points);

            _width = Mathf.Max(1f, width);
            SetVerticesDirty();
        }

        /// <summary>
        /// Builds the ribbon itself: two vertices per point, offset along the local normal.
        /// </summary>
        /// <param name="vh">Vertex helper the canvas renders.</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2) return;

            float half = _width * 0.5f;
            Color32 tint = color;
            int last = _points.Count - 1;

            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 normal = NormalAt(i);
                Vector2 point = _points[i];

                float fromEnd = Mathf.Min(i, last - i) / (float)last;
                float fade = Mathf.Clamp01(fromEnd / EndFadeFraction);

                Color32 endColor = tint;
                endColor.a = (byte)Mathf.RoundToInt(tint.a * Mathf.Lerp(0.25f, 1f, fade));

                vh.AddVert(point + normal * half, endColor, new Vector2(0f, 1f));
                vh.AddVert(point - normal * half, endColor, new Vector2(0f, 0f));
            }

            for (int i = 0; i < last; i++)
            {
                int index = i * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }

        /// <summary>
        /// Normal of the ribbon at one point, averaged from the segments either side of it so the
        /// width stays even around a bend.
        /// </summary>
        /// <param name="index">Index of the point.</param>
        /// <returns>A unit vector across the path.</returns>
        private Vector2 NormalAt(int index)
        {
            Vector2 before = index > 0 ? Direction(_points[index - 1], _points[index]) : Vector2.zero;
            Vector2 after = index < _points.Count - 1 ? Direction(_points[index], _points[index + 1]) : Vector2.zero;

            Vector2 sum = before + after;
            if (sum.sqrMagnitude < 0.0001f) sum = after.sqrMagnitude > 0.0001f ? after : before;
            if (sum.sqrMagnitude < 0.0001f) return Vector2.up;

            sum.Normalize();
            return new Vector2(-sum.y, sum.x);
        }

        private static Vector2 Direction(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return delta.sqrMagnitude < 0.0001f ? Vector2.zero : delta.normalized;
        }

        /// <summary>
        /// Builds the profile texture the ribbon is drawn with, once per session.
        /// </summary>
        /// <returns>A white texture that is solid down the middle and clear at both edges.</returns>
        private static Texture2D GetProfile()
        {
            if (_profile != null) return _profile;

            _profile = new Texture2D(ProfileWidth, ProfileHeight, TextureFormat.RGBA32, false)
            {
                name = "StarLinkRibbonProfile",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[ProfileWidth * ProfileHeight];
            float half = (ProfileHeight - 1) * 0.5f;

            for (int y = 0; y < ProfileHeight; y++)
            {
                float distance = Mathf.Abs(y - half) / half;

                // Solid middle, soft rim, so the ribbon reads as a glow rather than a flat stripe.
                float alpha = Mathf.Clamp01((1f - distance) / 0.6f);
                alpha = alpha * alpha * (3f - 2f * alpha);

                var pixel = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));

                for (int x = 0; x < ProfileWidth; x++)
                {
                    pixels[y * ProfileWidth + x] = pixel;
                }
            }

            _profile.SetPixels32(pixels);
            _profile.Apply(false, true);
            return _profile;
        }
    }
}
