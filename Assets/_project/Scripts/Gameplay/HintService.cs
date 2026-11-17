namespace Constellation.Gameplay
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Owns the hint button so the connect phase does not have to wire UI itself.
    /// Taking a hint costs <see cref="ConnectPhaseController.HintPenalty"/> points.
    /// </summary>
    public class HintService : MonoBehaviour
    {
        [Tooltip("Button the player presses to reveal the next chunk.")]
        [SerializeField] private Button _hintButton;

        [Tooltip("Controller that resolves and scores the hint.")]
        [SerializeField] private ConnectPhaseController _controller;

        private void Awake()
        {
            if (_hintButton == null) return;

            _hintButton.onClick.AddListener(RequestHint);
        }

        private void OnDestroy()
        {
            if (_hintButton == null) return;

            _hintButton.onClick.RemoveListener(RequestHint);
        }

        /// <summary>
        /// Enables or disables the hint button.
        /// </summary>
        /// <param name="interactable">True to allow hints.</param>
        public void SetInteractable(bool interactable)
        {
            if (_hintButton != null) _hintButton.interactable = interactable;
        }

        private void RequestHint()
        {
            if (_controller == null)
            {
                Debug.LogWarning("[HintService] No connect phase controller assigned.");
                return;
            }

            _controller.RequestHint();
        }
    }
}
