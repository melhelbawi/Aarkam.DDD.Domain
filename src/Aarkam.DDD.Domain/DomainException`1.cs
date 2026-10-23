namespace Aarkam.DDD.Domain;

/// <summary>
/// Categorizes the impact and operational severity of a domain error or exception.
/// Enables structured telemetry, Serilog/Datadog alerting, and dynamic error routing.
/// </summary>
public enum ExceptionSeverity
{
    /// <summary>
    /// Non-fatal, expected business validation or client-side deviation (e.g., invalid input, transient condition).
    /// Typically logged as an informational or warning metric without triggering on-call alerts.
    /// </summary>
    Warning,

    /// <summary>
    /// Standard domain violation or business invariant failure (e.g., broken invariant, conflict, forbidden action).
    /// Typically logged as an application error and mapped to 4xx HTTP status codes.
    /// </summary>
    Error,

    /// <summary>
    /// High-impact or structural system condition (e.g., state corruption, null event dispatched, concurrency race failure).
    /// Triggers immediate logging alerts and potential retry or compensation workflows.
    /// </summary>
    Critical
}

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
    /// Evaluates whether the current domain state variables violate this explicit business rule specification.
    /// </summary>
    /// <returns><see langword="true"/> if the rule constraint is broken/violated; otherwise, <see langword="false"/>.</returns>
    bool IsBroken();

    /// <summary>
    /// Gets the intention-revealing, localized explanation message exposed when the specification is violated.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets a unique, machine-readable identifier for this rule violation (e.g., ERR_AUTH_001, USER_NOT_FOUND).
    /// Defaults to the class name of the rule specification.
    /// </summary>
    string ErrorCode => GetType().Name;

    /// <summary>
    /// Gets the severity categorization of this rule violation.
    /// Defaults to <see cref="ExceptionSeverity.Error"/>.
    /// </summary>
    ExceptionSeverity Severity => ExceptionSeverity.Error;
}

/// <summary>
/// Serves as the abstract foundation for all exceptions originating inside the Domain Model layer.
/// Incorporates Structured Error Handling with machine-readable <see cref="ErrorCode"/>,
/// <see cref="Severity"/> categorization, and contextual <see cref="Details"/>.
/// </summary>
/// <remarks>
/// <b>BOUNDARY ARCHITECTURE RULE:</b> Exceptions inheriting from this class signify business domain logic failures. 
/// They are explicitly decoupled from infrastructural exceptions (like database deadlocks, network connection timeouts, or null-pointer checks) 
/// and are safe to bubble up directly into API Gateway exception mapping interceptors.
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets a unique, machine-readable identifier for the error (e.g., ERR_AUTH_001, USER_NOT_FOUND).
    /// Allows client applications to display localized messages without fragile text parsing.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the categorical severity of the error (Warning, Error, Critical).
    /// Enables logging infrastructure (Serilog, Datadog) to route alerts accordingly.
    /// </summary>
    public ExceptionSeverity Severity { get; }

    /// <summary>
    /// Gets structured contextual metadata properties associated with the exception.
    /// </summary>
    public IDictionary<string, object?> Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with structured error parameters.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    /// <param name="errorCode">A unique machine-readable error code. If omitted or null, defaults to "DOMAIN_ERROR".</param>
    /// <param name="severity">The categorical severity rating of the exception.</param>
    /// <param name="innerException">The structural catalyst exception that tripped this domain boundary check.</param>
    /// <param name="details">Optional dictionary containing contextual metadata for debugging and logging.</param>
    protected DomainException(
        string message,
        string? errorCode = null,
        ExceptionSeverity severity = ExceptionSeverity.Error,
        Exception? innerException = null,
        IDictionary<string, object?>? details = null)
        : base(message, innerException)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "DOMAIN_ERROR" : errorCode;
        Severity = severity;
        Details = details != null
            ? new Dictionary<string, object?>(details)
            : new Dictionary<string, object?>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    protected DomainException(string message)
        : this(message, "DOMAIN_ERROR", ExceptionSeverity.Error)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a targeted root exception trace.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    /// <param name="innerException">The structural catalyst exception that tripped this domain boundary layer check.</param>
    protected DomainException(string message, Exception innerException)
        : this(message, "DOMAIN_ERROR", innerException, ExceptionSeverity.Error)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an explicit error code and severity.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    /// <param name="errorCode">A unique machine-readable error code.</param>
    /// <param name="severity">The categorical severity rating of the exception.</param>
    protected DomainException(string message, string? errorCode, ExceptionSeverity severity = ExceptionSeverity.Error)
        : this(message, errorCode, severity, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an error code, inner exception, and severity.
    /// </summary>
    /// <param name="message">The exception message explaining the structural domain rule failure.</param>
    /// <param name="errorCode">A unique machine-readable error code.</param>
    /// <param name="innerException">The structural catalyst exception that tripped this domain boundary layer check.</param>
    /// <param name="severity">The categorical severity rating of the exception.</param>
    protected DomainException(string message, string? errorCode, Exception? innerException, ExceptionSeverity severity = ExceptionSeverity.Error)
        : this(message, errorCode, severity, innerException, null)
    {
    }

    /// <summary>
    /// Fluently attaches a contextual key-value pair to the exception's structured details payload for logging and tracing.
    /// </summary>
    /// <param name="key">The metadata key name.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current <see cref="DomainException"/> instance for chaining.</returns>
    public DomainException WithDetail(string key, object? value)
    {
        Details[key] = value;
        return this;
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
    /// Automatically extracts <see cref="IBusinessRule.ErrorCode"/> and <see cref="IBusinessRule.Severity"/>.
    /// </summary>
    /// <param name="brokenRule">The business specification instance marking the violation boundary.</param>
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(
            brokenRule?.Message ?? "A critical domain business invariant specification rule has been violated.",
            brokenRule?.ErrorCode ?? "ERR_BUSINESS_RULE_VIOLATION",
            brokenRule?.Severity ?? ExceptionSeverity.Error)
    {
        BrokenRule = brokenRule ?? throw new ArgumentNullException(nameof(brokenRule));
        WithDetail("BrokenRuleType", brokenRule.GetType().FullName);
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
        : base(
            $"The requested domain resource entity '{entityType.Name}' tagged with ID '{entityId}' was not found.",
            "ERR_ENTITY_NOT_FOUND",
            ExceptionSeverity.Warning)
    {
        EntityType = entityType;
        EntityId = entityId;
        WithDetail("EntityType", entityType?.FullName);
        WithDetail("EntityId", entityId?.ToString());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom, tailored exception message.
    /// </summary>
    /// <param name="message">The custom message detailing missing entity context.</param>
    /// <param name="errorCode">An optional custom error code, defaulting to "ERR_ENTITY_NOT_FOUND".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Warning"/>.</param>
    public EntityNotFoundException(
        string message,
        string errorCode = "ERR_ENTITY_NOT_FOUND",
        ExceptionSeverity severity = ExceptionSeverity.Warning)
        : base(message, errorCode, severity)
    {
    }
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
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_DOMAIN_INVALID_OPERATION".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Error"/>.</param>
    public DomainInvalidOperationException(
        string message,
        string errorCode = "ERR_DOMAIN_INVALID_OPERATION",
        ExceptionSeverity severity = ExceptionSeverity.Error)
        : base(message, errorCode, severity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainInvalidOperationException"/> class with an inner exception.
    /// </summary>
    public DomainInvalidOperationException(
        string message,
        Exception innerException,
        string errorCode = "ERR_DOMAIN_INVALID_OPERATION",
        ExceptionSeverity severity = ExceptionSeverity.Error)
        : base(message, errorCode, severity, innerException)
    {
    }
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
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_DOMAIN_CONFLICT".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Error"/>.</param>
    public DomainConflictException(
        string message,
        string errorCode = "ERR_DOMAIN_CONFLICT",
        ExceptionSeverity severity = ExceptionSeverity.Error)
        : base(message, errorCode, severity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class with an inner exception.
    /// </summary>
    public DomainConflictException(
        string message,
        Exception innerException,
        string errorCode = "ERR_DOMAIN_CONFLICT",
        ExceptionSeverity severity = ExceptionSeverity.Error)
        : base(message, errorCode, severity, innerException)
    {
    }
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
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_DOMAIN_FORBIDDEN".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Warning"/>.</param>
    public DomainForbiddenException(
        string message,
        string errorCode = "ERR_DOMAIN_FORBIDDEN",
        ExceptionSeverity severity = ExceptionSeverity.Warning)
        : base(message, errorCode, severity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainForbiddenException"/> class with an inner exception.
    /// </summary>
    public DomainForbiddenException(
        string message,
        Exception innerException,
        string errorCode = "ERR_DOMAIN_FORBIDDEN",
        ExceptionSeverity severity = ExceptionSeverity.Warning)
        : base(message, errorCode, severity, innerException)
    {
    }
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
        : base(
            $"An optimistic concurrency write conflict was encountered while updating aggregate stream '{aggregateType.Name}' [ID: {aggregateId}]. The historical sequence has advanced.",
            "ERR_CONCURRENCY_CONFLICT",
            ExceptionSeverity.Critical)
    {
        AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
        AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
        WithDetail("AggregateType", aggregateType.FullName);
        WithDetail("AggregateId", aggregateId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateConcurrencyException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom exception explanation message text.</param>
    /// <param name="aggregateType">The class configuration metadata type of the target entity.</param>
    /// <param name="aggregateId">The identifying key of the aggregate instance.</param>
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_CONCURRENCY_CONFLICT".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Critical"/>.</param>
    public AggregateConcurrencyException(
        string message,
        Type aggregateType,
        string aggregateId,
        string errorCode = "ERR_CONCURRENCY_CONFLICT",
        ExceptionSeverity severity = ExceptionSeverity.Critical)
        : base(message, errorCode, severity)
    {
        AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
        AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
        WithDetail("AggregateType", aggregateType.FullName);
        WithDetail("AggregateId", aggregateId);
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
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_DOMAIN_EVENT_NULL".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Critical"/>.</param>
    public DomainEventIsNullException(
        string message = "The dispatched domain event instance reference cannot be null.",
        string errorCode = "ERR_DOMAIN_EVENT_NULL",
        ExceptionSeverity severity = ExceptionSeverity.Critical)
        : base(message, errorCode, severity)
    {
    }
}

/// <summary>
/// Represents a discrete domain invariant or validation failure on a specific property or state component.
/// </summary>
/// <param name="PropertyName">The name of the property or component that failed validation.</param>
/// <param name="ErrorMessage">The human-readable description of why the validation failed.</param>
/// <param name="ErrorCode">The optional machine-readable error code for localized client handling.</param>
public record DomainValidationError(string PropertyName, string ErrorMessage, string? ErrorCode = null);

/// <summary>
/// Exception thrown when one or more aggregate validation rules or invariants fail simultaneously.
/// Prevents repetitive single-error roundtrips by aggregating all invalid domain states.
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    /// Gets the list of individual domain validation failures.
    /// </summary>
    public IReadOnlyList<DomainValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a collection of validation errors.
    /// </summary>
    /// <param name="errors">The collection of validation failures.</param>
    /// <param name="message">The overarching failure message.</param>
    /// <param name="errorCode">A unique machine-readable error code, defaulting to "ERR_DOMAIN_VALIDATION".</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Warning"/>.</param>
    public DomainValidationException(
        IEnumerable<DomainValidationError> errors,
        string message = "One or more domain validation invariants failed.",
        string errorCode = "ERR_DOMAIN_VALIDATION",
        ExceptionSeverity severity = ExceptionSeverity.Warning)
        : base(message, errorCode, severity)
    {
        Errors = errors?.ToList().AsReadOnly() ?? (IReadOnlyList<DomainValidationError>)Array.Empty<DomainValidationError>();
        WithDetail("ErrorsCount", Errors.Count);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class with a single validation error.
    /// </summary>
    /// <param name="propertyName">The name of the invalid property or target.</param>
    /// <param name="errorMessage">The error description.</param>
    /// <param name="errorCode">The optional machine-readable error code.</param>
    /// <param name="severity">The categorical severity rating, defaulting to <see cref="ExceptionSeverity.Warning"/>.</param>
    public DomainValidationException(
        string propertyName,
        string errorMessage,
        string? errorCode = null,
        ExceptionSeverity severity = ExceptionSeverity.Warning)
        : this(
            new[] { new DomainValidationError(propertyName, errorMessage, errorCode) },
            errorMessage,
            errorCode ?? "ERR_DOMAIN_VALIDATION",
            severity)
    {
    }
}
