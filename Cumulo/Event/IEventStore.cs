using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Event
{
    public interface IEventStore : IDisposable
    {
        /// <summary>
        /// Add events to the store
        /// </summary>
        /// <param name="events">Events to be saved</param>
        /// <param name="cancellationToken">token to cancel request</param>
        /// <returns></returns>
        void TryAddEventContainer(IEventsContainer container);

        /// <summary>
        /// Remove event container from the store
        /// </summary>
        /// <param name="container"></param>
        void RemoveEventContainer(IEventsContainer container);

        /// <summary>
        /// Save events async
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get events for an specific aggregate
        /// </summary>
        /// <param name="aggregateType"></param>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken);

        /// <summary>
        /// Get events for specific aggregate after an specific version
        /// </summary>
        /// <param name="aggregateType"></param>
        /// <param name="id"></param>
        /// <param name="afterVersion"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, long afterVersion, CancellationToken cancellationToken);

        /// <summary>
        /// Get last version of aggregate
        /// </summary>
        /// <param name="aggregateType">the aggregate type</param>
        /// <param name="id">the aggregate unique identifier</param>
        /// <param name="cancellationToken">token to cancel request</param>
        /// <returns></returns>
        Task<long> GetAggregateLastVersionAsync<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken);

        /// <summary>
        /// React to store saves
        /// </summary>
        IObservable<IEnumerable<IDomainEventDescriptor>> AfterSave { get; }
    }
}
