namespace Lifestories.Events
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject event channel for broadcasting 3D spatial position vectors.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVector3EventChannel", menuName = "Lifestories/Events/Vector3 Event Channel")]
    public class Vector3EventChannelSO : EventChannelSO<Vector3>
    {
    }
}