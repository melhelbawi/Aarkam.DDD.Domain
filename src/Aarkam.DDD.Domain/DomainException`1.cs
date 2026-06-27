namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the contract for an encapsulation of a distinct, atomic domain business rule.
/// </summary>
/// <remarks>
/// <b>DESIGN PHILOSOPHY:</b> Instead of cluttering Aggregate methods with complex nested <c>if-else</c> conditional expressions, 
/// business rules are extracted into dedicated, highly testable specification objects implementing this interface. 
/// They decouple behavioral checks from the execution path of the entity state engine.
/// </remarks>
public interface IBusinessRule
{
    /// <summary>
    /// Evaluates whether the current domain state state variables violate this explicit business rule specification.
    /// </summary>
    /// <returns><see langword="true"/> if the rule constraint is broken/violated; otherwise, <see langword="false"/>.</returns>
    bool IsBroken();

    /// <summary>
    /// Gets the intention-revealing, localized explanation message exposed when the specification is violated.
    /// </summary>
    string Message { get; }
}

/// <summary>
/// Serves as the abstract foundation for all exceptions originating inside the Domain Model layer.
/// </summary>
/// <remarks>
/// <b>BOUNDARY ARCHITECTURE RULE:</b> Exceptions inheriting from this class signify business domain logic failures. 
/// They are explicitly decoupled from infrastructural exceptions (like database deadlocks, network connection timeouts, or null-pointer checks) 
/// and are safe to bubble up directly into API Gateway exception mapping interceptors.
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a targeted root exception trace.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    /// <param name="innerException">The structural catalyst exception that tripped this domain boundary layer check.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown immediately when an <see cref="IBusinessRule"/> specification asserts a validation violation.
/// </summary>
public class BusinessRuleValidationException : DomainException
{
    /// <summary>
    /// Gets the concrete specification capsule that failed validation parameters.
    /// </summary>
    public IBusinessRule BrokenRule { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleValidationException"/> error tracking class.
    /// </summary>
    /// <param name="brokenRule">The business specification instance marking the violation boundary.</param>
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule?.Message ?? "A critical domain business invariant specification rule has been violated.")
    {
        BrokenRule = brokenRule ?? throw new ArgumentNullException(nameof(brokenRule));
    }

    /// <summary>
    /// Unified static assertion interceptor providing rapid inline validation testing patterns inside aggregate roots.
    /// </summary>
    /// <param name="rule">The targeted business constraint specification to analyze.</param>
    /// <exception cref="BusinessRuleValidationException">Thrown instantly if the evaluate condition of <see cref="IBusinessRule.IsBroken"/> satisfies true.</exception>
    public static void Check(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
}

/// <summary>
/// Exception thrown when a requested domain entity fails to materialize from local data stores because its primary key record is absent.
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Gets the type classification of the domain entity requested.
    /// </summary>
    public Type? EntityType { get; }

    /// <summary>
    /// Gets the structural primary lookup identification key driving the query transaction.
    /// </summary>
    public object? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with metadata tags.
    /// </summary>
    /// <param name="entityType">The class configuration metadata type of the target entity.</param>
    /// <param name="entityId">The identifying key used during retrieval operations.</param>
    public EntityNotFoundException(Type entityType, object entityId)
        : base($"The requested domain resource entity '{entityType.Name}' tagged with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom, tailored exception message.
    /// </summary>
    /// <param name="message">The custom message detailing missing entity context.</param>
    public EntityNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when an aggregate root command attempts to trigger a state transition that is invalid given its current operational status.
/// </summary>
/// <remarks>
/// <b>WHEN TO USE:</b> Apply this when an external command breaks state-machine workflows (e.g., calling <c>CancelOrder()</c> on an order that has already been shipped).
/// </remarks>
public class DomainInvalidOperationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainInvalidOperationException"/> exception tracking class.
    /// </summary>
    /// <param name="message">The custom exception explanation string text.</param>
    public DomainInvalidOperationException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a structural logical conflict occurs between overlapping domain rules or tracking indexes.
/// </summary>
/// <remarks>
/// <b>WHEN TO USE:</b> Ideal for logical business clashes that happen outside technical database tracking lines, 
/// such as trying to claim a voucher code that has already been exhausted, or allocating overlapping calendar booking schedules.
/// </remarks>
public class DomainConflictException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> exception tracking class.
    /// </summary>
    /// <param name="message">The custom conflict details statement.</param>
    public DomainConflictException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a user or principal attempts to invoke behavior that is explicitly barred by the model's inner security policy.
/// </summary>
/// <remarks>
/// <b>WHEN TO USE:</b> Enforces internal behavioral security rules (e.g., an invoice author trying to approve their own cost sheet).
/// </remarks>
public class DomainForbiddenException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainForbiddenException"/> security tracking class.
    /// </summary>
    /// <param name="message">The authorization violation explanation details statement.</param>
    public DomainForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when an Optimistic Concurrency trace check asserts that an aggregate version mismatch occurred during stream serialization updates.
/// </summary>
/// <remarks>
/// <b>CONCURRENCY RESOLUTION MECHANICS:</b> In event-sourced streams or snapshot-delta stores, this signals a race condition—another 
/// thread updated the state stream since this instance was read. Custom infrastructure layers capture this block to prompt command-retry loops safely.
/// </remarks>
public class AggregateConcurrencyException : DomainException
{
    /// <summary>
    /// Gets the class reflection type of the competing aggregate root instance.
    /// </summary>
    public Type AggregateType { get; }

    /// <summary>
    /// Gets the string conversion identity key identifying the targeted stream resource partition.
    /// </summary>
    public string AggregateId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateConcurrencyException"/> concurrency tracker.
    /// </summary>
    /// <param name="aggregateType">The class configuration metadata type of the target entity.</param>
    /// <param name="aggregateId">The identifying key of the aggregate instance.</param>
    public AggregateConcurrencyException(Type aggregateType, string aggregateId)
        : base($"An optimistic concurrency write conflict was encountered while updating aggregate stream '{aggregateType.Name}' [ID: {aggregateId}]. The historical sequence has advanced.")
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateConcurrencyException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom exception explanation message text.</param>
    /// <param name="aggregateType">The class configuration metadata type of the target entity.</param>
    /// <param name="aggregateId">The identifying key of the aggregate instance.</param>
    public AggregateConcurrencyException(string message, Type aggregateType, string aggregateId)
        : base(message)
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
    }
}

/// <summary>
/// Exception thrown when a required domain event instance reference evaluates to null during processing.
/// </summary>
public class DomainEventIsNullException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventIsNullException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DomainEventIsNullException(string message = "The dispatched domain event instance reference cannot be null.")
        : base(message)
    {
    }
}