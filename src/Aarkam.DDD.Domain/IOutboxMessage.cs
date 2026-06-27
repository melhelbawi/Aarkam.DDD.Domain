namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the structural contract for an Outbox Message record enabling guaranteed, resilient event distribution using the Transactional Outbox pattern.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Modifying a domain aggregate's database state and immediately publishing an event directly over the network 
/// to a message broker introduces a critical **Distributed Transaction Risk**. If the database transaction commits successfully but the 
/// message broker network link fails, downstream consumers remain completely unaware of the mutation. 
/// </para>
/// <para>
/// This contract resolves the Dual-Write dilemma by persisting the serialization payload directly into the exact same local database 
/// transaction boundary hosting your aggregate state transitions.
/// </para>
/// <para>
/// <b>TRANSACTIONAL DISPATCH SEQUENCE:</b>
/// <list type="number">
///   <item><description>The business domain operation mutates state data and queues staging event metadata inside the aggregate graph.</description></item>
///   <item><description>The unit of work interceptor intercepts the events, maps them into an <see cref="IOutboxMessage"/>, and writes them to local storage.</description></item>
///   <item><description>An independent background worker task polls the table for uncompleted messages where <see cref="ProcessedAt"/> remains null.</description></item>
///   <item><description>The worker publishes the payload to the external broker, and marks completion by invoking <see cref="MarkAsProcessed"/>.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IIntegrationEvent"/>
/// <seealso cref="IInboxMessage"/>
public interface IOutboxMessage
{
    /// <summary>
    /// Gets the unique sequence identity key of the outbox envelope tracking record.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the fully qualified assembly-qualified C# class string name of the target message type payload.
    /// </summary>
    /// <remarks>
    /// Leveraged by background worker dispatchers to safely instantiate and deserialize the string <see cref="Payload"/> 
    /// back into its concrete object type wrapper before pushing it over the wire.
    /// </remarks>
    /// <example>"Aarkam.Ordering.IntegrationEvents.OrderPaidIntegrationEvent"</example>
    string EventType { get; }

    /// <summary>
    /// Gets the text-based structured representation containing the comprehensive state data of the event.
    /// </summary>
    /// <value>A self-contained string, typically formatted as compressed JSON, representing the serialized message model.</value>
    string Payload { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when this outbox envelope was written to local database storage.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when the background worker successfully acknowledged distribution to the external broker.
    /// </summary>
    /// <value>Returns a valid timestamp if processing finalized; otherwise, <see langword="null"/> indicates a pending state transaction.</value>
    DateTimeOffset? ProcessedAt { get; }

    /// <summary>
    /// Gets the cumulative integer value tracking how many processing attempts have been performed against this envelope.
    /// </summary>
    /// <remarks>
    /// Essential for background worker scheduling routines to calculate exponential backoff pauses or route failing 
    /// messages into a Dead-Letter Table after surpassing threshold ceilings.
    /// </remarks>
    int RetryCount { get; }

    /// <summary>
    /// Gets the string trace exception logging output from the most recent failed network delivery transaction.
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Gets the multi-tenant logical partitioning identifier isolating this outbox payload boundary.
    /// </summary>
    /// <value>
    /// A string representation mapping to your multi-tenant tenant context, or <see langword="null"/> if processed 
    /// under global host infrastructure pipelines.
    /// </value>
    string? TenantId { get; }

    /// <summary>
    /// Gets the ambient correlation identifier tracing this message's overarching distributed workflow path.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Transition behavior flagging the outbox message envelope as successfully dispatched to the external infrastructure broker.
    /// </summary>
    void MarkAsProcessed();

    /// <summary>
    /// Operational fallback behavior logging a failed delivery transaction, capturing trace diagnostics, and preparing backoff indexes.
    /// </summary>
    /// <param name="errorMessage">The structural error or network exception message returned from the target broker channel link.</param>
    void LogFailure(string errorMessage);
}