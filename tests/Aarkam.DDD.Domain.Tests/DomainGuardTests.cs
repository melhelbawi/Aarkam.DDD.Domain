using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class DomainGuardTests
{
    private sealed class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
    }

    private sealed class AlwaysBrokenRule : IBusinessRule
    {
        public string Message => "Invariant broken rule test message.";
        public string ErrorCode => "ERR_ALWAYS_BROKEN";
        public ExceptionSeverity Severity => ExceptionSeverity.Critical;
        public bool IsBroken() => true;
    }

    private sealed class AlwaysValidRule : IBusinessRule
    {
        public string Message => "Valid rule.";
        public string ErrorCode => "RULE_VALID";
        public ExceptionSeverity Severity => ExceptionSeverity.Warning;
        public bool IsBroken() => false;
    }

    #region Against

    [Fact]
    public void Against_WhenAssertionIsTrue_ThrowsDomainInvalidOperationException()
    {
        var ex = Assert.Throws<DomainInvalidOperationException>(() =>
            DomainGuard.Against(true, "Operation was invalid."));

        Assert.Equal("Operation was invalid.", ex.Message);
        Assert.Equal("ERR_GUARD_VIOLATION", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Error, ex.Severity);
    }

    [Fact]
    public void Against_WhenAssertionIsFalse_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            DomainGuard.Against(false, "Should not throw."));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("CUSTOM_ERR_01", ExceptionSeverity.Warning)]
    [InlineData("CUSTOM_ERR_02", ExceptionSeverity.Error)]
    [InlineData("CRITICAL_ERR_03", ExceptionSeverity.Critical)]
    public void Against_WithCustomErrorCodeAndSeverity_PropagatesCorrectly(string errorCode, ExceptionSeverity severity)
    {
        var ex = Assert.Throws<DomainInvalidOperationException>(() =>
            DomainGuard.Against(true, "Custom guard message.", errorCode, severity));

        Assert.Equal("Custom guard message.", ex.Message);
        Assert.Equal(errorCode, ex.ErrorCode);
        Assert.Equal(severity, ex.Severity);
    }

    #endregion

    #region Against<TException>

    [Fact]
    public void AgainstGeneric_WhenAssertionIsTrue_ThrowsSpecifiedExceptionType()
    {
        var ex = Assert.Throws<CustomTestException>(() =>
            DomainGuard.Against<CustomTestException>(true, "Custom exception triggered."));

        Assert.Equal("Custom exception triggered.", ex.Message);
    }

    [Fact]
    public void AgainstGeneric_WhenAssertionIsFalse_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            DomainGuard.Against<CustomTestException>(false, "Should not throw."));

        Assert.Null(exception);
    }

    [Fact]
    public void AgainstGeneric_WithDomainNotFoundException_ThrowsCorrectType()
    {
        var ex = Assert.Throws<EntityNotFoundException>(() =>
            DomainGuard.Against<EntityNotFoundException>(true, "Item not found."));

        Assert.Equal("Item not found.", ex.Message);
    }

    [Fact]
    public void AgainstGeneric_WithDomainConflictException_ThrowsCorrectType()
    {
        var ex = Assert.Throws<DomainConflictException>(() =>
            DomainGuard.Against<DomainConflictException>(true, "Conflict detected."));

        Assert.Equal("Conflict detected.", ex.Message);
    }

    [Fact]
    public void AgainstGeneric_WithArgumentException_ThrowsCorrectType()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainGuard.Against<ArgumentException>(true, "Invalid argument."));

        Assert.Equal("Invalid argument.", ex.Message);
    }

    #endregion

    #region AgainstNull

    [Fact]
    public void AgainstNull_WhenValueIsNull_ThrowsArgumentNullException()
    {
        object? nullObj = null;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            DomainGuard.AgainstNull(nullObj, "targetParameter"));

        Assert.Equal("targetParameter", ex.ParamName);
        Assert.Contains("targetParameter", ex.Message);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData(42)]
    [InlineData(true)]
    public void AgainstNull_WhenValueIsNotNull_DoesNotThrow(object validValue)
    {
        var exception = Record.Exception(() =>
            DomainGuard.AgainstNull(validValue, "param"));

        Assert.Null(exception);
    }

    #endregion

    #region AgainstNullOrEmpty

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  \r\n  ")]
    public void AgainstNullOrEmpty_WhenValueIsNullOrWhitespace_ThrowsArgumentException(string? invalidString)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainGuard.AgainstNullOrEmpty(invalidString, "stringParam"));

        Assert.Equal("stringParam", ex.ParamName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Valid text content")]
    [InlineData("  leading and trailing  ")]
    public void AgainstNullOrEmpty_WhenValueHasContent_DoesNotThrow(string validString)
    {
        var exception = Record.Exception(() =>
            DomainGuard.AgainstNullOrEmpty(validString, "stringParam"));

        Assert.Null(exception);
    }

    #endregion

    #region AgainstNegative

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-999999999999L)]
    [InlineData(long.MinValue)]
    public void AgainstNegative_WhenValueIsNegative_ThrowsArgumentException(long negativeValue)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainGuard.AgainstNegative(negativeValue, "amount"));

        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(long.MaxValue)]
    public void AgainstNegative_WhenValueIsZeroOrPositive_DoesNotThrow(long nonNegativeValue)
    {
        var exception = Record.Exception(() =>
            DomainGuard.AgainstNegative(nonNegativeValue, "amount"));

        Assert.Null(exception);
    }

    #endregion

    #region AgainstNegativeOrZero

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    [InlineData(long.MinValue)]
    public void AgainstNegativeOrZero_WhenValueIsZeroOrNegative_ThrowsArgumentException(long invalidValue)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DomainGuard.AgainstNegativeOrZero(invalidValue, "positiveOnlyParam"));

        Assert.Equal("positiveOnlyParam", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1000000L)]
    [InlineData(long.MaxValue)]
    public void AgainstNegativeOrZero_WhenValueIsStrictlyPositive_DoesNotThrow(long positiveValue)
    {
        var exception = Record.Exception(() =>
            DomainGuard.AgainstNegativeOrZero(positiveValue, "positiveOnlyParam"));

        Assert.Null(exception);
    }

    #endregion

    #region AgainstOutOfRange

    [Theory]
    [InlineData(-1, 0, 10)]
    [InlineData(11, 0, 10)]
    [InlineData(-25, -20, 20)]
    [InlineData(21, -20, 20)]
    [InlineData(int.MinValue, 0, 100)]
    [InlineData(int.MaxValue, 0, 100)]
    public void AgainstOutOfRange_WhenValueIsOutsideRange_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DomainGuard.AgainstOutOfRange(value, min, max, "rangeParam"));

        Assert.Equal("rangeParam", ex.ParamName);
        Assert.Equal(value, (int)ex.ActualValue!);
    }

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(5, 0, 10)]
    [InlineData(10, 0, 10)]
    [InlineData(-20, -20, 20)]
    [InlineData(0, -20, 20)]
    [InlineData(20, -20, 20)]
    public void AgainstOutOfRange_WhenValueIsWithinRangeInclusive_DoesNotThrow(int value, int min, int max)
    {
        var exception = Record.Exception(() =>
            DomainGuard.AgainstOutOfRange(value, min, max, "rangeParam"));

        Assert.Null(exception);
    }

    #endregion

    #region CheckRule

    [Fact]
    public void CheckRule_WhenRuleIsBroken_ThrowsBusinessRuleValidationExceptionWithRuleMetadata()
    {
        var rule = new AlwaysBrokenRule();

        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            DomainGuard.CheckRule(rule));

        Assert.Same(rule, ex.BrokenRule);
        Assert.Equal("Invariant broken rule test message.", ex.Message);
        Assert.Equal("ERR_ALWAYS_BROKEN", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Critical, ex.Severity);
    }

    [Fact]
    public void CheckRule_WhenRuleIsValid_DoesNotThrow()
    {
        var rule = new AlwaysValidRule();

        var exception = Record.Exception(() =>
            DomainGuard.CheckRule(rule));

        Assert.Null(exception);
    }

    #endregion
}
