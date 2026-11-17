namespace Lifestories.Demo
{
    using UnityEngine;
    using UnityEngine.UI;
    using Lifestories.Pause;

    /// <summary>
    /// Demonstrates the IPausable contract: registers with the PauseManager and reflects the pause state in a label.
    /// </summary>
    public class PauseController : MonoBehaviour, IPausable
    {
        [Tooltip("Label showing whether the game is running or paused.")]
        [SerializeField] private Text _statusLabel;

        private void OnEnable()
        {
            PauseManager.Register(this);
            refreshStatusLabel();
        }

        private void OnDisable()
        {
            PauseManager.Unregister(this);
        }

        /// <summary>
        /// Toggles the pause state. Safe to bind to a Button onClick event.
        /// </summary>
        public void TogglePause()
        {
            PauseManager.TogglePause();
            refreshStatusLabel();
        }

        /// <summary>
        /// Called by the PauseManager when the game pauses.
        /// </summary>
        public void Pause()
        {
            refreshStatusLabel();
        }

        /// <summary>
        /// Called by the PauseManager when the game resumes.
        /// </summary>
        public void Resume()
        {
            refreshStatusLabel();
        }

        private void refreshStatusLabel()
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = PauseManager.IsPaused ? "Paused" : "Running";
            }
        }
    }
}
