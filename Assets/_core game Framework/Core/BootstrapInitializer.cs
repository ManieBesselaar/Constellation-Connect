namespace Lifestories.Core
{
    using System.Collections;
    using UnityEngine;
    using Lifestories.Navigation;
    using Lifestories.SaveSystem;

    /// <summary>
    /// Runs the startup sequence: loads persistent player state, then requests the initial menu scene.
    /// </summary>
    public class BootstrapInitializer : MonoBehaviour
    {
        [Header("Initial Scene Target")]
        [Tooltip("Scene asset requested once the boot sequence completes, normally the main menu.")]
        [SerializeField] private GameSceneSO _mainMenuSceneSO;

        [Tooltip("Channel the boot sequence raises the initial scene load request on.")]
        [SerializeField] private SceneLoadEventChannelSO _loadSceneRequestChannel;

        private void Start()
        {
            StartCoroutine(executeBootSequenceRoutine());
        }

        /* Executes the startup initialization sequence and loads player preferences. */
        private IEnumerator executeBootSequenceRoutine()
        {
            // 1. Load player options and progression state; the result is cached in SaveSystemManager.CurrentData
            SaveSystemManager.Load();

            // 2. Yield so every other manager in the bootstrap scene finishes its own Awake and OnEnable first
            yield return null;

            // 3. Request the asynchronous transition to the initial scene
            if (_loadSceneRequestChannel == null || _mainMenuSceneSO == null)
            {
                Debug.LogWarning("[BootstrapInitializer] Boot sequence stopped: the initial scene asset or the scene load request channel is not assigned.");
                yield break;
            }

            _loadSceneRequestChannel.RaiseEvent(_mainMenuSceneSO);
        }
    }
}
