namespace Constellation.UI
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Lightweight coroutine tweens for UI juice: hover scaling, fades, punches and glow tints.
    /// Kept dependency-free so no external tweening package is required.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIImageTween : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Transform scaled on hover or punch. Defaults to this transform.")]
        [SerializeField] private Transform _scaleTarget;

        [Tooltip("Canvas group faded by the fade helpers.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("Image whose colour is tinted by the glow helpers.")]
        [SerializeField] private Image _glowImage;

        [Header("Hover")]
        [Tooltip("Scale applied while the element is hovered or selected.")]
        [SerializeField] private float _hoverScale = 1.05f;

        [Tooltip("Seconds taken to reach the hover scale.")]
        [SerializeField] private float _hoverDuration = 0.12f;

        [Header("Glow")]
        [Tooltip("Colour blended into the glow image while hovered.")]
        [SerializeField] private Color _glowColor = new Color(0.35f, 0.95f, 1f, 1f);

        [Tooltip("Alpha of the glow while hovered.")]
        [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.85f;

        private Vector3 _baseScale = Vector3.one;
        private Color _baseGlowColor = Color.white;
        private Coroutine _scaleRoutine;
        private Coroutine _fadeRoutine;
        private Coroutine _glowRoutine;

        private void Awake()
        {
            if (_scaleTarget == null) _scaleTarget = transform;
            _baseScale = _scaleTarget.localScale;

            if (_glowImage != null) _baseGlowColor = _glowImage.color;
        }

        /// <summary>
        /// Scales the target up or back to its base scale.
        /// </summary>
        /// <param name="hovered">True to apply the hover scale.</param>
        public void SetHovered(bool hovered)
        {
            Vector3 target = hovered ? _baseScale * _hoverScale : _baseScale;
            StartScale(target, _hoverDuration);
            SetGlowing(hovered);
        }

        /// <summary>
        /// Fades the attached canvas group to an alpha over a duration.
        /// </summary>
        /// <param name="alpha">Target alpha, 0-1.</param>
        /// <param name="duration">Seconds the fade should take.</param>
        public void FadeTo(float alpha, float duration)
        {
            if (_canvasGroup == null)
            {
                Debug.LogWarning("[UIImageTween] FadeTo called with no CanvasGroup assigned on " + name + ".");
                return;
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine(Mathf.Clamp01(alpha), Mathf.Max(0.01f, duration)));
        }

        /// <summary>
        /// Briefly overshoots the scale to draw attention, then settles back.
        /// </summary>
        /// <param name="amount">Extra scale multiplier for the punch, e.g. 0.12 for +12%.</param>
        /// <param name="duration">Seconds for the whole punch.</param>
        public void Punch(float amount = 0.12f, float duration = 0.25f)
        {
            if (!gameObject.activeInHierarchy) return;

            Vector3 peak = _baseScale * (1f + amount);
            StartCoroutine(PunchRoutine(peak, Mathf.Max(0.05f, duration)));
        }

        /// <summary>
        /// Turns the glow tint on or off.
        /// </summary>
        /// <param name="glowing">True to blend in the glow colour.</param>
        public void SetGlowing(bool glowing)
        {
            if (_glowImage == null) return;

            Color target = glowing
                ? new Color(_glowColor.r, _glowColor.g, _glowColor.b, _glowAlpha)
                : _baseGlowColor;

            if (_glowRoutine != null) StopCoroutine(_glowRoutine);
            _glowRoutine = StartCoroutine(GlowRoutine(target, _hoverDuration));
        }

        private void StartScale(Vector3 target, float duration)
        {
            if (_scaleTarget == null) return;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleRoutine(target, Mathf.Max(0.01f, duration)));
        }

        private IEnumerator ScaleRoutine(Vector3 target, float duration)
        {
            Vector3 start = _scaleTarget.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _scaleTarget.localScale = Vector3.LerpUnclamped(start, target, SmoothStep(t));
                yield return null;
            }

            _scaleTarget.localScale = target;
            _scaleRoutine = null;
        }

        private IEnumerator PunchRoutine(Vector3 peak, float duration)
        {
            float half = duration * 0.4f;
            yield return ScaleRoutine(peak, half);
            yield return ScaleRoutine(_baseScale, duration - half);
        }

        private IEnumerator FadeRoutine(float alpha, float duration)
        {
            float start = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, alpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _canvasGroup.alpha = alpha;
            _fadeRoutine = null;
        }

        private IEnumerator GlowRoutine(Color target, float duration)
        {
            Color start = _glowImage.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _glowImage.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _glowImage.color = target;
            _glowRoutine = null;
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
