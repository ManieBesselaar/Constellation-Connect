namespace Constellation.Gameplay
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// The junction a bridge chunk links through. One slot per bridge chunk sharing the keyword is
    /// created under it by the connect phase.
    /// </summary>
    public class BridgeHubView : MonoBehaviour
    {
        [Tooltip("Hub image swapped on fill.")]
        [SerializeField] private Image _hub;

        [Tooltip("Label showing the bridge keyword.")]
        [SerializeField] private TextMeshProUGUI _keywordLabel;

        [Tooltip("Sprite used once the bridge is completed.")]
        [SerializeField] private Sprite _completedSprite;

        private Color _restColor = Color.white;

        /// <summary>The bridge keyword this hub represents.</summary>
        public string BridgeTag { get; private set; }

        private void Awake()
        {
            if (_hub != null) _restColor = _hub.color;
        }

        /// <summary>
        /// Binds the hub to a bridge keyword.
        /// </summary>
        /// <param name="tag">Keyword shared by the chunks this hub takes.</param>
        public void Bind(string tag)
        {
            BridgeTag = tag;
            if (_keywordLabel != null) _keywordLabel.text = tag;
        }

        /// <summary>
        /// Lights the hub and swaps in its completed sprite when its first chunk lands.
        /// </summary>
        public void SetCompleteVisual()
        {
            if (_hub == null) return;

            if (_completedSprite != null) _hub.sprite = _completedSprite;
            _hub.color = _restColor;
        }

        /// <summary>
        /// Forces the hub colour, used for the victory tint.
        /// </summary>
        /// <param name="color">Colour to apply.</param>
        public void SetFillColor(Color color)
        {
            if (_hub != null) _hub.color = color;
        }

        /// <summary>
        /// Pulses the hub to draw the player's eye.
        /// </summary>
        public void Pulse()
        {
            var tween = GetComponent<Constellation.UI.UIImageTween>();
            if (tween != null) tween.Punch(0.14f, 0.5f);
        }
    }
}
