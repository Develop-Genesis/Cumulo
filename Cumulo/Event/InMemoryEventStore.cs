using Cumulo.Bus;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Event
{
    public class InMemoryEventStore : EventStore
    {
        static ConcurrentDictionary<string, ConcurrentDictionary<string, List<IDomainEventDescriptor>>> _store = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<IDomainEventDescriptor>>>();

        public InMemoryEventStore(IDomainEventPublisher bus) : base(bus)
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async override IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, long afterVersion, [EnumeratorCancellation]CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var stream = _store.GetOrAdd(aggregateType, (t) => new ConcurrentDictionary<string, List<IDomainEventDescriptor>>());
            var events = stream.GetOrAdd(id.ToString(), (k) => new List<IDomainEventDescriptor>());                       

            foreach( var @event in events.Skip((int)afterVersion + 1) )
            {
                yield return @event;
            }
        }

        public async override Task<long> GetAggregateLastVersionAsync<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken)
        {
            var events = GetAggregateEvents(aggregateType, id, cancellationToken);
            return await events.CountAsync();
        }

        protected override Task WriteEventsToStoreAsync(IEnumerable<IDomainEventDescriptor> events, CancellationToken cancellationToken)
        {
            foreach (var @event in events)
            {
                var streams = _store.GetOrAdd(@event.AggregateType, new ConcurrentDictionary<string, List<IDomainEventDescriptor>>());
                var eventsList = streams.GetOrAdd(@event.AggregateId, new List<IDomainEventDescriptor>());

                var publishedEvent = DomainEventDescriptor.FromStore(@event.AggregateType, @event.AggregateId, @event.Event, @event.Version, @event.EventType);

                eventsList.Add(publishedEvent);
            }

            return Task.CompletedTask;
        }
    }
}
