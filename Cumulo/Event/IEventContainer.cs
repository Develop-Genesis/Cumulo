using System;
using System.Collections.Generic;
using System.Text;

namespace Cumulo.Event
{
    public interface IEventsContainer
    {
        /// <summary>
        /// Get the domain events dispatched
        /// </summary>
        IEnumerable<IDomainEventDescriptor> DomainEvents { get; }

        /// <summary>
        /// Clear the events that have not been publish, this is usually called after publishing
        /// </summary>
        void NotifyEventsPublished();
    }
}
