namespace Lifestories.Demo
{
    using UnityEngine;

    /// <summary>
    /// Quits the player, or stops play mode when running inside the editor.
    /// </summary>
    public class ApplicationQuitter : MonoBehaviour
    {
        /// <summary>
        /// Quits the application. Safe to bind to a Button onClick event.
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
