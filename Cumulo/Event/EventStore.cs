using Cumulo.Bus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Event
{
    public abstract class EventStore : IEventStore
    {        
        IDomainEventPublisher Bus { get; }

        Subject<IEnumerable<IDomainEventDescriptor>> _afterSave = new Subject<IEnumerable<IDomainEventDescriptor>>();
        public IObservable<IEnumerable<IDomainEventDescriptor>> AfterSave => _afterSave;

        ConcurrentDictionary<IEventsContainer, byte> _eventsContainers = new ConcurrentDictionary<IEventsContainer, byte>();

        public EventStore(IDomainEventPublisher bus)
        {            
            Bus = bus;
        }

        public void TryAddEventContainer(IEventsContainer container)
        {
            _eventsContainers.TryAdd(container, 1);
        }

        public IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken)
            => GetAggregateEvents(aggregateType, id, -1, cancellationToken);

        public abstract IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, long afterVersion, CancellationToken cancellationToken);

        public abstract Task<long> GetAggregateLastVersionAsync<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken);        

        public void RemoveEventContainer(IEventsContainer container)
        {
            _eventsContainers.Remove(container, out _);
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            var events = _eventsContainers.Keys.SelectMany(x => x.DomainEvents).ToList();
            while (events.Count > 0)
            {
                // store events and publish them                
                await WriteEventsToStoreAsync(events, cancellationToken);

                foreach (var container in _eventsContainers.Keys)
                {
                    container.NotifyEventsPublished();
                }

                if(Bus != null)
                {
                    await Bus.PublishDomainEventsAsync(events, cancellationToken);
                }
                
                events = _eventsContainers.Keys.SelectMany(x => x.DomainEvents).ToList();
            }

            _afterSave.OnNext(events);
        }

        protected abstract Task WriteEventsToStoreAsync(IEnumerable<IDomainEventDescriptor> events, CancellationToken cancellationToken);
                
        public static string GenerateStreamId<TKey>(string aggregateType, TKey id)
        {
            return $"{aggregateType}-{id}";
        }

        public void Dispose()
        {
            _afterSave.OnCompleted();
            _afterSave.Dispose();
        }
    }
}
