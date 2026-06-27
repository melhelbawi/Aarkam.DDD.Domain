namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines a marker contract for CQRS projections responsible for synthesizing read-optimized data representations from immutable event streams.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> In a Command Query Responsibility Segregation (CQRS) or Event Sourced model, the write database is optimized 
/// exclusively for high-throughput state transitions and variant validation. Query paths require denormalized, pre-computed data structures 
/// optimized for specific user interface panels or analytical views. Projections continuously ingest historical facts (<see cref="IDomainEvent"/>) 
/// and transform them into flat read models.
/// </para>
/// <para>
/// <b>STATELESS ARCHITECTURE NOTE:</b> Concrete implementations of this interface must remain entirely stateless. Values such as <see cref="Position"/> 
/// and <see cref="LastProcessedAt"/> are monitored by background consumer pipelines and stored inside infrastructure checkpoint tables. 
/// They do not represent in-memory variables of the projection class instance itself.
/// </para>
/// <para>
/// <b>PROJECTION STRATEGIES:</b>
/// <list type="bullet">
///   <item><description><b>Inline/Synchronous:</b> Executed inside the same database transaction boundary as the Command. Offers immediate consistency but introduces latency penalties to write paths.</description></item>
///   <item><description><b>Asynchronous (Recommended):</b> Executed out-of-band by a background log tailing processor. Provides high throughput and structural resilience via eventual consistency.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IDomainEvent"/>
public interface IProjection
{
    /// <summary>
    /// Gets a unique alphanumeric taxonomy name identifying this specific projection engine instance within the cluster namespace.
    /// </summary>
    /// <remarks>
    /// Used by the framework pipeline to route event distribution tracking, map telemetry logs, and register check-point indices.
    /// </remarks>
    /// <example>"OrderSummaryProjection", "CustomerBillingDashboardProjection"</example>
    string ProjectionName { get; }

    /// <summary>
    /// Gets the current structural checkpoint index sequence number processed by this projection pipeline.
    /// </summary>
    /// <value>A global coordinate increment tracker matching the highest high-water mark recorded inside the event log.</value>
    long Position { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when the projection pipeline last committed an incremental stream write.
    /// </summary>
    DateTimeOffset? LastProcessedAt { get; }

    /// <summary>
    /// Triggers an administrative purge routine resetting the read tables and high-water mark checkpoints associated with this projection.
    /// </summary>
    /// <param name="cancellationToken">A system utility flag propagated to notify background thread disposal commands.</param>
    /// <returns>A tracking asynchronous promise task completion indicator.</returns>
    /// <remarks>
    /// <b>WHY RESET PROJECTIONS?</b> One of the primary advantages of event-driven architectures is the ability to throw away a corrupted 
    /// or legacy read table, update the C# projection schema logic, and replay the entire history of the application from time index zero 
    /// to reconstruct a pristine state.
    /// </remarks>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a strongly-typed contract for an event handler responsible for feeding an incremental update loop for a specific domain event type.
/// </summary>
/// <typeparam name="TEvent">The target event blueprint implementing the <see cref="IDomainEvent"/> interface specifications.</typeparam>
/// <seealso cref="IProjection"/>
public interface IProjection<in TEvent> : IProjection
    where TEvent : class, IDomainEvent
{
    /// <summary>
    /// Appends or alters denormalized read records using parameters supplied inside the provided domain event snapshot.
    /// </summary>
    /// <param name="event">The immutable transaction fact model being processed.</param>
    /// <param name="cancellationToken">A system utility flag propagated to notify background thread disposal commands.</param>
    /// <returns>A tracking asynchronous promise task completion indicator.</returns>
    /// <remarks>
    /// <b>ERROR HANDLING BEST PRACTICE:</b> If an exception occurs inside this execution block during an asynchronous replay loop, 
    /// the pipeline must halt and log a critical warning rather than dropping the message. Event streaming requires strict, unbroken order; 
    /// skipping a single mutation creates permanent historical data corruption inside the target read view model.
    /// </remarks>
    Task ApplyAsync(TEvent @event, CancellationToken cancellationToken = default);
}