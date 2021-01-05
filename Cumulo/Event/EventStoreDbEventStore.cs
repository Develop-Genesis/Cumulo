using EventStore.Client;
using Cumulo.Bus;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static EventStore.Client.EventStoreClient;

namespace Cumulo.Event
{
    public class EventStoreDbEventStore : EventStore
    {
        EventStoreClient EventStoreClient { get; }
        
        public EventStoreDbEventStore(EventStoreClient eventStoreClient, IDomainEventPublisher bus) : base(bus)
        {
            EventStoreClient = eventStoreClient;            
        }

        public override async IAsyncEnumerable<IDomainEventDescriptor> GetAggregateEvents<TKey>(string aggregateType, TKey id, long afterVersion, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var streamName = GenerateStreamId(aggregateType, id);

            ReadStreamResult eventStoreDbEvents;

            if (afterVersion == -1)
            {
                eventStoreDbEvents = EventStoreClient.ReadStreamAsync(
                    Direction.Forwards,
                    streamName,
                    StreamPosition.Start,
                    cancellationToken: cancellationToken);
            }
            else
            {
                eventStoreDbEvents = EventStoreClient.ReadStreamAsync(
                    Direction.Forwards,
                    streamName,
                    StreamPosition.FromInt64(afterVersion + 1),
                    cancellationToken: cancellationToken);
            }

            if (await eventStoreDbEvents.ReadState == ReadState.StreamNotFound)
            {
                yield break;
            }

            var eventDescriptors = eventStoreDbEvents.Select(@event =>
            {
                var version = @event.Event.EventNumber.ToInt64();
                var bsonData = @event.Event.Data;
                var bsonMetadata = @event.Event.Metadata;
                var eventType = @event.Event.EventType;

                DomainEventDescriptor descriptor;

                using (var stream = bsonData.AsStream())
                using (var reader = new BsonDataReader(stream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.All;
                    var domainEvent = serializer.Deserialize<IDomainEvent>(reader);
                    descriptor = DomainEventDescriptor.FromStore(aggregateType, id.ToString(), domainEvent, version, eventType);
                }

                using (var stream = bsonMetadata.AsStream())
                using (var reader = new BsonDataReader(stream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.All;
                    var metadata = serializer.Deserialize<IDictionary<string, string>>(reader);

                    foreach (var keyValue in metadata)
                    {
                        descriptor.Metadata.Add(keyValue.Key, keyValue.Value);
                    }
                }

                return descriptor;
            });

            await foreach (var eventDescriptor in eventDescriptors)
            {
                yield return eventDescriptor;
            }
        }

        public override async Task<long> GetAggregateLastVersionAsync<TKey>(string aggregateType, TKey id, CancellationToken cancellationToken)
        {
            var streamName = GenerateStreamId(aggregateType, id);
            var result = EventStoreClient.ReadStreamAsync(
                    Direction.Backwards,
                    streamName,
                    StreamPosition.End,
                    maxCount: 1,
                    cancellationToken: cancellationToken);

            if (await result.ReadState == ReadState.StreamNotFound)
            {
                return 0L;
            }

            var lastEvent = await result.FirstAsync(cancellationToken);

            return lastEvent.Event.EventNumber.ToInt64();
        }

        protected override async Task WriteEventsToStoreAsync(IEnumerable<IDomainEventDescriptor> events, CancellationToken cancellationToken)
        {
            var aggregateGroup = events.GroupBy(x => GenerateStreamId(x.AggregateType, x.AggregateId));

            var eventDataGroups = aggregateGroup.Select(group =>
            {
                var eventList = group.ToList();

                var eventDatas = eventList.Select(x =>
                {
                    byte[] data;
                    byte[] metadata;

                    // serialize data
                    using (var ms = new MemoryStream())
                    using (var writer = new BsonDataWriter(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.TypeNameHandling = TypeNameHandling.All;
                        serializer.Serialize(writer, x.Event);
                        data = ms.ToArray();
                    }

                    // serialize metadata
                    using (var ms = new MemoryStream())
                    using (var writer = new BsonDataWriter(ms))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(writer, x.Metadata);
                        metadata = ms.ToArray();
                    }

                    return new EventData(
                            Uuid.FromGuid(x.EventId),
                            x.EventType,
                            data,
                            metadata,
                            "application/octet-stream"
                        );
                });

                if (eventList.Any(x => x.IsPublished))
                {
                    throw new InvalidOperationException("Cannot publish events that have been published");
                }

                var firstEvent = eventList.First();

                return (StreamId: group.Key, EventDatas: eventDatas, ExpectedVersionBeforeAppend: firstEvent.Version - 1L);
            });

            foreach (var group in eventDataGroups)
            {
                // expect no stream when there is not positive version
                if (group.ExpectedVersionBeforeAppend == -1)
                {
                    await EventStoreClient.AppendToStreamAsync(
                               group.StreamId,
                               StreamState.NoStream,
                               group.EventDatas,
                               cancellationToken: cancellationToken
                              );
                }
                // expect stream when there an unknow version
                else if (group.ExpectedVersionBeforeAppend == DomainEventDescriptor.UnknowVersion - 1)
                {
                    await EventStoreClient.AppendToStreamAsync(
                               group.StreamId,
                               StreamState.StreamExists,
                               group.EventDatas,
                               cancellationToken: cancellationToken
                              );
                }
                // expect stream when there is a valid version
                else
                {
                    await EventStoreClient.AppendToStreamAsync(
                               group.StreamId,
                               StreamRevision.FromInt64(group.ExpectedVersionBeforeAppend),
                               group.EventDatas,
                               cancellationToken: cancellationToken
                              );
                }
            }

        }

    }
}
