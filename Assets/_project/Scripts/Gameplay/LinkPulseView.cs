namespace Constellation.Gameplay
{
    using UnityEngine;

    /// <summary>
    /// Walks a glow along a drawn link, following the curve of the link, on a continuous loop.
    /// </summary>
    public class LinkPulseView : MonoBehaviour
    {
        [Tooltip("Seconds one start-to-finish pass takes.")]
        [SerializeField] private float _cycleSeconds = 1f;

        private RectTransform _rect;
        private Vector2[] _path;
        private float[] _distances;
        private float _length;
        private float _elapsed;

        /// <summary>
        /// Sets the travel path the pulse runs along.
        /// </summary>
        /// <param name="path">Points the pulse walks, in its parent's local space.</param>
        /// <param name="cycleSeconds">Seconds for one start-to-finish pass.</param>
        public void Configure(Vector2[] path, float cycleSeconds)
        {
            _rect = (RectTransform)transform;
            _path = path;
            _cycleSeconds = Mathf.Max(0.05f, cycleSeconds);
            _elapsed = 0f;

            _distances = new float[_path == null ? 0 : _path.Length];
            _length = 0f;

            for (int i = 0; i < _distances.Length; i++)
            {
                if (i > 0) _length += Vector2.Distance(_path[i - 1], _path[i]);
                _distances[i] = _length;
            }

            _rect.anchoredPosition = _distances.Length > 0 ? _path[0] : Vector2.zero;
        }

        private void Update()
        {
            if (_rect == null || _length <= 0f) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Repeat(_elapsed / _cycleSeconds, 1f);

            Sample(t * _length, out Vector2 position, out float degrees);

            _rect.anchoredPosition = position;
            _rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>
        /// Finds the point a given distance along the path, and the direction of travel there.
        /// </summary>
        /// <param name="distance">Distance travelled from the start of the path.</param>
        /// <param name="position">Point reached, in the parent's local space.</param>
        /// <param name="degrees">Angle of travel at that point, in degrees.</param>
        private void Sample(float distance, out Vector2 position, out float degrees)
        {
            int last = _path.Length - 1;

            for (int i = 1; i <= last; i++)
            {
                if (distance > _distances[i]) continue;

                Vector2 delta = _path[i] - _path[i - 1];
                float span = _distances[i] - _distances[i - 1];
                float t = span <= 0.0001f ? 0f : (distance - _distances[i - 1]) / span;

                position = Vector2.Lerp(_path[i - 1], _path[i], t);
                degrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                return;
            }

            Vector2 tail = _path[last] - _path[last - 1];
            position = _path[last];
            degrees = Mathf.Atan2(tail.y, tail.x) * Mathf.Rad2Deg;
        }
    }
}
