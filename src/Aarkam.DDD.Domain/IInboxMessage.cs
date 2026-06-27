namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the structural contract for an Inbox Message record used to enforce idempotent integration event consumer patterns.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Distributed message brokers (such as RabbitMQ, Kafka, or Azure Service Bus) guarantee 
/// <i>At-Least-Once</i> delivery mechanics. This means network interruptions or handler retries will inevitably 
/// cause duplicate messages to arrive at your consumer boundaries. The Inbox pattern isolates your Domain layer 
/// from these duplicates by tracking consumer executions deterministically.
/// </para>
/// <para>
/// <b>TRANSACTIONAL EXECUTION SEQUENCE:</b>
/// <list type="number">
///   <item><description>An incoming message is received by the consumer infrastructure pipeline.</description></item>
///   <item><description>The pipeline queries the Inbox using a composite key matching the <see cref="EventId"/> and the active <see cref="ConsumerName"/>.</description></item>
///   <item><description>If a matching log exists, the execution is silently skipped (deduplicated) to protect state integrity.</description></item>
///   <item><description>If no log exists, the event handler runs, and the inbox record is stored within the exact same database transaction boundary.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>MAINTENANCE REQUIREMENT:</b> To protect datastore querying throughput, establish an automated background job 
/// (e.g., via Quartz.NET or Hangfire) to purge historical records older than a configured sliding retention window (typically 7 to 30 days).
/// </para>
/// </remarks>
public interface IInboxMessage
{
    /// <summary>
    /// Gets the unique tracking identifier assigned to the incoming message payload.
    /// </summary>
    /// <value>A <see cref="Guid"/> originating from the upstream integration event engine.</value>
    Guid EventId { get; }

    /// <summary>
    /// Gets the fully qualified C# structural namespace string classification of the incoming integration event object.
    /// </summary>
    /// <example>"Aarkam.Ordering.IntegrationEvents.OrderPaidIntegrationEvent"</example>
    string EventType { get; }

    /// <summary>
    /// Gets the unique behavioral identification label of the specific component consuming the event.
    /// </summary>
    /// <remarks>
    /// <b>WHY TRACK CONSUMER NAMES?</b> Multiple independent handlers might listen to the exact same event stream. 
    /// For instance, <c>SendOrderConfirmationEmailConsumer</c> and <c>AllocateWarehouseInventoryConsumer</c> will both process 
    /// the same event payload. Using a composite primary database key consisting of <c>[EventId, ConsumerName]</c> ensures 
    /// they can track their idempotency boundaries independently without blocking each other.
    /// </remarks>
    string ConsumerName { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when the event consumer execution successfully committed.
    /// </summary>
    DateTimeOffset ProcessedAt { get; }

    /// <summary>
    /// Gets the multi-tenant logical partitioning identifier isolating this inbox payload boundary.
    /// </summary>
    /// <value>
    /// A string representation mapping to your multi-tenant tenant context, or <see langword="null"/> if processed 
    /// under global host infrastructure pipelines.
    /// </value>
    string? TenantId { get; }
}