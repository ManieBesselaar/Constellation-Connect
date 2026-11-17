namespace Lifestories.Navigation
{
    using UnityEngine;

    /// <summary>
    /// Classification of a loadable scene, used to tell persistent, menu and gameplay scenes apart.
    /// </summary>
    public enum SceneType
    {
        /// <summary>
        /// Scene that is never unloaded and holds the persistent framework managers.
        /// </summary>
        Persistent,

        /// <summary>
        /// Front-end scene such as the main menu or the options panel.
        /// </summary>
        Menu,

        /// <summary>
        /// Gameplay scene streamed in behind the loading screen.
        /// </summary>
        Gameplay
    }

    /// <summary>
    /// ScriptableObject representing a loadable game scene.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameScene", menuName = "Lifestories/Navigation/Game Scene")]
    public class GameSceneSO : ScriptableObject
    {
        [Tooltip("Classification of this scene: persistent, menu or gameplay.")]
        [SerializeField] private SceneType _sceneType = SceneType.Gameplay;

        [Tooltip("Exact file name of the scene in the build settings, without the .unity extension.")]
        [SerializeField] private string _sceneName;

        [Tooltip("Localization key used to display a human readable name for this scene in menus.")]
        [SerializeField] private string _localizedDisplayNameKey;

        /// <summary>
        /// The classification type of the scene.
        /// </summary>
        public SceneType SceneType => _sceneType;

        /// <summary>
        /// Name of the build settings scene file, without the .unity extension.
        /// </summary>
        public string SceneName => _sceneName;

        /// <summary>
        /// Localization key for the scene's human readable display name.
        /// </summary>
        public string LocalizedDisplayNameKey => _localizedDisplayNameKey;
    }
}
