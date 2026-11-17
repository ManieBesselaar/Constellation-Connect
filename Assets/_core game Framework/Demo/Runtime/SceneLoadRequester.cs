namespace Lifestories.Demo
{
    using UnityEngine;
    using Lifestories.Navigation;

    /// <summary>
    /// Raises a scene load request for a fixed scene asset. Bind <see cref="RequestLoad"/> to a UI Button.
    /// </summary>
    public class SceneLoadRequester : MonoBehaviour
    {
        [Tooltip("Scene asset requested when RequestLoad is called.")]
        [SerializeField] private GameSceneSO _sceneToLoad;

        [Tooltip("Channel the request is raised on. The SceneLoaderManager must listen to the same channel.")]
        [SerializeField] private SceneLoadEventChannelSO _loadSceneRequestChannel;

        /// <summary>
        /// Raises the scene load request. Safe to bind to a Button onClick event.
        /// </summary>
        public void RequestLoad()
        {
            if (_sceneToLoad == null || _loadSceneRequestChannel == null)
            {
                Debug.LogWarning("[SceneLoadRequester] Request ignored: scene asset or scene load request channel is not assigned.");
                return;
            }

            _loadSceneRequestChannel.RaiseEvent(_sceneToLoad);
        }
    }
}
