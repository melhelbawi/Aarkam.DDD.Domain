using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class EnumerationTests
{
    private sealed class OrderStatus : Enumeration
    {
        public static readonly OrderStatus Draft = new(1, nameof(Draft), "Draft Order");
        public static readonly OrderStatus Processing = new(2, nameof(Processing), "Processing Order");
        public static readonly OrderStatus Shipped = new(3, nameof(Shipped), "Shipped to Customer");
        public static readonly OrderStatus Completed = new(4, nameof(Completed), "Completed");
        public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled), "Cancelled by Customer");

        private OrderStatus(int id, string name, string? displayName = null) : base(id, name, displayName) { }
    }

    private sealed class EmptyEnumeration : Enumeration
    {
        private EmptyEnumeration(int id, string name) : base(id, name) { }
    }

    private sealed class PaymentMethod : Enumeration
    {
        public static readonly PaymentMethod CreditCard = new(1, "CreditCard", "Credit / Debit Card");
        public static readonly PaymentMethod PayPal = new(2, "PayPal", "PayPal Account");
        public static readonly PaymentMethod Crypto = new(3, "Crypto", "Cryptocurrency Transfer");

        private PaymentMethod(int id, string name, string? displayName = null) : base(id, name, displayName) { }
    }

    [Fact]
    public void Constructor_SetsIdAndNameAndDisplayName()
    {
        var status = OrderStatus.Draft;

        Assert.Equal(1, status.Id);
        Assert.Equal("Draft", status.Name);
        Assert.Equal("Draft Order", status.DisplayName);
    }

    [Fact]
    public void ToString_ReturnsDisplayNameWhenPresent()
    {
        Assert.Equal("Draft Order", OrderStatus.Draft.ToString());
        Assert.Equal("Processing Order", OrderStatus.Processing.ToString());
    }

    [Fact]
    public void GetAll_ReturnsAllStaticDeclaredOptions()
    {
        var all = OrderStatus.GetAll<OrderStatus>().ToList();

        Assert.Equal(5, all.Count);
        Assert.Contains(OrderStatus.Draft, all);
        Assert.Contains(OrderStatus.Processing, all);
        Assert.Contains(OrderStatus.Shipped, all);
        Assert.Contains(OrderStatus.Completed, all);
        Assert.Contains(OrderStatus.Cancelled, all);
    }

    [Fact]
    public void GetAll_EmptyEnumeration_ReturnsEmpty()
    {
        var all = EmptyEnumeration.GetAll<EmptyEnumeration>().ToList();
        Assert.Empty(all);
    }

    [Fact]
    public void FromValue_ValidId_ReturnsExpectedInstance()
    {
        var status = Enumeration.FromValue<OrderStatus>(3);
        Assert.Same(OrderStatus.Shipped, status);
    }

    [Fact]
    public void FromValue_InvalidId_ThrowsEntityNotFoundException()
    {
        var ex = Assert.Throws<EntityNotFoundException>(() => Enumeration.FromValue<OrderStatus>(999));
        Assert.Equal("ERR_ENUMERATION_NOT_FOUND", ex.ErrorCode);
        Assert.Equal(ExceptionSeverity.Warning, ex.Severity);
    }

    [Fact]
    public void FromName_ExactName_ReturnsExpectedInstance()
    {
        var status = Enumeration.FromName<OrderStatus>("Draft");
        Assert.Same(OrderStatus.Draft, status);
    }

    [Fact]
    public void FromName_CaseInsensitive_ReturnsExpectedInstance()
    {
        var status = Enumeration.FromName<OrderStatus>("shipped", ignoreCase: true);
        Assert.Same(OrderStatus.Shipped, status);
    }

    [Fact]
    public void FromName_ByDisplayName_ReturnsExpectedInstance()
    {
        var status = Enumeration.FromName<OrderStatus>("Shipped to Customer");
        Assert.Same(OrderStatus.Shipped, status);
    }

    [Fact]
    public void FromName_InvalidName_ThrowsEntityNotFoundException()
    {
        var ex = Assert.Throws<EntityNotFoundException>(() => Enumeration.FromName<OrderStatus>("NonExistent"));
        Assert.Equal("ERR_ENUMERATION_NOT_FOUND", ex.ErrorCode);
    }

    [Fact]
    public void TryFromValue_ValidId_ReturnsTrueAndInstance()
    {
        var success = Enumeration.TryFromValue<OrderStatus>(4, out var result);

        Assert.True(success);
        Assert.Same(OrderStatus.Completed, result);
    }

    [Fact]
    public void TryFromValue_InvalidId_ReturnsFalseAndNull()
    {
        var success = Enumeration.TryFromValue<OrderStatus>(1234, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromName_ValidName_ReturnsTrueAndInstance()
    {
        var success = Enumeration.TryFromName<OrderStatus>("Cancelled", ignoreCase: true, out var result);

        Assert.True(success);
        Assert.Same(OrderStatus.Cancelled, result);
    }

    [Fact]
    public void TryFromName_InvalidName_ReturnsFalseAndNull()
    {
        var success = Enumeration.TryFromName<OrderStatus>("Invalid", ignoreCase: true, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var status = OrderStatus.Draft;
        Assert.True(status.Equals((object)status));
        Assert.True(status.Equals(status));
        Assert.True(status == status);
        Assert.False(status != status);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var status = OrderStatus.Draft;

        Assert.False(status.Equals((object?)null));
        Assert.False(status.Equals((OrderStatus?)null));
        Assert.False(status == null);
        Assert.False(null == status);
        Assert.True(status != null);
    }

    [Fact]
    public void Equals_DifferentEnumerationTypesWithSameId_ReturnsFalse()
    {
        var orderStatus = OrderStatus.Draft; // Id = 1
        var paymentMethod = PaymentMethod.CreditCard; // Id = 1

        Assert.False(orderStatus.Equals((object)paymentMethod));
    }

    [Fact]
    public void GetHashCode_SameIdAndType_ReturnsSameHashCode()
    {
        var fromValue = Enumeration.FromValue<OrderStatus>(1);
        Assert.Equal(OrderStatus.Draft.GetHashCode(), fromValue.GetHashCode());
    }

    [Fact]
    public void CompareTo_OrdersCorrectlyById()
    {
        Assert.True(OrderStatus.Draft.CompareTo(OrderStatus.Processing) < 0);
        Assert.True(OrderStatus.Processing.CompareTo(OrderStatus.Draft) > 0);
        Assert.True(OrderStatus.Draft.CompareTo(OrderStatus.Draft) == 0);
    }

    [Fact]
    public void RelationalOperators_WorkAsExpected()
    {
        Assert.True(OrderStatus.Draft < OrderStatus.Processing);
        Assert.True(OrderStatus.Draft <= OrderStatus.Processing);
        Assert.True(OrderStatus.Draft <= OrderStatus.Draft);
        Assert.True(OrderStatus.Completed > OrderStatus.Draft);
        Assert.True(OrderStatus.Completed >= OrderStatus.Draft);
        Assert.True(OrderStatus.Completed >= OrderStatus.Completed);
    }

    [Theory]
    [InlineData(1, "Draft")]
    [InlineData(2, "Processing")]
    [InlineData(3, "Shipped")]
    [InlineData(4, "Completed")]
    [InlineData(5, "Cancelled")]
    public void Parameterized_FromValueAndName(int id, string name)
    {
        var byId = Enumeration.FromValue<OrderStatus>(id);
        var byName = Enumeration.FromName<OrderStatus>(name);

        Assert.Same(byId, byName);
        Assert.Equal(id, byId.Id);
        Assert.Equal(name, byId.Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(100)]
    public void Parameterized_InvalidIds_Throw(int invalidId)
    {
        Assert.Throws<EntityNotFoundException>(() => Enumeration.FromValue<OrderStatus>(invalidId));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("DRAFT")]
    [InlineData("Draft")]
    [InlineData("dRaFt")]
    public void Parameterized_CaseInsensitiveMatching(string variation)
    {
        var result = Enumeration.FromName<OrderStatus>(variation, ignoreCase: true);
        Assert.Same(OrderStatus.Draft, result);
    }
}
