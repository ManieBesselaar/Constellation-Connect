namespace Lifestories.Demo
{
    using UnityEngine;
    using Lifestories.UI;

    /// <summary>
    /// Fades a CanvasGroup in on Start using the shared <see cref="CanvasGroupFader"/>.
    /// </summary>
    public class DemoFadeInOnStart : MonoBehaviour
    {
        [Tooltip("Fader used to animate the canvas group.")]
        [SerializeField] private CanvasGroupFader _canvasGroupFader;

        [Tooltip("CanvasGroup faded in when this component starts.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Tooltip("Fade duration in unscaled seconds.")]
        [SerializeField] private float _duration = 0.4f;

        private void Start()
        {
            if (_canvasGroupFader == null || _canvasGroup == null)
            {
                return;
            }

            _canvasGroupFader.FadeIn(_canvasGroup, _duration);
        }
    }
}
