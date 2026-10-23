using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class ExtensibleEntityTests
{
    private sealed class ExtensibleCustomer : Entity<Guid>, IHasExtraProperties
    {
        public Dictionary<string, object?> ExtraProperties { get; set; } = new();

        public ExtensibleCustomer() : base() { }
        public ExtensibleCustomer(Guid id) : base(id) { }
    }

    private sealed class CustomMetadata
    {
        public string Tier { get; set; } = "Gold";
        public int LoyaltyPoints { get; set; } = 500;
    }

    #region SetProperty Tests

    [Fact]
    public void SetProperty_ValidKeyAndValue_StoresSuccessfullyAndReturnsFluently()
    {
        var customer = new ExtensibleCustomer(Guid.NewGuid());

        var returned = customer.SetProperty("PreferredLanguage", "en-US");

        Assert.Same(customer, returned);
        Assert.True(customer.ExtraProperties.ContainsKey("PreferredLanguage"));
        Assert.Equal("en-US", customer.ExtraProperties["PreferredLanguage"]);
    }

    [Fact]
    public void SetProperty_OverwriteExistingKey_UpdatesValue()
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty("Theme", "Light");
        Assert.Equal("Light", customer.ExtraProperties["Theme"]);

        customer.SetProperty("Theme", "Dark");
        Assert.Equal("Dark", customer.ExtraProperties["Theme"]);
    }

    [Fact]
    public void SetProperty_NullValue_IsPermitted()
    {
        var customer = new ExtensibleCustomer();

        customer.SetProperty("OptionalCode", null);

        Assert.True(customer.ExtraProperties.ContainsKey("OptionalCode"));
        Assert.Null(customer.ExtraProperties["OptionalCode"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  \r\n")]
    public void SetProperty_InvalidPropertyName_ThrowsArgumentException(string? invalidKey)
    {
        var customer = new ExtensibleCustomer();

        var ex = Assert.Throws<ArgumentException>(() =>
            customer.SetProperty(invalidKey!, "someValue"));

        Assert.Equal("name", ex.ParamName);
    }

    #endregion

    #region GetProperty<T> Tests

    [Theory]
    [InlineData("Key1", "TestString")]
    [InlineData("Key2", 12345)]
    [InlineData("Key3", true)]
    [InlineData("Key4", 99.95)]
    public void GetProperty_ExactTypeMatch_ReturnsTypedValue(string key, object value)
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty(key, value);

        if (value is string s)
            Assert.Equal(s, customer.GetProperty<string>(key));
        else if (value is int i)
            Assert.Equal(i, customer.GetProperty<int>(key));
        else if (value is bool b)
            Assert.Equal(b, customer.GetProperty<bool>(key));
        else if (value is double d)
            Assert.Equal(d, customer.GetProperty<double>(key));
    }

    [Fact]
    public void GetProperty_ComplexReferenceType_ReturnsTypedObject()
    {
        var customer = new ExtensibleCustomer();
        var metadata = new CustomMetadata { Tier = "Platinum", LoyaltyPoints = 1500 };

        customer.SetProperty("LoyaltyInfo", metadata);

        var retrieved = customer.GetProperty<CustomMetadata>("LoyaltyInfo");

        Assert.NotNull(retrieved);
        Assert.Equal("Platinum", retrieved.Tier);
        Assert.Equal(1500, retrieved.LoyaltyPoints);
    }

    [Fact]
    public void GetProperty_KeyDoesNotExist_ReturnsDefault()
    {
        var customer = new ExtensibleCustomer();

        var stringVal = customer.GetProperty<string>("NonExistentKey");
        var intVal = customer.GetProperty<int>("NonExistentKey");

        Assert.Null(stringVal);
        Assert.Equal(0, intVal);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("9999", 9999)]
    [InlineData("0", 0)]
    public void GetProperty_ConvertibleStringToInt_ConvertsSuccessfully(string stringValue, int expectedInt)
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty("NumericString", stringValue);

        var converted = customer.GetProperty<int>("NumericString");

        Assert.Equal(expectedInt, converted);
    }

    [Fact]
    public void GetProperty_IntToLongConversion_ConvertsSuccessfully()
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty("SmallNumber", 42);

        var longVal = customer.GetProperty<long>("SmallNumber");

        Assert.Equal(42L, longVal);
    }

    [Fact]
    public void GetProperty_IncompatibleTypeConversion_ReturnsDefaultWithoutThrowing()
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty("InvalidInt", "This is not a number!");

        var result = customer.GetProperty<int>("InvalidInt");

        Assert.Equal(0, result);
    }

    #endregion

    #region RemoveProperty Tests

    [Fact]
    public void RemoveProperty_WhenKeyExists_RemovesKeyAndReturnsTrue()
    {
        var customer = new ExtensibleCustomer();
        customer.SetProperty("Flag", true);

        var removed = customer.RemoveProperty("Flag");

        Assert.True(removed);
        Assert.False(customer.ExtraProperties.ContainsKey("Flag"));
        Assert.False(customer.GetProperty<bool>("Flag"));
    }

    [Fact]
    public void RemoveProperty_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var customer = new ExtensibleCustomer();

        var removed = customer.RemoveProperty("UnknownKey");

        Assert.False(removed);
    }

    #endregion

    #region Null Dictionary Resilience

    private sealed class EntityWithNullDictionary : IHasExtraProperties
    {
        public Dictionary<string, object?> ExtraProperties => null!;
    }

    [Fact]
    public void GetProperty_WhenExtraPropertiesIsNull_ReturnsDefault()
    {
        var entity = new EntityWithNullDictionary();

        var result = entity.GetProperty<string>("AnyKey");

        Assert.Null(result);
    }

    [Fact]
    public void RemoveProperty_WhenExtraPropertiesIsNull_ReturnsFalse()
    {
        var entity = new EntityWithNullDictionary();

        var result = entity.RemoveProperty("AnyKey");

        Assert.False(result);
    }

    #endregion

    #region FullAuditableEntity Extensibility Integration

    private sealed class AuditedExtensibleOrder : FullAuditableEntity
    {
        public AuditedExtensibleOrder() : base() { }
        public AuditedExtensibleOrder(Guid id) : base(id) { }
    }

    [Fact]
    public void FullAuditableEntity_SupportsDynamicExtraPropertiesSeamlessly()
    {
        var order = new AuditedExtensibleOrder(Guid.NewGuid());
        order.SetCreation("system-worker");

        order.SetProperty("TrackingNumber", "TRK-987654321")
             .SetProperty("InternalPriority", 10);

        Assert.Equal("TRK-987654321", order.GetProperty<string>("TrackingNumber"));
        Assert.Equal(10, order.GetProperty<int>("InternalPriority"));

        order.RemoveProperty("InternalPriority");
        Assert.Equal(0, order.GetProperty<int>("InternalPriority"));
    }

    #endregion
}
