# Cumulo
Event sourcing infrastructure library for .Net


## Getting Started

````cs

 services
    .AddGskiInfrastructure()
    // in memory event store only for testing
    .AddInMemoryEventStore();
    // optional snapshooting is supported
    .AddInMemorySnapshoot();
    // .AddDistributedSnapshoot() -- for distributed cache    

````

## Creating your aggregates 

````cs

    public class UserDomain : AggregateRoot<Guid>
    {
        const string DType = "User";        
        
        public string FirstName { get; private set; }
        public string LastName { get; private set; }

        public UserDomain() : base(DType, Guid.Empty){}

        public UserDomain(Guid id, IEnumerable<IDomainEventDescriptor> events) : base(Type, id, events) {}

        public UserDomain(string firstName, string lastName) : base(DType, Guid.NewGuid())
        {            
            Dispatch(new CreateUserEvent(){ FirstName = firstName, LastName = lastName });
        }

        public void SetFirstName(string firstName)
        {
            Dispatch(UpdateFirstNameEvent(){ FirstName = firstName });
        }

        public void SetLastName(string lastName)
        {
            Dispatch(UpdateLastNameEvent(){ LastName = lastName });
        }

        // handlers
        void On(CreateUserEvent @event)
        {
            FirstName = @event.FirstName;
            LastName = @event.LastName;
        }

        void On(UpdateFirstNameEvent @event)
        {
            FirstName = @event.FirstName;            
        } 

        void On(UpdateLastNameEvent @event)
        {            
            LastName = @event.LastName;
        }
        
    }

    // Domain Events
    public class CreateUserEvent : ICreateAggregateDomainEvent
    {
        public string FirstName{ get; set; }
        public string LastName{ get; set; }
    }

    public class UpdateFirstNameEvent : IDomainEvent
    {
        public string FirstName{ get; se; }
    }

    public class UpdateLastNameEvent : IDomainEvent
    {
        public string LastName{ get; se; }
    }

````

### Get Repository
````cs
    
    public MyCommandContructor(IEventStoreRepository<UserDomain, Guid> repository)
    {
        ...
    }

````

### Add
````cs
    
    var user = new UserDomain("Alejandro", "Guardiola");

    repo.Add(user);

````

### Get
````cs
    
    var user = await repo.GetAsync(userId);

````

### Update
````cs
    
    var user = await repo.GetAsync(userId);
    user.SetFirstName("Jorge");

````

### Remove
````cs
    repo.Remove(userId);
    // or
    repo.Remove(user);
````

### Save Changes
````cs
    // Inject IEventStore in the constructor
    await eventStore.SaveAsync();
````