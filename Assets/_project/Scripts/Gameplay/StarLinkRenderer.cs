namespace Constellation.Gameplay
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Draws the star links between placed anchor nodes as rotated UI segments,
    /// using the cyan link art for normal connections and gold for bridge links.
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

        private readonly List<Image> _segments = new List<Image>();

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

            _segments.Add(image);
        }

        /// <summary>
        /// Removes every drawn link.
        /// </summary>
        public void ClearLinks()
        {
            foreach (Image segment in _segments)
            {
                if (segment != null) Destroy(segment.gameObject);
            }

            _segments.Clear();
        }
    }
}
