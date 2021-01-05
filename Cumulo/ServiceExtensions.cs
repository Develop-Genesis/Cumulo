using EventStore.Client;
using EventStore.ClientAPI;
using Cumulo.Event;
using Cumulo.Repository;
using Cumulo.Snapshoot;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net.Http;

namespace Cumulo
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGskiInfrastructure(this IServiceCollection services)
        {
            services.AddScoped(typeof(ISnapshootRepository<,>), typeof(NoSnapshoot<,>));
            services.AddScoped(typeof(IEventStoreRepository<,>), typeof(EventStoreRepository<,>));

            return services;
        }

        public static IServiceCollection AddInMemorySnapshoot(this IServiceCollection services)
        {
            services.TryAddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            services.Replace(new ServiceDescriptor(typeof(ISnapshootRepository<,>), typeof(InMemorySnapshootRepository<,>), ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddDistributedSnapshoot(this IServiceCollection services)
        {
            services.Replace(new ServiceDescriptor(typeof(ISnapshootRepository<,>), typeof(DistributedSnapshootRepository<,>), ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddEventStoreDB(this IServiceCollection services, Action<EventStoreClientSettings> configureSettings = null)
        {
            services.AddEventStoreClient(configureSettings);
            services.AddScoped<IEventStore, EventStoreDbEventStore>();
            return services;
        }

        public static IServiceCollection AddEventStoreDB(this IServiceCollection services, string connectionString, Action<EventStoreClientSettings> configureSettings = null)
        {
            services.AddEventStoreClient(connectionString, configureSettings);
            services.AddScoped<IEventStore, EventStoreDbEventStore>();
            return services;
        }

        public static IServiceCollection AddEventStoreDB(this IServiceCollection services, Uri address, Func<HttpMessageHandler> createHttpMessageHandler = null)
        {
            services.AddEventStoreClient(address, createHttpMessageHandler);
            services.AddScoped<IEventStore, EventStoreDbEventStore>();
            return services;
        }

        public static IServiceCollection AddInMemoryEventStore(this IServiceCollection services)
        {            
            services.AddScoped<IEventStore, InMemoryEventStore>();
            return services;
        }

    }
}
