namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the core contract for Integration Events designed to communicate committed business facts across Bounded Context boundaries.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Unlike a <see cref="IDomainEvent"/> (which operates synchronously within an isolated domain aggregate boundary), 
/// an Integration Event is designed for asynchronous propagation across independent microservices or distinct application subsystems. 
/// They act as the primary catalyst for achieving distributed Eventual Consistency.
/// </para>
/// <para>
/// <b>STRUCTURAL BOUNDARIES:</b> Integration events must be highly optimized for cross-network serialization. They must only 
/// contain flat data structures comprised of primitive types, simple arrays, or basic Data Transfer Objects (DTOs). Under no 
/// circumstances should they contain complex domain entity references or internal behavioral logic.
/// </para>
/// </remarks>
/// <seealso cref="IDomainEvent"/>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique tracking identifier assigned to this specific event message instance.
    /// </summary>
    /// <value>A <see cref="Guid"/> used uniformly by outbox dispatchers and inbox tables to enforce idempotency and message deduplication.</value>
    Guid EventId { get; }

    /// <summary>
    /// Gets the exact, timezone-aware point in time when the core transaction was permanently committed by the origin domain service.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets a string discriminator identifying the concrete event structure type.
    /// </summary>
    /// <remarks>
    /// Infrastructure messaging handlers use this token as a routing key or mapping target during deserialization loops. 
    /// For cross-platform stability (e.g., sharing messages with non-.NET services), this can be customized to a stable uniform identifier.
    /// </remarks>
    string EventType { get; }
}

/// <summary>
/// Serves as the base implementation for immutable integration events published outside the active local aggregate boundaries.
/// </summary>
/// <remarks>
/// This base class encapsulates standard asynchronous messaging telemetry (including Event IDs, generation timestamps, 
/// and distributed messaging tracing tokens) while preserving strict model immutability.
/// </remarks>
public abstract class IntegrationEvent : IIntegrationEvent
{
    /// <inheritdoc />
    /// <value>The protected setter allows infrastructure serialization engines to safely rehydrate values during outbox processing.</value>
    public Guid EventId { get; protected set; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOn { get; protected set; }

    /// <inheritdoc />
    public virtual string EventType => GetType().FullName ?? GetType().Name;

    /// <summary>
    /// Gets the ambient correlation identifier assigned to trace an overarching distributed business transaction flow across multiple services.
    /// </summary>
    /// <remarks>
    /// <b>DISTRIBUTED TRACING ROLE:</b> This value tracks the origin transaction path across decoupled systems (e.g., following a request from 
    /// an API Gateway down to Ordering, Billing, and Shipping services). It mirrors OpenTelemetry standard tracking parameters.
    /// </remarks>
    public Guid CorrelationId { get; protected set; }

    /// <summary>
    /// Gets the optional causation identifier specifying the exact upstream message token that directly triggered this event.
    /// </summary>
    /// <remarks>
    /// Useful for reconstructing causal messaging trees, choreographing Sagas, and debugging complex distributed event chains.
    /// </remarks>
    public Guid? CausationId { get; protected set; }

    /// <summary>
    /// Initializes a brand-new integration event instance with standard diagnostic tracing telemetry.
    /// </summary>
    /// <param name="correlationId">An optional correlation context token. Automatically generates a fresh <see cref="Guid"/> if omitted.</param>
    /// <param name="causationId">An optional event or command identification token indicating the catalyst for this transaction.</param>
    protected IntegrationEvent(Guid? correlationId = null, Guid? causationId = null)
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        CorrelationId = correlationId ?? Guid.NewGuid();
        CausationId = causationId;
    }

    /// <summary>
    /// Protected parameterless constructor enforcing structural compatibility with reflection-driven serialization frameworks.
    /// </summary>
    /// <remarks>
    /// <b>WHY PROVIDE THIS HOOK?</b> Advanced binary and text-based network serializers (including System.Text.Json, 
    /// MessagePack, and Protocol Buffers) require a parameterless constructor to instantiate the blank metadata envelope before 
    /// mapping property tokens. It maps healthy defaults to shield against null-assignment telemetry corruptions.
    /// </remarks>
    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        CorrelationId = Guid.NewGuid();
    }
}