using Cumulo.Aggregate;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Snapshoot
{
    public interface ISnapshootRepository<T, TKey>        
        where T : class, IAggregateRoot<TKey>
    {
        Task<T> GetSnapshootAsync(TKey key, CancellationToken cancellationToken);

        Task SaveSnapshoot(T snapshoot, CancellationToken cancellationToken);
    }
}
