namespace Lifestories.Demo
{
    using UnityEngine;

    /// <summary>
    /// Rotates a RectTransform using scaled time, so the rotation visibly freezes while the game is paused.
    /// </summary>
    public class DemoSpinner : MonoBehaviour
    {
        [Tooltip("RectTransform rotated by this component.")]
        [SerializeField] private RectTransform _target;

        [Tooltip("Rotation speed in degrees per second of scaled game time.")]
        [SerializeField] private float _degreesPerSecond = 120f;

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            _target.Rotate(0f, 0f, -_degreesPerSecond * Time.deltaTime);
        }
    }
}
