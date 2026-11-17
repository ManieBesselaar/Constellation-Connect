namespace Lifestories.Navigation
{
    using Lifestories.Events;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Handles asynchronous scene loading through an intermediate loading screen while broadcasting progress.
    /// </summary>
    public class SceneLoaderManager : MonoBehaviour
    {
        [Header("Listening To Channels")]
        [Tooltip("Channel this manager listens to. Any GameSceneSO raised on it becomes the next scene to load.")]
        [SerializeField] private SceneLoadEventChannelSO _loadSceneRequestChannel;

        [Header("Publishing Channels")]
        [Tooltip("Channel publishing normalized loading progress (0 to 1) while the target scene streams in.")]
        [SerializeField] private FloatEventChannelSO _loadingProgressChannel;

        [Tooltip("Channel raised once the requested scene has finished activating.")]
        [SerializeField] private VoidEventChannelSO _onSceneLoadCompletedChannel;

        [Header("Scene Configuration")]
        [Tooltip("Scene displayed while another scene streams in. Leave empty to load targets directly.")]
        [SerializeField] private GameSceneSO _loadingScreenScene;

        [Tooltip("Minimum time in seconds the loading screen stays visible, so progress bars never flash past instantly.")]
        [SerializeField] private float _minimumLoadingScreenDuration = 0.75f;

        private GameSceneSO _targetScene;
        private AsyncOperation _targetLoadOperation;
        private Coroutine _activeLoadRoutine;
        private bool _isLoading;

        private void OnEnable()
        {
            if (_loadSceneRequestChannel != null)
            {
                _loadSceneRequestChannel.Subscribe(handleSceneLoadRequested);
            }
        }

        private void OnDisable()
        {
            if (_loadSceneRequestChannel != null)
            {
                _loadSceneRequestChannel.Unsubscribe(handleSceneLoadRequested);
            }

            // A coroutine stopped by a disabled or destroyed host would otherwise leave this manager locked forever.
            if (_activeLoadRoutine != null)
            {
                StopCoroutine(_activeLoadRoutine);
                _activeLoadRoutine = null;
            }

            _targetLoadOperation = null;
            _isLoading = false;
        }

        /* Handles incoming scene load requests and starts the asynchronous load sequence. */
        private void handleSceneLoadRequested(GameSceneSO targetScene)
        {
            if (targetScene == null)
            {
                Debug.LogWarning("[SceneLoaderManager] Scene load request ignored: the requested scene asset is null.");
                return;
            }

            if (string.IsNullOrEmpty(targetScene.SceneName))
            {
                Debug.LogWarning($"[SceneLoaderManager] Scene load request ignored: scene asset '{targetScene.name}' has no scene name configured.");
                return;
            }

            if (_isLoading)
            {
                Debug.LogWarning($"[SceneLoaderManager] Scene load request for '{targetScene.SceneName}' ignored because '{_targetScene?.SceneName}' is already loading.");
                return;
            }

            _targetScene = targetScene;
            _isLoading = true;
            _activeLoadRoutine = StartCoroutine(executeSceneLoadRoutine());
        }

        /* Shows the loading screen, streams the target scene, then activates it. */
        private IEnumerator executeSceneLoadRoutine()
        {
            Debug.Log($"[SceneLoaderManager] Scene load requested for '{_targetScene.SceneName}'.");
            _loadingProgressChannel?.RaiseEvent(0f);

            bool hasLoadingScreen = _loadingScreenScene != null
                && !string.IsNullOrEmpty(_loadingScreenScene.SceneName)
                && _loadingScreenScene.SceneName != _targetScene.SceneName;

            if (hasLoadingScreen)
            {
                AsyncOperation loadingScreenOperation = SceneManager.LoadSceneAsync(_loadingScreenScene.SceneName, LoadSceneMode.Single);

                if (loadingScreenOperation != null)
                {
                    while (!loadingScreenOperation.isDone)
                    {
                        yield return null;
                    }

                    // Give the loading screen controllers two frames to subscribe to the progress channel.
                    yield return null;
                    yield return null;
                }
                else
                {
                    Debug.LogWarning($"[SceneLoaderManager] Loading screen '{_loadingScreenScene.SceneName}' could not be loaded. Is it added to the build settings? Loading the target scene directly.");
                }
            }

            _targetLoadOperation = SceneManager.LoadSceneAsync(_targetScene.SceneName, LoadSceneMode.Single);

            if (_targetLoadOperation == null)
            {
                Debug.LogError($"[SceneLoaderManager] Could not start loading '{_targetScene.SceneName}'. Is the scene added to the build settings?");
                _targetScene = null;
                _activeLoadRoutine = null;
                _isLoading = false;
                yield break;
            }

            _targetLoadOperation.allowSceneActivation = false;

            float elapsedTime = 0f;

            // Unity stalls async progress at 0.9 until scene activation is allowed.
            while (_targetLoadOperation.progress < 0.9f)
            {
                float normalizedProgress = Mathf.Clamp01(_targetLoadOperation.progress / 0.9f);
                _loadingProgressChannel?.RaiseEvent(normalizedProgress);

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            _loadingProgressChannel?.RaiseEvent(1f);

            if (elapsedTime < _minimumLoadingScreenDuration)
            {
                yield return new WaitForSecondsRealtime(_minimumLoadingScreenDuration - elapsedTime);
            }

            _targetLoadOperation.allowSceneActivation = true;

            while (!_targetLoadOperation.isDone)
            {
                _loadingProgressChannel?.RaiseEvent(1f);
                yield return null;
            }

            string completedSceneName = _targetScene.SceneName;

            _targetLoadOperation = null;
            _targetScene = null;
            _activeLoadRoutine = null;
            _isLoading = false;

            Debug.Log($"[SceneLoaderManager] '{completedSceneName}' finished loading and is now the active scene.");
            _onSceneLoadCompletedChannel?.RaiseEvent();
        }
    }
}
