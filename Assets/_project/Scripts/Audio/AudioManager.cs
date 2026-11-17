namespace Constellation.Audio
{
    using Constellation.Save;
    using UnityEngine;

    /// <summary>
    /// Central audio routing for the game: one looping music channel and one one-shot
    /// sound effect channel, both scaled by the saved master/music/sfx volumes.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        /// <summary>The active audio manager, or null when none exists in the scene.</summary>
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [Tooltip("Audio source used for looping background music.")]
        [SerializeField] private AudioSource _bgmSource;

        [Tooltip("Audio source used for one-shot sound effects.")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("Content")]
        [Tooltip("Music clip started automatically when the game boots.")]
        [SerializeField] private AudioClip _defaultBgm;

        [Header("Sound Effects")]
        [Tooltip("Played when the player starts dragging a phrase tile out of the tray.")]
        [SerializeField] private AudioClip _tilePickup;

        [Tooltip("Played when a phrase tile lands on the anchor or bridge that expects it.")]
        [SerializeField] private AudioClip _tileSnapCorrect;

        [Tooltip("Played when a phrase tile is dropped on a node that does not want it.")]
        [SerializeField] private AudioClip _tileRippleWrong;

        [Tooltip("Played the moment every chunk of a single verse is connected.")]
        [SerializeField] private AudioClip _verseComplete;

        [Tooltip("Played when the player spends points on a hint.")]
        [SerializeField] private AudioClip _hintPulse;

        [Tooltip("Played once per second during the last ten seconds of the study timer.")]
        [SerializeField] private AudioClip _timerTickWarning;

        [Tooltip("Played when the sixth verse completes and the victory phase opens.")]
        [SerializeField] private AudioClip _constellationVictory;

        [Header("Mix")]
        [Tooltip("Headroom applied to the music channel before the saved music volume.")]
        [SerializeField, Range(0f, 1f)] private float _bgmLevel = 0.65f;

        [Tooltip("Headroom applied to the sound effect channel before the saved sfx volume.")]
        [SerializeField, Range(0f, 1f)] private float _sfxLevel = 0.9f;

        // Static state survives play sessions when domain reload is disabled, so it must be reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioManager] A second AudioManager was found and destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
            }

            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            ApplyVolumes();
        }

        private void Start()
        {
            if (_defaultBgm != null)
            {
                PlayBgm(_defaultBgm);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Starts looping background music, ignoring a request for the clip already playing.
        /// </summary>
        /// <param name="clip">Clip to play. Null stops the music.</param>
        /// <param name="loop">Whether the clip should loop.</param>
        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                StopBgm();
                return;
            }

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.time = 0f;
            _bgmSource.Play();
            Debug.Log($"[AudioManager] BGM playing '{clip.name}' loop={loop}.");
        }

        /// <summary>
        /// Stops the background music channel.
        /// </summary>
        public void StopBgm()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
            Debug.Log("[AudioManager] BGM stopped.");
        }

        /// <summary>
        /// Plays a one-shot sound effect.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volumeScale">Additional per-call volume scale, 0-1.</param>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlaySfx called with a null clip.");
                return;
            }

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        /// <summary>
        /// Recalculates both channel volumes from the saved settings.
        /// </summary>
        public void ApplyVolumes()
        {
            float master = SaveService.MasterVolume;
            float music = IsMusicMuted ? 0f : SaveService.MusicVolume;

            _bgmSource.volume = master * music * _bgmLevel;
            _sfxSource.volume = master * SaveService.SfxVolume * _sfxLevel;
        }

        /// <summary>
        /// True when the player has muted the music channel. Muting leaves the clip playing at zero
        /// volume, so unmuting resumes at the same point and the saved music level is never lost.
        /// </summary>
        public bool IsMusicMuted => SaveService.MusicMuted;

        /// <summary>
        /// Stores a new master volume and applies it.
        /// </summary>
        /// <param name="value">Volume in the 0-1 range.</param>
        public void SetMasterVolume(float value)
        {
            SaveService.MasterVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        /// <summary>
        /// Stores a new music volume and applies it.
        /// </summary>
        /// <param name="value">Volume in the 0-1 range.</param>
        public void SetMusicVolume(float value)
        {
            SaveService.MusicVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        /// <summary>
        /// Stores a new sound effect volume and applies it.
        /// </summary>
        /// <param name="value">Volume in the 0-1 range.</param>
        public void SetSfxVolume(float value)
        {
            SaveService.SfxVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        /// <summary>
        /// Mutes or unmutes the music channel, keeping the saved music volume untouched.
        /// </summary>
        /// <param name="muted">True to silence the music.</param>
        public void SetMusicMuted(bool muted)
        {
            SaveService.MusicMuted = muted;
            ApplyVolumes();
        }

        /// <summary>
        /// Flips the music mute state.
        /// </summary>
        /// <returns>The new mute state.</returns>
        public bool ToggleMusicMuted()
        {
            SetMusicMuted(!SaveService.MusicMuted);
            return IsMusicMuted;
        }

        // ------------------------------------------------------------------ named cues

        // Gameplay code calls these so no scene object has to hold a clip reference. They are
        // no-ops when the persistent audio manager is absent, which keeps scenes playable on their own.

        /// <summary>Plays the tile pickup cue.</summary>
        public static void PlayTilePickup()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._tilePickup);
        }

        /// <summary>Plays the correct-snap cue.</summary>
        public static void PlayTileSnapCorrect()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._tileSnapCorrect);
        }

        /// <summary>Plays the wrong-drop ripple cue.</summary>
        public static void PlayTileRippleWrong()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._tileRippleWrong);
        }

        /// <summary>Plays the single-verse completion cue.</summary>
        public static void PlayVerseComplete()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._verseComplete);
        }

        /// <summary>Plays the hint cue.</summary>
        public static void PlayHintPulse()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._hintPulse);
        }

        /// <summary>Plays the study timer warning tick.</summary>
        public static void PlayTimerTickWarning()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._timerTickWarning);
        }

        /// <summary>Plays the constellation victory fanfare.</summary>
        public static void PlayConstellationVictory()
        {
            if (Instance == null) return;
            Instance.PlaySfx(Instance._constellationVictory);
        }
    }
}
