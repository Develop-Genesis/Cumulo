using Cumulo.Aggregate;
using Cumulo.Event;
using JsonNet.ContractResolvers;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Snapshoot
{
    public class DistributedSnapshootRepository<T, TKey> : ISnapshootRepository<T, TKey>
        where T : class, IAggregateRoot<TKey>
    {
        const string CacheKey = "eventstores";        
        string AggregateType { get; }

        IDistributedCache DistributedCache { get; }

        public DistributedSnapshootRepository(IDistributedCache distributedCache)
        {
            DistributedCache = distributedCache;
            AggregateType = AggregateRoot<TKey>.GetAggregateType<T>();
        }

        public async Task<T> GetSnapshootAsync(TKey key, CancellationToken cancellationToken)
        {
            var bytes = await DistributedCache.GetAsync(GenerateCacheKey(key), cancellationToken);

            if(bytes == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BsonDataReader(stream))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new PrivateSetterContractResolver();
                var snapshoot = serializer.Deserialize<T>(reader);
                return snapshoot;
            }
        }

        public async Task SaveSnapshoot(T snapshoot, CancellationToken cancellationToken)
        {
            // serialize data
            using (var ms = new MemoryStream())
            using (var writer = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new PrivateSetterContractResolver();
                serializer.Serialize(writer, snapshoot);
                var bytes = ms.ToArray();
                await DistributedCache.SetAsync(GenerateCacheKey(snapshoot.Id), bytes, cancellationToken);
            }
        }

        string GenerateCacheKey(TKey id)
        {
            return $"{CacheKey}_{AggregateType}_{id}";
        }

    }
}
