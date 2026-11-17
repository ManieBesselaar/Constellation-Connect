namespace Constellation.UI
{
    using Constellation.Audio;
    using Constellation.Save;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Music mute button on the gameplay HUD. Swaps between the music-on and music-off glyphs and
    /// keeps the persistent <see cref="AudioManager"/> in step with the saved mute state.
    /// </summary>
    public class MusicMuteToggleView : MonoBehaviour
    {
        [Tooltip("Button pressed to toggle the music.")]
        [SerializeField] private Button _button;

        [Tooltip("Image showing the glyph for the current state.")]
        [SerializeField] private Image _icon;

        [Tooltip("Glyph shown while the music is audible.")]
        [SerializeField] private Sprite _musicOnSprite;

        [Tooltip("Glyph shown while the music is muted.")]
        [SerializeField] private Sprite _musicOffSprite;

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>
        /// Repaints the glyph from the current mute state.
        /// </summary>
        public void Refresh()
        {
            bool muted = AudioManager.Instance != null
                ? AudioManager.Instance.IsMusicMuted
                : SaveService.MusicMuted;

            if (_icon == null) return;

            Sprite wanted = muted ? _musicOffSprite : _musicOnSprite;

            if (wanted != null && _icon.sprite != wanted) _icon.sprite = wanted;
        }

        private void HandleClick()
        {
            // The gameplay scene can be opened on its own in the editor, in which case there is no
            // persistent audio manager and only the saved flag can be flipped.
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusicMuted();
            else SaveService.MusicMuted = !SaveService.MusicMuted;

            Refresh();
        }
    }
}
