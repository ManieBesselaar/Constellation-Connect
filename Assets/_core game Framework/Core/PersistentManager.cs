namespace Lifestories.Core
{
    using UnityEngine;

    /// <summary>
    /// Ensures core framework managers persist across scene transitions.
    /// </summary>
    public class PersistentManager : MonoBehaviour
    {
        private static PersistentManager _instance;

        /// <summary>
        /// The persistent instance, or null when no persistent manager exists in the session.
        /// </summary>
        public static PersistentManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // DontDestroyOnLoad only accepts root objects; a nested manager already persists
            // through its parent and the call would log a warning.
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        // Static state survives play sessions when domain reload is disabled, so it must be reset explicitly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void resetStatics()
        {
            _instance = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
