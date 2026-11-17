namespace Constellation.Gameplay
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// How-to-play overlay raised when the gameplay scene loads. The run is held until the player
    /// dismisses it, so the study timer does not tick away behind the instructions.
    /// </summary>
    public class HowToPlayView : MonoBehaviour
    {
        [Tooltip("Panel holding the dim and the instruction card. Hidden until Show is called.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Button pressed to close the overlay.")]
        [SerializeField] private Button _dismissButton;

        /// <summary>Raised once the player closes the overlay.</summary>
        public event Action Dismissed;

        private void Awake()
        {
            if (_dismissButton != null) _dismissButton.onClick.AddListener(HandleDismiss);
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_dismissButton != null) _dismissButton.onClick.RemoveListener(HandleDismiss);
        }

        /// <summary>
        /// Opens the overlay and logs that the run is waiting on the player.
        /// </summary>
        public void Show()
        {
            if (_panel == null)
            {
                Debug.LogWarning("[HowToPlayView] No panel assigned; raising Dismissed immediately.");
                Dismissed?.Invoke();
                return;
            }

            _panel.SetActive(true);
            Debug.Log("[HowToPlayView] Instructions shown.");
        }

        private void HandleDismiss()
        {
            if (_panel != null) _panel.SetActive(false);

            Debug.Log("[HowToPlayView] Instructions dismissed.");
            Dismissed?.Invoke();
        }
    }
}
