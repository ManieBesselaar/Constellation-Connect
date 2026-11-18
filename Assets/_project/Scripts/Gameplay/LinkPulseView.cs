namespace Constellation.Gameplay
{
    using UnityEngine;

    /// <summary>
    /// Walks a glow along a drawn link, from its start to its end, on a continuous loop.
    /// </summary>
    public class LinkPulseView : MonoBehaviour
    {
        [Tooltip("Seconds one start-to-finish pass takes.")]
        [SerializeField] private float _cycleSeconds = 1f;

        private RectTransform _rect;
        private float _length;
        private float _elapsed;

        /// <summary>
        /// Sets the travel path the pulse runs along.
        /// </summary>
        /// <param name="length">Link length, in the link root's units.</param>
        /// <param name="cycleSeconds">Seconds for one start-to-finish pass.</param>
        public void Configure(float length, float cycleSeconds)
        {
            _rect = (RectTransform)transform;
            _length = Mathf.Max(0f, length);
            _cycleSeconds = Mathf.Max(0.05f, cycleSeconds);
            _elapsed = 0f;

            _rect.anchoredPosition = new Vector2(0f, _rect.anchoredPosition.y);
        }

        private void Update()
        {
            if (_rect == null || _length <= 0f) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Repeat(_elapsed / _cycleSeconds, 1f);

            Vector2 position = _rect.anchoredPosition;
            position.x = t * _length;
            _rect.anchoredPosition = position;
        }
    }
}
