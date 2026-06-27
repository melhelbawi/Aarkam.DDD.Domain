
namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the core structural contract for all Domain Events within the Domain-Driven Design ecosystem.
/// </summary>
/// <remarks>
/// <b>DESIGN PHILOSOPHY:</b> A Domain Event represents an immutable, historical statement about something notable 
/// that has already occurred within the domain boundary. It contains a snapshot of data explaining the event 
/// and serves as the primary driver for both Event Sourcing state transitions and Outbox/Pub-Sub messaging.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the globally unique identifier for this specific event message instance.
    /// </summary>
    /// <value>A <see cref="Guid"/> used uniformly by message brokers and event stores to enforce idempotency and message deduplication.</value>
    Guid EventId { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when the business event was finalized by the aggregate root.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the unique text classification name of the Aggregate Root that produced this event.
    /// </summary>
    /// <example>"Order", "InventoryItem", "Customer"</example>
    string AggregateType { get; }

    /// <summary>
    /// Gets the string representation of the target aggregate instance identity.
    /// </summary>
    string AggregateId { get; }

    /// <summary>
    /// Gets the sequence version index of the aggregate root at the precise moment this event was committed.
    /// </summary>
    /// <value>Used by the infrastructure to reconstruct state history sequentially and assert optimistic concurrency limits.</value>
    int Version { get; }
}

/// <summary>
/// A non-generic base implementation providing common tracking metadata and initialization patterns for domain events.
/// </summary>
/// <remarks>
/// <b>WHY USE THIS?</b> Provides a clean foundation for basic events where the aggregate typing is handled manually 
/// or evaluated dynamically during processing cycles.
/// </remarks>
public abstract class DomainEventBase : IDomainEvent
{
    /// <inheritdoc />
    /// <value>The protected setter allows infrastructure serializers to rehydrate the state during event log replay loops.</value>
    public Guid EventId { get; protected set; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; protected set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public virtual string AggregateType { get; protected set; } = null!;

    /// <inheritdoc />
    public virtual string AggregateId { get; protected set; } = null!;

    /// <inheritdoc />
    public virtual int Version { get; protected set; }

    /// <summary>
    /// Parameterless constructor enabling reflection-based deserialization.
    /// </summary>
    protected DomainEventBase() { }

    /// <summary>
    /// Initializes an operational event state transaction instance.
    /// </summary>
    /// <param name="aggregateType">The class type identity of the generating aggregate root boundary.</param>
    /// <param name="aggregateId">The localized tracking key identifying the aggregate stream instance.</param>
    protected DomainEventBase(string aggregateType, string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("Aggregate taxonomy classification is required.", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(aggregateId))
            throw new ArgumentException("Aggregate identifier context string is required.", nameof(aggregateId));

        AggregateType = aggregateType;
        AggregateId = aggregateId;
    }

    /// <summary>
    /// Initializes an explicit historical event snapshot model, typically leveraged by event store infrastructure builders.
    /// </summary>
    /// <param name="aggregateType">The class type identity of the generating aggregate root boundary.</param>
    /// <param name="aggregateId">The localized tracking key identifying the aggregate stream instance.</param>
    /// <param name="occurredOn">The exact historical UTC timeline timestamp.</param>
    /// <param name="eventId">The persistent identifier assigned to this unique message record.</param>
    protected DomainEventBase(string aggregateType, string aggregateId, DateTimeOffset occurredOn, Guid eventId)
        : this(aggregateType, aggregateId)
    {
        OccurredOn = occurredOn;
        EventId = eventId;
    }
}

/// <summary>
/// A generic base class providing strong typing context to the originating Aggregate Root framework.
/// </summary>
/// <typeparam name="TAggregate">The class type blueprint of the aggregate boundary generating this event signature.</typeparam>
/// <typeparam name="TKey">The primary key structure binding the entity context tracking rules.</typeparam>
/// <remarks>
/// <b>WHY GO GENERIC?</b> This variant reduces code redundancy by automatically deriving the <see cref="AggregateType"/> 
/// metadata name via compilation reflection (<c>typeof(TAggregate).Name</c>). This enforces design clarity across concrete implementations.
/// </remarks>
public abstract class DomainEvent<TAggregate, TKey> : DomainEventBase
    where TAggregate : AggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Automatically overrides taxonomy descriptors by binding directly to the compiled class name of the target domain graph root.
    /// </summary>
    public override string AggregateType => typeof(TAggregate).Name;

    /// <summary>
    /// Parameterless constructor enabling reflection-based infrastructure compilation.
    /// </summary>
    protected DomainEvent() : base() { }

    /// <summary>
    /// Initializes a typed operational domain event instance.
    /// </summary>
    /// <param name="aggregateId">The structural string conversion string of the aggregate identity key.</param>
    /// <param name="version">The targeted modification event sequence tracker index.</param>
    protected DomainEvent(string aggregateId, int version) : base(typeof(TAggregate).Name, aggregateId)
    {
        Version = version;
    }
}

/// <summary>
/// A convenient shortcut base class for generic domain events referencing standard globally unique system identifiers (<see cref="Guid"/>).
/// </summary>
public abstract class DomainEvent<TAggregate> : DomainEvent<TAggregate, Guid>
    where TAggregate : AggregateRoot<Guid>
{
    protected DomainEvent() : base() { }
    protected DomainEvent(string aggregateId, int version) : base(aggregateId, version) { }
}

/// <summary>
/// A structural marker interface intended for reflection-driven assembly parsing, IoC automation, and handler filtering.
/// </summary>
public interface IDomainEventHandler
{
}

/// <summary>
/// Defines the asynchronous execution boundary responsible for reacting to a specific event signature.
/// </summary>
/// <typeparam name="TEvent">The target message contract implementing the <see cref="IDomainEvent"/> model.</typeparam>
/// <remarks>
/// <b>DDD BEST PRACTICE:</b> Domain Event Handlers must remain completely disconnected from the execution context of the 
/// original aggregate root business action. They execute in distinct, isolated transactions (often mediated by MediatR, CAP, or ABP EventBus) 
/// to manage side effects, update read-model cache stores, or trigger integration events across distributed microservices.
/// </remarks>
public interface IEventHandler<in TEvent> : IDomainEventHandler
    where TEvent : class, IDomainEvent
{
    /// <summary>
    /// Invokes the asynchronous execution loop processing the designated historical domain record.
    /// </summary>
    /// <param name="domainEvent">The target transaction message context to read.</param>
    /// <param name="cancellationToken">A system utility flag propagated to notify background thread disposal commands.</param>
    /// <returns>A tracking asynchronous promise task.</returns>
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}