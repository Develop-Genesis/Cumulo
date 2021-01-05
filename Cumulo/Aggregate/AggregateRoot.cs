using Cumulo.Event;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cumulo.Aggregate
{
    /// <summary>
    /// Generic implementation of an aggregate root
    /// </summary>
    public abstract class AggregateRoot<T> : IAggregateRoot<T>
    {
        private List<IDomainEventDescriptor> _domainEvents = new List<IDomainEventDescriptor>();
                
        public T Id { get; private set; }

        public long Version { get; private set; } = -1;

        [JsonIgnore]
        public IEnumerable<IDomainEventDescriptor> DomainEvents => _domainEvents.AsEnumerable();

        [JsonIgnore]
        public bool IsVersionSync { get; private set; } = true;

        [JsonIgnore]
        public string Type { get; }

        public AggregateRoot(string type, T id, IEnumerable<IDomainEventDescriptor> events) : this(type, id)
        {            
            UpdateFromStore(events);
        }

        public AggregateRoot(string type, T id)
        {
            Id = id;
            Type = type == null ? GetType().Name : type;
        }

        public void Dispatch<TEventData>(TEventData eventData, string type = null, IDictionary<string, string> metadata = null) where TEventData : class, IDomainEvent
        {            
            var domainEvent = DomainEventDescriptor.CreateNew(Type, Id.ToString(), eventData, ++Version, type);

            if(metadata != null)
            {
                foreach(var keyValue in metadata)
                {
                    metadata.Add(keyValue);
                }
            }

            OnEvent(eventData);

            _domainEvents.Add(domainEvent);
            IsVersionSync = false;            
        }

        public void UpdateFromStore(IEnumerable<IDomainEventDescriptor> events)
        {
            if(!IsVersionSync)
            {
                throw new InvalidOperationException("Cannot update aggregate that is not sync the Event Store");
            }

            foreach (var @event in events)
            {
                Version = @event.Version;
                if (!@event.IsPublished)
                {
                    throw new InvalidOperationException($"Event with id {@event.EventId} of type {@event.EventType} is not in the event store");
                }               

                OnEvent(@event.Event);
            }
        }

        public virtual bool ShouldTakeSnapshoot()
        {
            return Version % 10 == 0;
        }

        protected virtual void OnEvent(IDomainEvent @event)
        {
            var type = GetType();

            var domainEventType = @event.GetType();

            var eventMethod =
                type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "On")
                .Where(x => x.GetParameters().Length == 1)
                .FirstOrDefault(x => x.GetParameters().First().ParameterType.Equals(domainEventType));

            if(eventMethod == null)
            {
                return;
            }

            eventMethod.Invoke(this, new object[] { @event });
        }

        public void NotifyEventsPublished()
        {
            IsVersionSync = true;
            _domainEvents.Clear();
        }

        public static string GetAggregateType<A>() where A : IAggregateRoot<T>
        {
            var aggregateInstance = Activator.CreateInstance<A>();
            return aggregateInstance.Type;
        }

    }
}
