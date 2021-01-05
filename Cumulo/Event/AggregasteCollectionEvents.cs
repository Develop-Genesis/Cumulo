



namespace Cumulo.Event
{
    public interface ICreateAggregateDomainEvent : IDomainEvent
    {

    }

    public class RemoveAggregateRootEvent : IDomainEvent
    {
        public string Id { get; private set; }

        public RemoveAggregateRootEvent(string id)
        {
            Id = id;
        }
    }
}
