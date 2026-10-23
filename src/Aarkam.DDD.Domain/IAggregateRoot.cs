using Aarkam.DDD.Domain;
using Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines the core contract for an Aggregate Root within a Domain-Driven Design (DDD) model supporting Event Sourcing.
/// </summary>
/// <remarks>
/// <para>
/// An Aggregate Root acts as the primary gatekeeper, transactional consistency boundary, and operational wrapper 
/// for a cluster of associated domain entities and value objects. All external interactions with the aggregate 
/// graph must navigate directly through this root boundary.
/// </para>
/// <para>
/// This contract specifically models an <b>Event-Sourced Aggregate Root</b>, which shifts the responsibility of 
/// data state tracking from current-snapshot delta maps to an ordered append-only sequence of immutable 
/// <see cref="IDomainEvent"/> streams.
/// </para>
/// </remarks>
public interface IAggregateRoot : IDeletionAuditableEntity, IHasExtraProperties, IHasDomainEvents
{
    /// <summary>
    /// Gets the current structural version sequence of the aggregate instance.
    /// </summary>
    /// <value>
    /// An integer representing the version, starting at <c>-1</c> or <c>0</c> for unpersisted initial instances, 
    /// incrementing strictly by <c>1</c> per applied domain event. Used as a safeguard for optimistic concurrency checks.
    /// </value>
    int Version { get; }

    /// <summary>
    /// Applies an immutable domain event to mutate the aggregate's internal state configurations.
    /// </summary>
    /// <param name="event">The domain event specifying a discrete historical business mutation.</param>
    /// <remarks>
    /// This method is dual-purpose:
    /// <list type="bullet">
    /// <item>Replays historical database events sequentially during aggregate reconstitution via <see cref="LoadFromHistory"/>.</item>
    /// <item>Tracks and emits newly raised operational modifications triggered by contemporary command execution within the boundary.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is null.</exception>
    void Apply(IDomainEvent @event);

    /// <summary>
    /// Evaluates a unique text-based classification descriptor identifying the concrete aggregate type boundary.
    /// </summary>
    /// <returns>A string representation of the concrete class domain identifier (e.g., "Order", "InventoryItem").</returns>
    /// <remarks>
    /// Typically mapped to the runtime reflection metadata name. This value binds events to their respective 
    /// aggregate streams inside multi-tenant event stores.
    /// </remarks>
    string GetAggregateType();

    /// <summary>
    /// Reconstitutes the internal state of the aggregate root by systematically replaying an ordered chronological sequence of historical events.
    /// </summary>
    /// <param name="history">The sequential chronological record of past domain events retrieved from the event store.</param>
    /// <remarks>
    /// This represents the primary infrastructure mechanism used to rehydrate or materialize an aggregate state from storage history.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="history"/> collection reference is null.</exception>
    void LoadFromHistory(IEnumerable<IDomainEvent> history);
}

/// <summary>
/// Defines the strongly-typed generic contract for an Aggregate Root, seamlessly unifying identity with behavioral contracts.
/// </summary>
/// <typeparam name="TKey">The identity type mapping back to the underlying <see cref="IEntity{TKey}"/> contract.</typeparam>
public interface IAggregateRoot<out TKey> : IAggregateRoot, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
}
