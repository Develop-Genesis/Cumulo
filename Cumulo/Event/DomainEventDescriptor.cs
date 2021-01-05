using System;
using System.Collections.Generic;

namespace Cumulo.Event
{
    public class DomainEventDescriptor : IDomainEventDescriptor 
    {
        public static long UnknowVersion = long.MaxValue;

        public Guid EventId { get; }

        public string AggregateId { get; }

        public string AggregateType { get; }

        public string EventType { get; }

        public IDomainEvent Event { get; }

        public IDictionary<string, string> Metadata { get; }

        public bool IsPublished { get; }

        public long Version { get; }

        private DomainEventDescriptor(string aggregateType, string aggregateId, IDomainEvent @event, long version, string type = null, bool isPublished = false)
        {
            AggregateType = aggregateType;
            EventId = Guid.NewGuid();
            AggregateId = aggregateId;
            Event = @event;
            EventType = type ?? @event.GetType().Name;
            Metadata = new Dictionary<string, string>();
            Version = version;
            IsPublished = isPublished;
        }

        public static DomainEventDescriptor FromStore(string aggregateType, string aggregateId, IDomainEvent @event, long version, string type = null)
          => new DomainEventDescriptor(aggregateType, aggregateId, @event, version, type, true);
        

        public static DomainEventDescriptor CreateNew(string aggregateType, string aggregateId, IDomainEvent @event, long version, string type = null)
           => new DomainEventDescriptor(aggregateType, aggregateId, @event, version, type);

    }
}
