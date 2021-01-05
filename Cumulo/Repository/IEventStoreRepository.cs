using Cumulo.Aggregate;
using Cumulo.Event;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Repository
{
    public interface IEventStoreRepository<T, TKey> : IEventsContainer
        where T : class, IAggregateRoot<TKey>
    {
        void AddMany(IEnumerable<T> aggregates);
        void Add(T aggregate);
        void Remove(TKey aggregateId);
        void Remove(T aggregate);
        void Dettach(T aggregate);
        Task<T> GetAsync(TKey id, CancellationToken cancellationToken);
        Task<T> GetAsync(T snapshoot, CancellationToken cancellationToken);
    }
}
