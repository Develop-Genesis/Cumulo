using Cumulo.Event;
using System.Collections.Generic;

namespace Cumulo.Aggregate
{
    /// <summary>
    /// Aggregate root
    /// </summary>
    public interface IAggregateRoot<T> : IEventsContainer
    {
        /// <summary>
        /// Unique identifier of the aggregate root
        /// </summary>
        T Id { get; }

        /// <summary>
        /// Type of the aggregate
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Last version of the state of this aggregate root
        /// </summary>
        long Version { get; }

        /// <summary>
        /// Returns true if the current version of the aggregate is in the event store
        /// </summary>
        bool IsVersionSync { get; }

        /// <summary>
        /// Dispatch domain event
        /// </summary>
        /// <typeparam name="TEventData">Type of the event data</typeparam>
        /// <param name="eventData">event data</param>
        /// <param name="type">optional name for the type of the event if not provided the Clr name will be used</param>
        /// <param name="metadata">optional additional metadata to be saved with the event</param>
        void Dispatch<TEventData>(TEventData eventData, string type = null, IDictionary<string, string> metadata = null) where TEventData : class, IDomainEvent;

        /// <summary>
        /// Apply events previusly dispatched
        /// </summary>
        /// <param name="events">events previusly dispatched</param>
        void UpdateFromStore(IEnumerable<IDomainEventDescriptor> events);

        /// <summary>
        /// Return true if a snapshoot of the aggregate shoulb be save
        /// </summary>
        /// <returns></returns>
        bool ShouldTakeSnapshoot();
    }
}
