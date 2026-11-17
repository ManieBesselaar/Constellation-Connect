namespace Constellation.Menu
{
    using Constellation.Audio;
    using Constellation.Localization;
    using Constellation.Save;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Settings modal: study timer choice, master/music/sfx volumes and the WEB attribution notice.
    /// Every change is written straight through <see cref="SaveService"/> and re-applied to the mixer.
    /// </summary>
    public class SettingsModalView : MonoBehaviour
    {
        [Header("Roots")]
        [Tooltip("Panel shown while the modal is open.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Button that closes the modal.")]
        [SerializeField] private Button _closeButton;

        [Header("Study Timer")]
        [Tooltip("Selects a 30 second study phase.")]
        [SerializeField] private Button _timer30Button;

        [Tooltip("Selects a 60 second study phase.")]
        [SerializeField] private Button _timer60Button;

        [Tooltip("Selects a 90 second study phase.")]
        [SerializeField] private Button _timer90Button;

        [Tooltip("Selects Zen mode, which has no countdown.")]
        [SerializeField] private Button _timerZenButton;

        [Tooltip("Label summarising the current study timer choice.")]
        [SerializeField] private TextMeshProUGUI _timerSummary;

        [Header("Audio")]
        [Tooltip("Master volume slider, 0-1.")]
        [SerializeField] private Slider _masterSlider;

        [Tooltip("Music volume slider, 0-1.")]
        [SerializeField] private Slider _musicSlider;

        [Tooltip("Sound effect volume slider, 0-1.")]
        [SerializeField] private Slider _sfxSlider;

        [Header("Attribution")]
        [Tooltip("Label holding the World English Bible public domain notice.")]
        [SerializeField] private TextMeshProUGUI _attributionLabel;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            if (_timer30Button != null) _timer30Button.onClick.AddListener(() => SelectTimer(30));
            if (_timer60Button != null) _timer60Button.onClick.AddListener(() => SelectTimer(60));
            if (_timer90Button != null) _timer90Button.onClick.AddListener(() => SelectTimer(90));
            if (_timerZenButton != null) _timerZenButton.onClick.AddListener(() => SelectTimer(0));

            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(HandleMusicChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);

            if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
            if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        }

        /// <summary>
        /// Shows the modal and refreshes every control from the saved settings.
        /// </summary>
        public void Open()
        {
            if (_panel == null)
            {
                Debug.LogError("[SettingsModalView] No panel assigned.");
                return;
            }

            _panel.SetActive(true);
            RefreshControls();
        }

        /// <summary>
        /// Hides the modal.
        /// </summary>
        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void RefreshControls()
        {
            if (_attributionLabel != null)
            {
                _attributionLabel.text = LocalizedText.Get(
                    "TRANSLATION_ATTRIBUTION",
                    "Scripture quotations taken from the World English Bible (WEB), Public Domain.");
            }

            if (_timerSummary != null)
            {
                _timerSummary.text = SaveService.Data.ZenMode
                    ? "Zen (no timer)"
                    : SaveService.Data.StudyDurationSeconds + " seconds";
            }

            // Assigning a slider value raises its callback, so the listeners are suspended while seeding.
            SeedSlider(_masterSlider, SaveService.MasterVolume);
            SeedSlider(_musicSlider, SaveService.MusicVolume);
            SeedSlider(_sfxSlider, SaveService.SfxVolume);
        }

        private static void SeedSlider(Slider slider, float value)
        {
            if (slider == null) return;

            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        private void SelectTimer(int seconds)
        {
            SaveService.SetStudyDuration(seconds);
            RefreshControls();
        }

        private void HandleMasterChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
            else SaveService.MasterVolume = value;
        }

        private void HandleMusicChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
            else SaveService.MusicVolume = value;
        }

        private void HandleSfxChanged(float value)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
            else SaveService.SfxVolume = value;
        }
    }
}
