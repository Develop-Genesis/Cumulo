using Cumulo.Aggregate;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Snapshoot
{
    public class InMemorySnapshootRepository<T, TKey> : ISnapshootRepository<T, TKey> 
        where T : class, IAggregateRoot<TKey>
    {
        const string CacheKey = "eventstores";
        IMemoryCache MemoryCache { get; }
        string AggregateType { get; }

        public InMemorySnapshootRepository(IMemoryCache memoryCache)
        {
            MemoryCache = memoryCache;
            AggregateType = AggregateRoot<TKey>.GetAggregateType<T>();
        }

        public Task<T> GetSnapshootAsync(TKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(MemoryCache.Get<T>(GenerateCacheKey(key)));
        }

        public Task SaveSnapshoot(T snapshoot, CancellationToken cancellationToken)
        {
            MemoryCache.Set(GenerateCacheKey(snapshoot.Id), snapshoot);
            return Task.CompletedTask;
        }

        string GenerateCacheKey(TKey id)
        {
            return $"{CacheKey}_{AggregateType}_{id}";
        }

    }
}
