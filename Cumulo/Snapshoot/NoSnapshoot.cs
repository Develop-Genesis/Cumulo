using Cumulo.Aggregate;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Snapshoot
{
    public class NoSnapshoot<T, TKey> : ISnapshootRepository<T, TKey>      
        where T : class, IAggregateRoot<TKey>
    {
        public Task<T> GetSnapshootAsync(TKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult<T>(null);
        }

        public Task SaveSnapshoot(T snapshoot, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
