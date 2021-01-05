using System;
using System.Collections.Generic;

namespace Cumulo.Event
{
    public interface IDomainEventDescriptor
    {        
        /// <summary>
        /// Unique identifier of the event
        /// </summary>
        Guid EventId { get; }
        /// <summary>
        /// Aggregate id that this event mutates
        /// </summary>
        string AggregateId { get; }
        /// <summary>
        /// Aggregate type
        /// </summary>
        string AggregateType { get; }
        /// <summary>
        /// Returns true if the event is in the Event Store
        /// </summary>
        bool IsPublished { get; }
        /// <summary>
        /// Version of the event
        /// </summary>
        long Version { get; }
        /// <summary>
        /// Type of event
        /// </summary>
        string EventType { get; }
        /// <summary>
        /// Event data
        /// </summary>
        IDomainEvent Event { get; }
        /// <summary>
        /// Metadata related with the event
        /// </summary>
        IDictionary<string, string> Metadata { get; }       
    }

}
