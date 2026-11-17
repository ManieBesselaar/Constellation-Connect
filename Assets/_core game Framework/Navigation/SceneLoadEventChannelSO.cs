namespace Lifestories.Navigation
{
    using UnityEngine;
    using Lifestories.Events;

    /// <summary>
    /// ScriptableObject event channel for broadcasting scene load requests.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSceneLoadEventChannel", menuName = "Lifestories/Navigation/Scene Load Event Channel")]
    public class SceneLoadEventChannelSO : EventChannelSO<GameSceneSO>
    {
    }
}