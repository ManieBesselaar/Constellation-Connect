namespace Lifestories.Events
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Abstract ScriptableObject base class for single-parameter event broadcast channels.
    /// </summary>
    /// <typeparam name="T">The data type passed to listeners upon invocation.</typeparam>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        private Action<T> _onEventRaised;

        /// <summary>
        /// Raises the event, broadcasting payload data to all subscribed listeners.
        /// </summary>
        /// <param name="value">Payload data emitted with the event.</param>
        public void RaiseEvent(T value)
        {
            _onEventRaised?.Invoke(value);
        }

        /// <summary>
        /// Subscribes a listener method to this event channel.
        /// </summary>
        /// <param name="listener">Method callback to invoke when event is raised.</param>
        public void Subscribe(Action<T> listener)
        {
            _onEventRaised += listener;
        }

        /// <summary>
        /// Unsubscribes a listener method from this event channel.
        /// </summary>
        /// <param name="listener">Method callback to remove.</param>
        public void Unsubscribe(Action<T> listener)
        {
            _onEventRaised -= listener;
        }
    }
}