namespace Lifestories.Demo
{
    using UnityEngine;
    using UnityEngine.UI;
    using Lifestories.Events;

    /// <summary>
    /// Drives the loading screen progress bar and label from a float progress channel.
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        [Tooltip("Channel the SceneLoaderManager publishes normalized loading progress on.")]
        [SerializeField] private FloatEventChannelSO _loadingProgressChannel;

        [Tooltip("Image stretched horizontally from 0 to 1 to visualise loading progress.")]
        [SerializeField] private Image _progressFill;

        [Tooltip("Label showing the loading percentage.")]
        [SerializeField] private Text _progressLabel;

        private void OnEnable()
        {
            if (_loadingProgressChannel != null)
            {
                _loadingProgressChannel.Subscribe(handleProgressChanged);
            }

            handleProgressChanged(0f);
        }

        private void OnDisable()
        {
            if (_loadingProgressChannel != null)
            {
                _loadingProgressChannel.Unsubscribe(handleProgressChanged);
            }
        }

        /* Stretches the fill image and refreshes the percentage label. */
        private void handleProgressChanged(float normalizedProgress)
        {
            float clampedProgress = Mathf.Clamp01(normalizedProgress);

            if (_progressFill != null)
            {
                RectTransform fillRect = _progressFill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(clampedProgress, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }

            if (_progressLabel != null)
            {
                _progressLabel.text = $"Loading {Mathf.RoundToInt(clampedProgress * 100f)}%";
            }
        }
    }
}
