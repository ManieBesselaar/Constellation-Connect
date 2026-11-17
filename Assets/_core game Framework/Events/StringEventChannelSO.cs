namespace Lifestories.Events
{
    using UnityEngine;

    /// <summary>
    /// ScriptableObject event channel for broadcasting string parameters or localization keys.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStringEventChannel", menuName = "Lifestories/Events/String Event Channel")]
    public class StringEventChannelSO : EventChannelSO<string>
    {
    }
}