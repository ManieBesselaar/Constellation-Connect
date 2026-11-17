namespace Lifestories.Events
{
    using System;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject event channel for parameterless broadcast signals.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVoidEventChannel", menuName = "Lifestories/Events/Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        private Action _onEventRaised;

        /// <summary>
        /// Raises the parameterless event signal.
        /// </summary>
        public void RaiseEvent()
        {
            _onEventRaised?.Invoke();
        }

        /// <summary>
        /// Subscribes a parameterless listener method to this channel.
        /// </summary>
        /// <param name="listener">Method callback to invoke when event is raised.</param>
        public void Subscribe(Action listener)
        {
            _onEventRaised += listener;
        }

        /// <summary>
        /// Unsubscribes a parameterless listener method from this channel.
        /// </summary>
        /// <param name="listener">Method callback to remove.</param>
        public void Unsubscribe(Action listener)
        {
            _onEventRaised -= listener;
        }
    }
}