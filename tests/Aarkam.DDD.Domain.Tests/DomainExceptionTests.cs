using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class DomainExceptionTests
{
    private sealed class CustomTestException : DomainException
    {
        public CustomTestException(string message) : base(message) { }
        public CustomTestException(string message, string errorCode, ExceptionSeverity severity)
            : base(message, errorCode, severity) { }
        public CustomTestException(string message, Exception innerException)
            : base(message, innerException) { }
        public CustomTestException(string message, string errorCode, Exception innerException, ExceptionSeverity severity)
            : base(message, errorCode, innerException, severity) { }
    }

    private sealed class BrokenRuleMock : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "Custom invariant violation message.";
        public string ErrorCode => "ERR_CUSTOM_INVARIANT";
        public ExceptionSeverity Severity => ExceptionSeverity.Warning;
    }

    private sealed class DefaultRuleMock : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "Default rule broken.";
    }

    [Fact]
    public void BaseDomainException_DefaultProperties_InitializedCorrectly()
    {
        var ex = new CustomTestException("Something went wrong");

        Assert.Equal("Something went wrong", ex.Message);
        Assert.Equal("DOMAIN_ERROR", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Error, ex.Severity);
        Assert.NotNull(ex.Details);
        Assert.Empty(ex.Details);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void BaseDomainException_CustomErrorCodeAndSeverity_InitializedCorrectly()
    {
        var ex = new CustomTestException("Invalid balance", "ERR_INVALID_BALANCE", ExceptionSeverity.Warning);

        Assert.Equal("Invalid balance", ex.Message);
        Assert.Equal("ERR_INVALID_BALANCE", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
    }

    [Fact]
    public void BaseDomainException_WithInnerException_InitializedCorrectly()
    {
        var inner = new InvalidOperationException("Root cause");
        var ex = new CustomTestException("Domain failure", inner);

        Assert.Same(inner, ex.InnerException);
        Assert.Equal("DOMAIN_ERROR", ex.ErrorCode);
    }

    [Fact]
    public void BaseDomainException_WithInnerExceptionAndCustomSeverity_InitializedCorrectly()
    {
        var inner = new TimeoutException("Database timeout");
        var ex = new CustomTestException("Timeout occurred", "ERR_TIMEOUT", inner, ExceptionSeverity.Critical);

        Assert.Same(inner, ex.InnerException);
        Assert.Equal("ERR_TIMEOUT", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Critical, ex.Severity);
    }

    [Fact]
    public void WithDetail_FluentAttachment_PopulatesDetailsDictionary()
    {
        var ex = new CustomTestException("Error")
            .WithDetail("UserId", "user-123")
            .WithDetail("TenantId", "tenant-abc")
            .WithDetail("AttemptCount", 3);

        Assert.Equal(3, ex.Details.Count);
        Assert.Equal("user-123", ex.Details["UserId"]);
        Assert.Equal("tenant-abc", ex.Details["TenantId"]);
        Assert.Equal(3, ex.Details["AttemptCount"]);
    }

    [Fact]
    public void BusinessRuleValidationException_InheritsFromBrokenRuleProperties()
    {
        var rule = new BrokenRuleMock();
        var ex = new BusinessRuleValidationException(rule);

        Assert.Same(rule, ex.BrokenRule);
        Assert.Equal(rule.Message, ex.Message);
        Assert.Equal("ERR_CUSTOM_INVARIANT", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
        Assert.Equal(typeof(BrokenRuleMock).FullName, ex.Details["BrokenRuleType"]);
    }

    [Fact]
    public void BusinessRuleValidationException_DefaultRule_DerivesClassnameAndErrorSeverity()
    {
        var rule = new DefaultRuleMock();
        var ex = new BusinessRuleValidationException(rule);

        Assert.Equal(rule.Message, ex.Message);
        Assert.Equal(nameof(DefaultRuleMock), ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Error, ex.Severity);
    }

    [Fact]
    public void BusinessRuleValidationException_NullRule_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BusinessRuleValidationException(null!));
    }

    [Fact]
    public void EntityNotFoundException_WithEntityTypeAndId_SetsDetailsAndWarningSeverity()
    {
        var entityId = Guid.NewGuid();
        var ex = new EntityNotFoundException(typeof(EntityNotFoundException), entityId);

        Assert.Equal(typeof(EntityNotFoundException), ex.EntityType);
        Assert.Equal(entityId, ex.EntityId);
        Assert.Equal("ERR_ENTITY_NOT_FOUND", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
        Assert.Equal(typeof(EntityNotFoundException).FullName, ex.Details["EntityType"]);
        Assert.Equal(entityId.ToString(), ex.Details["EntityId"]);
    }

    [Fact]
    public void EntityNotFoundException_WithMessageConstructor_SetsProperties()
    {
        var ex = new EntityNotFoundException("Customer #42 not found", "CUSTOMER_NOT_FOUND", ExceptionSeverity.Warning);

        Assert.Equal("Customer #42 not found", ex.Message);
        Assert.Equal("CUSTOMER_NOT_FOUND", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
    }

    [Fact]
    public void DomainInvalidOperationException_InitializesDefaults()
    {
        var ex = new DomainInvalidOperationException("Cannot cancel shipped order");

        Assert.Equal("Cannot cancel shipped order", ex.Message);
        Assert.Equal("ERR_DOMAIN_INVALID_OPERATION", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Error, ex.Severity);
    }

    [Fact]
    public void DomainConflictException_InitializesDefaults()
    {
        var ex = new DomainConflictException("Voucher code already claimed");

        Assert.Equal("Voucher code already claimed", ex.Message);
        Assert.Equal("ERR_DOMAIN_CONFLICT", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Error, ex.Severity);
    }

    [Fact]
    public void DomainForbiddenException_InitializesDefaults()
    {
        var ex = new DomainForbiddenException("User is not authorized to approve own invoice");

        Assert.Equal("User is not authorized to approve own invoice", ex.Message);
        Assert.Equal("ERR_DOMAIN_FORBIDDEN", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
    }

    [Fact]
    public void AggregateConcurrencyException_SetsAggregateTypeAndIdWithCriticalSeverity()
    {
        var ex = new AggregateConcurrencyException(typeof(AggregateRoot), "order-777");

        Assert.Equal(typeof(AggregateRoot), ex.AggregateType);
        Assert.Equal("order-777", ex.AggregateId);
        Assert.Equal("ERR_CONCURRENCY_CONFLICT", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Critical, ex.Severity);
        Assert.Equal(typeof(AggregateRoot).FullName, ex.Details["AggregateType"]);
        Assert.Equal("order-777", ex.Details["AggregateId"]);
    }

    [Fact]
    public void DomainEventIsNullException_SetsCriticalSeverity()
    {
        var ex = new DomainEventIsNullException();

        Assert.Equal("The dispatched domain event instance reference cannot be null.", ex.Message);
        Assert.Equal("ERR_DOMAIN_EVENT_NULL", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Critical, ex.Severity);
    }

    [Fact]
    public void DomainValidationException_MultipleErrors_StoresAllErrorsAndCount()
    {
        var errors = new[]
        {
            new DomainValidationError("Email", "Email is required.", "ERR_EMAIL_REQUIRED"),
            new DomainValidationError("Age", "Must be at least 18.", "ERR_AGE_INVALID"),
            new DomainValidationError("Password", "Too short.")
        };

        var ex = new DomainValidationException(errors);

        Assert.Equal(3, ex.Errors.Count);
        Assert.Equal("ERR_DOMAIN_VALIDATION", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
        Assert.Equal(3, ex.Details["ErrorsCount"]);
        Assert.Equal("Email", ex.Errors[0].PropertyName);
        Assert.Equal("ERR_EMAIL_REQUIRED", ex.Errors[0].ErrorCode);
        Assert.Null(ex.Errors[2].ErrorCode);
    }

    [Fact]
    public void DomainValidationException_SingleErrorConstructor_SetsSingleError()
    {
        var ex = new DomainValidationException("Username", "Username is already taken.", "ERR_USER_EXISTS");

        Assert.Single(ex.Errors);
        Assert.Equal("Username", ex.Errors[0].PropertyName);
        Assert.Equal("Username is already taken.", ex.Errors[0].ErrorMessage);
        Assert.Equal("ERR_USER_EXISTS", ex.Errors[0].ErrorCode);
        Assert.Equal("ERR_USER_EXISTS", ex.ErrorCode);
    }

    [Theory]
    [InlineData("ERR_001", ExceptionSeverity.Warning)]
    [InlineData("ERR_002", ExceptionSeverity.Error)]
    [InlineData("ERR_003", ExceptionSeverity.Critical)]
    public void Parameterized_SeverityAndErrorCode_Mapping(string code, ExceptionSeverity severity)
    {
        var ex = new CustomTestException("Test message", code, severity);

        Assert.Equal(code, ex.ErrorCode);
        Assert.Equal(severity, ex.Severity);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void BaseDomainException_EmptyOrNullErrorCode_DefaultsToDomainError(string? emptyCode)
    {
        var ex = new CustomTestException("Message", emptyCode!, ExceptionSeverity.Error);
        Assert.Equal("DOMAIN_ERROR", ex.ErrorCode);
    }
}
