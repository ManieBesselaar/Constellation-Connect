namespace Constellation.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Drives a horizontal fill bar from a countdown, including a warning pulse in the final seconds.
    /// </summary>
    public class CountdownBar : MonoBehaviour
    {
        [Tooltip("Image whose anchorMax.x is driven from 0 to 1.")]
        [SerializeField] private RectTransform _fill;

        [Tooltip("Image tinted during the final warning window.")]
        [SerializeField] private Image _fillImage;

        [Tooltip("Seconds remaining at which the warning pulse begins.")]
        [SerializeField] private float _warningThreshold = 10f;

        [Tooltip("Colour used during the final seconds.")]
        [SerializeField] private Color _warningColor = new Color(1f, 0.45f, 0.4f, 1f);

        private Color _restColor = Color.white;
        private float _pulsePhase;

        /// <summary>Raised once when the countdown reaches zero.</summary>
        public event Action Expired;

        private void Awake()
        {
            if (_fillImage != null) _restColor = _fillImage.color;
        }

        /// <summary>
        /// Renders the remaining fraction, 0-1.
        /// </summary>
        /// <param name="fraction">Remaining time as a fraction of the total.</param>
        public void SetFraction(float fraction)
        {
            float clamped = Mathf.Clamp01(fraction);

            if (_fill != null)
            {
                Vector2 anchorMax = _fill.anchorMax;
                anchorMax.x = clamped;
                _fill.anchorMax = anchorMax;
            }

            if (_fillImage == null) return;

            bool warning = clamped > 0f && clamped * 100f <= _warningThreshold;
            _fillImage.color = warning ? Color.Lerp(_restColor, _warningColor, 0.5f + 0.5f * Mathf.Sin(_pulsePhase)) : _restColor;
        }

        /// <summary>
        /// Advances the warning pulse animation. Call once per frame while counting down.
        /// </summary>
        /// <param name="deltaTime">Unscaled delta time.</param>
        public void TickPulse(float deltaTime)
        {
            _pulsePhase += deltaTime * 9f;
        }

        /// <summary>
        /// Raises <see cref="Expired"/>. Called by the owner when time runs out.
        /// </summary>
        public void RaiseExpired()
        {
            Expired?.Invoke();
        }
    }
}
