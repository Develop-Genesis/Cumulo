using Cumulo.Aggregate;
using Cumulo.Event;
using Cumulo.Exceptions;
using Cumulo.Snapshoot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Repository
{
    public class EventStoreRepository<T, TKey> : IEventStoreRepository<T, TKey> 
        where T : class, IAggregateRoot<TKey>
    {
        string AggregateType { get; }
        IEventStore EventStore { get; }
        ISnapshootRepository<T, TKey> SnapshootRepository { get; }

        ConcurrentDictionary<TKey, T> Aggregates = new ConcurrentDictionary<TKey, T>();

        private List<IDomainEventDescriptor> _domainEvents = new List<IDomainEventDescriptor>();
        public IEnumerable<IDomainEventDescriptor> DomainEvents => _domainEvents.AsEnumerable();

        public EventStoreRepository(IEventStore eventStore, ISnapshootRepository<T, TKey> snapshootRepository)
        {
            EventStore = eventStore;
            EventStore.TryAddEventContainer(this);
            AggregateType = AggregateRoot<TKey>.GetAggregateType<T>();
            SnapshootRepository = snapshootRepository;
            EventStore.AfterSave
                .Select(x => Observable.FromAsync(() => AfterSaveAsync(x)))
                .Concat()
                .Subscribe();
        }

        async Task AfterSaveAsync(IEnumerable<IDomainEventDescriptor> domainEvents)
        {
            foreach(var aggregate in Aggregates.Values.Where(x => x.IsVersionSync).Where(x => x.ShouldTakeSnapshoot()))
            {
                await SnapshootRepository.SaveSnapshoot(aggregate, default);
            }
        }

        /// <summary>
        /// Add new aggregate
        /// </summary>
        /// <param name="aggregate"></param>
        public void Add(T aggregate)
        {
            Aggregates.TryAdd(aggregate.Id, aggregate);
            EventStore.TryAddEventContainer(aggregate);
        }

        /// <summary>
        /// Add many new aggregates
        /// </summary>
        /// <param name="aggregates"></param>
        public void AddMany(IEnumerable<T> aggregates)
        {
            foreach(var aggregate in aggregates)
            {
                Add(aggregate);
            }
        }

        /// <summary>
        /// Dettach aggregate from the events tracking. This will ignore all events in the aggregate when saving to the event store
        /// </summary>
        /// <param name="aggregate"></param>
        public void Dettach(T aggregate)
        {
            EventStore.RemoveEventContainer(aggregate);
            Aggregates.TryRemove(aggregate.Id, out _);
        }

        public async Task<T> GetAsync(TKey id, CancellationToken cancellationToken)
        {
            if(Aggregates.ContainsKey(id))
            {
                var aggregate = Aggregates[id];
                if(!aggregate.IsVersionSync)
                {
                    return aggregate;
                }
                
                return await GetAsync(aggregate, cancellationToken);
            }

            var snapshoot = await SnapshootRepository.GetSnapshootAsync(id, cancellationToken);
            if(snapshoot != null)
            {
                return await GetAsync(snapshoot, cancellationToken);
            }

            var events = await EventStore.GetAggregateEvents(AggregateType, id, cancellationToken).ToListAsync(cancellationToken);
            var instance = Activator.CreateInstance(typeof(T), new object[] { id, events }) as T;

            Aggregates[id] = instance;
            EventStore.TryAddEventContainer(instance);

            return instance;
        }

        public async Task<T> GetAsync(T snapshoot, CancellationToken cancellationToken)
        {
            if (!snapshoot.IsVersionSync)
                throw new VersionNotAccurateException();

            var snapshootVersion = snapshoot.Version;
            var lastVersion = await EventStore.GetAggregateLastVersionAsync(AggregateType, snapshoot.Id, cancellationToken);

            if(snapshootVersion < lastVersion)
            {
                var events = await EventStore.GetAggregateEvents(AggregateType, snapshoot.Id, snapshootVersion, cancellationToken).ToListAsync(cancellationToken);
                snapshoot.UpdateFromStore(events);
            }

            Aggregates.TryAdd(snapshoot.Id, snapshoot);
            EventStore.TryAddEventContainer(snapshoot);

            return snapshoot;
        }

        /// <summary>
        /// Mark aggregate as deleted
        /// </summary>
        /// <param name="aggregate"></param>
        public void Remove(T aggregate)
        {
            Remove(aggregate.Id);
            EventStore.RemoveEventContainer(aggregate);
        }

        /// <summary>
        /// Mark aggregate as deleted
        /// </summary>
        /// <param name="aggregate"></param>
        public void Remove(TKey aggregateId)
        {
            var removeEvent = DomainEventDescriptor.CreateNew(AggregateType, aggregateId.ToString(), new RemoveAggregateRootEvent(aggregateId.ToString()), DomainEventDescriptor.UnknowVersion);
            _domainEvents.Add(removeEvent);
        }

        public void NotifyEventsPublished()
        {
            _domainEvents.Clear();
        }
    }
}
