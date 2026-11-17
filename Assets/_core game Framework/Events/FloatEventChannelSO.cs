namespace Lifestories.Events
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject event channel for broadcasting single float values.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFloatEventChannel", menuName = "Lifestories/Events/Float Event Channel")]
    public class FloatEventChannelSO : EventChannelSO<float>
    {
    }
}