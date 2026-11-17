namespace Constellation.Core
{
    using Constellation.Data;
    using UnityEngine;

    /// <summary>
    /// Carries the player's constellation choice from the menu scene into the gameplay scene.
    /// Scenes load in <see cref="UnityEngine.SceneManagement.LoadSceneMode.Single"/> mode, so the
    /// framework's scene assets cannot carry a payload; this transient holder fills that gap and
    /// is deliberately not persisted.
    /// </summary>
    public static class GameSession
    {
        /// <summary>The constellation chosen on the menu, or null when a scene is entered directly.</summary>
        public static ConstellationSO SelectedConstellation { get; set; }

        // Static state survives play sessions when domain reload is disabled, so it must be reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SelectedConstellation = null;
        }
    }
}
