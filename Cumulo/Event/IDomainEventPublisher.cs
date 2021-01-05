using Cumulo.Event;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cumulo.Bus
{
    public interface IDomainEventPublisher
    {
        Task PublishDomainEventAsync(IDomainEventDescriptor @event, CancellationToken cancellationToken);
        Task PublishDomainEventsAsync(IEnumerable<IDomainEventDescriptor> events, CancellationToken cancellationToken);
    }
}
