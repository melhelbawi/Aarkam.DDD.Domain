namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the architectural contract for a Saga (Process Manager) orchestrating distributed, long-running workflows across distinct Aggregate boundaries.
/// </summary>
/// <typeparam name="TSagaData">The structural type encapsulating the persistent state memory variables for this process instance.</typeparam>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> In microservice environments or complex bounded contexts, a single business transaction can rarely 
/// be processed inside one atomic database write. For example, fulfilling an order demands actions inside Ordering, Inventory, and Billing aggregates. 
/// A Saga acts as an isolated state machine that listens to chronological integration events, updates its persistent state <see cref="Data"/>, 
/// and dispatches subsequent commands or integration messages until the entire business case satisfies eventual consistency.
/// </para>
/// <para>
/// <b>THE PRINCIPLE OF COMPENSATION:</b> Sagas do not leverage distributed two-phase commit database locks (which break system availability). 
/// If an operational failure occurs midway through the workflow (e.g., credit card declined after inventory reservation), the Saga transitions its lifecycle 
/// status to <see cref="SagaStatus.Compensating"/> and dispatches explicit backward **Compensating Transactions** (e.g., a command to release the reserved items) 
/// to restore equilibrium across the system.
/// </para>
/// </remarks>
/// <seealso cref="SagaStatus"/>
public interface ISaga<out TSagaData>
    where TSagaData : class, new()
{
    /// <summary>
    /// Gets the unique tracking identifier assigned to coordinate this specific process instance flow.
    /// </summary>
    /// <remarks>
    /// <b>CORRELATION MECHANICS:</b> This ID acts as the primary key binding incoming events to this state machine instance. 
    /// When an incoming message arrives, infrastructure interceptors execute a correlation lookup (e.g., mapping <c>OrderPlaced.OrderId</c> 
    /// to <c>SagaId</c>) to rehydrate the correct state instance out of storage.
    /// </remarks>
    Guid SagaId { get; }

    /// <summary>
    /// Gets the persistent state document backing this active process coordinator instance.
    /// </summary>
    /// <value>A custom reference state object containing cached variables necessary to drive downstream routing logic decisions.</value>
    TSagaData Data { get; }

    /// <summary>
    /// Gets the current structural position of the saga within its overarching lifecycle state machine.
    /// </summary>
    SagaStatus Status { get; }

    /// <summary>
    /// Gets the exact, timezone-aware point in time when the initial catalyst event instantiated this workflow.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the exact, timezone-aware point in time when the saga reached a terminal state closure.
    /// </summary>
    /// <value>A valid timestamp if the status matches <see cref="SagaStatus.Completed"/>, <see cref="SagaStatus.Failed"/>, or <see cref="SagaStatus.TimedOut"/>; otherwise, <see langword="null"/>.</value>
    DateTimeOffset? CompletedAt { get; }

    /// <summary>
    /// Gets the structural version sequence index utilized to protect the persistent state model from race conditions.
    /// </summary>
    /// <remarks>
    /// <b>WHY IS THIS CRITICAL?</b> Sagas are highly vulnerable to concurrent asynchronous event arrival. If two separate microservices emit 
    /// completion signals simultaneously, this version tracker forces **Optimistic Concurrency Control** at the datastore layer, ensuring one 
    /// event triggers a safe serial retry while the other commits cleanly.
    /// </remarks>
    long Version { get; }

    /// <summary>
    /// Transitions the state machine into a successfully finalized terminal status condition.
    /// </summary>
    /// <remarks>
    /// Once invoked, the workflow is closed, metadata marks are recorded, and infrastructure may choose to archive or prune the state data from active tables.
    /// </remarks>
    void Complete();

    /// <summary>
    /// Transitions the state machine into an unrecoverable failure state and logs technical diagnostic reasons.
    /// </summary>
    /// <param name="reason">A description detailing the operational bottleneck or business rule violation that halted progression.</param>
    void Fail(string reason);

    /// <summary>
    /// Initiates the reverse workflow chain, dispatching undo messages to return independent aggregates back to safe baseline positions.
    /// </summary>
    /// <remarks>
    /// Changes the operational state profile to <see cref="SagaStatus.Compensating"/>. Concrete implementations must handle this 
    /// by scattering relevant rollback commands out to the outbox mechanism.
    /// </remarks>
    void Compensate();
}

/// <summary>
/// Specifies the categorical operational lifecycle stages an active Saga instance can occupy.
/// </summary>
public enum SagaStatus
{
    /// <summary>
    /// The saga state has been allocated but has not yet accepted its initializing catalyst integration message.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The process coordinator is active, uncompleted, and currently waiting for outstanding asynchronous message callbacks.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The distributed workflow satisfied all validation boundaries and finalized successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The workflow encountered an unrecoverable business failure or logical dead-end, halting further progression.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// An intermediate phase running compensating command blocks to reverse partial state transformations following a downstream bottleneck.
    /// </summary>
    Compensating = 4,

    /// <summary>
    /// A terminal failure state reached when an expected step event failed to return within its designated maximum temporal allowance window.
    /// </summary>
    TimedOut = 5
}