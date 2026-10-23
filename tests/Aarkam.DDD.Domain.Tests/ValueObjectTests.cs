using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string? PostalCode { get; }

        public Address(string street, string city, string? postalCode = null)
        {
            Street = street;
            City = city;
            PostalCode = postalCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return PostalCode;
        }
    }

    private sealed class CompositeValueObject : ValueObject
    {
        public string Label { get; }
        public Money Price { get; }

        public CompositeValueObject(string label, Money price)
        {
            Label = label;
            Price = price;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Label;
            yield return Price;
        }
    }

    private sealed class CollectionValueObject : ValueObject
    {
        public string Title { get; }
        public IReadOnlyList<string> Tags { get; }

        public CollectionValueObject(string title, IEnumerable<string> tags)
        {
            Title = title;
            Tags = tags.ToList();
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Title;
            foreach (var tag in Tags)
            {
                yield return tag;
            }
        }
    }

    [Fact]
    public void Equals_IdenticalComponents_ReturnsTrue()
    {
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        Assert.True(money1.Equals((object)money2));
        Assert.True(money1.Equals(money2));
        Assert.True(money1 == money2);
        Assert.False(money1 != money2);
    }

    [Fact]
    public void Equals_DifferentAmounts_ReturnsFalse()
    {
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(200.00m, "USD");

        Assert.False(money1.Equals((object)money2));
        Assert.False(money1.Equals(money2));
        Assert.False(money1 == money2);
        Assert.True(money1 != money2);
    }

    [Fact]
    public void Equals_DifferentCurrencies_ReturnsFalse()
    {
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "EUR");

        Assert.False(money1.Equals((object)money2));
        Assert.False(money1 == money2);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var money = new Money(50m, "EGP");
        Assert.True(money.Equals((object)money));
        Assert.True(money.Equals(money));
        Assert.True(money == money);
    }

    [Fact]
    public void Equals_NullComparison_ReturnsFalse()
    {
        var money = new Money(50m, "EGP");

        Assert.False(money.Equals((object?)null));
        Assert.False(money.Equals((Money?)null));
        Assert.False(money == null);
        Assert.False(null == money);
        Assert.True(money != null);
        Assert.True(null != money);
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        Money? left = null;
        Money? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Equals_DifferentTypes_ReturnsFalse()
    {
        var money = new Money(100m, "USD");
        var address = new Address("Main St", "City");

        Assert.False(money.Equals(address));
    }

    [Fact]
    public void Equals_NullableComponentsMatch_ReturnsTrue()
    {
        var addr1 = new Address("Road 9", "Cairo", null);
        var addr2 = new Address("Road 9", "Cairo", null);

        Assert.True(addr1 == addr2);
        Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());
    }

    [Fact]
    public void Equals_OneNullableComponentNullOneProvided_ReturnsFalse()
    {
        var addr1 = new Address("Road 9", "Cairo", null);
        var addr2 = new Address("Road 9", "Cairo", "11511");

        Assert.False(addr1 == addr2);
        Assert.True(addr1 != addr2);
    }

    [Fact]
    public void GetHashCode_IdenticalComponents_ProduceSameHashCode()
    {
        var money1 = new Money(42m, "GBP");
        var money2 = new Money(42m, "GBP");

        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentComponents_ProduceDifferentHashCode()
    {
        var money1 = new Money(42m, "GBP");
        var money2 = new Money(43m, "GBP");

        Assert.NotEqual(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void CompositeValueObject_MatchesWhenNestedValueObjectsMatch()
    {
        var comp1 = new CompositeValueObject("Product A", new Money(99.99m, "USD"));
        var comp2 = new CompositeValueObject("Product A", new Money(99.99m, "USD"));
        var comp3 = new CompositeValueObject("Product A", new Money(88.88m, "USD"));

        Assert.True(comp1 == comp2);
        Assert.False(comp1 == comp3);
        Assert.Equal(comp1.GetHashCode(), comp2.GetHashCode());
    }

    [Fact]
    public void CollectionValueObject_MatchesWhenSequenceIsIdentical()
    {
        var col1 = new CollectionValueObject("Doc", new[] { "tag1", "tag2", "tag3" });
        var col2 = new CollectionValueObject("Doc", new[] { "tag1", "tag2", "tag3" });
        var col3 = new CollectionValueObject("Doc", new[] { "tag1", "tag3", "tag2" }); // different order

        Assert.True(col1 == col2);
        Assert.False(col1 == col3);
        Assert.Equal(col1.GetHashCode(), col2.GetHashCode());
    }

    [Theory]
    [InlineData(10, "USD", 10, "USD", true)]
    [InlineData(10, "USD", 20, "USD", false)]
    [InlineData(10, "USD", 10, "EUR", false)]
    [InlineData(0, "USD", 0, "USD", true)]
    [InlineData(-10, "USD", -10, "USD", true)]
    [InlineData(-10, "USD", 10, "USD", false)]
    [InlineData(999999.99, "SAR", 999999.99, "SAR", true)]
    public void Money_ParameterizedEquality(decimal amount1, string curr1, decimal amount2, string curr2, bool expectedEqual)
    {
        var m1 = new Money(amount1, curr1);
        var m2 = new Money(amount2, curr2);

        Assert.Equal(expectedEqual, m1 == m2);
        Assert.Equal(!expectedEqual, m1 != m2);
        Assert.Equal(expectedEqual, m1.Equals(m2));
        if (expectedEqual)
        {
            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
        }
    }

    [Theory]
    [InlineData("123 Street", "Cairo", "11511", "123 Street", "Cairo", "11511", true)]
    [InlineData("123 Street", "Cairo", "11511", "124 Street", "Cairo", "11511", false)]
    [InlineData("123 Street", "Cairo", "11511", "123 Street", "Giza", "11511", false)]
    [InlineData("123 Street", "Cairo", "11511", "123 Street", "Cairo", "11522", false)]
    [InlineData("Main Ave", "Alexandria", null, "Main Ave", "Alexandria", null, true)]
    public void Address_ParameterizedEquality(
        string s1, string c1, string? p1,
        string s2, string c2, string? p2,
        bool expectedEqual)
    {
        var a1 = new Address(s1, c1, p1);
        var a2 = new Address(s2, c2, p2);

        Assert.Equal(expectedEqual, a1 == a2);
        Assert.Equal(!expectedEqual, a1 != a2);
    }
}
