using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class EntityTests
{
    private sealed class TestGuidEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestGuidEntity() : base() { }
        public TestGuidEntity(Guid id) : base(id) { }

        public static Guid CreateNewId() => GenerateNewId();
    }

    private sealed class TestIntEntity : Entity<int>
    {
        public TestIntEntity() : base() { }
        public TestIntEntity(int id) : base(id) { }
    }

    private sealed class TestStringEntity : Entity<string>
    {
        public TestStringEntity() : base() { }
        public TestStringEntity(string id) : base(id) { }

        public static string CreateNewId() => GenerateNewId();
    }

    private sealed class DefaultGuidEntity : Entity
    {
        public DefaultGuidEntity() : base() { }
        public DefaultGuidEntity(Guid id) : base(id) { }
    }

    private sealed class AnotherEntityWithSameId : Entity<Guid>
    {
        public AnotherEntityWithSameId(Guid id) : base(id) { }
    }

    [Fact]
    public void Constructor_Default_CreatesTransientEntityWithDefaultId()
    {
        var entity = new TestGuidEntity();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.True(entity.IsTransient());
    }

    [Fact]
    public void Constructor_WithExplicitGuid_SetsIdAndIsNotTransient()
    {
        var id = Guid.NewGuid();
        var entity = new TestGuidEntity(id);

        Assert.Equal(id, entity.Id);
        Assert.False(entity.IsTransient());
    }

    [Fact]
    public void Constructor_WithNullStringId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestStringEntity(null!));
    }

    [Fact]
    public void Constructor_WithDefaultGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TestGuidEntity(Guid.Empty));
    }

    [Fact]
    public void Constructor_WithDefaultInt_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TestIntEntity(0));
    }

    [Fact]
    public void NonGenericEntity_DefaultConstructor_SetsDefaultGuidId()
    {
        var entity = new DefaultGuidEntity();
        Assert.Equal(Guid.Empty, entity.Id);
        Assert.True(entity.IsTransient());
    }

    [Fact]
    public void NonGenericEntity_WithExplicitGuid_SetsIdCorrectly()
    {
        var id = Guid.NewGuid();
        var entity = new DefaultGuidEntity(id);
        Assert.Equal(id, entity.Id);
        Assert.False(entity.IsTransient());
    }

    [Fact]
    public void GetKeys_SingleKeyEntity_ReturnsArrayContainingId()
    {
        var id = Guid.NewGuid();
        var entity = new TestGuidEntity(id);

        IEntity nonGeneric = entity;
        var keys = nonGeneric.GetKeys();

        Assert.NotNull(keys);
        Assert.Single(keys);
        Assert.Equal(id, keys[0]);
    }

    [Fact]
    public void GenerateNewId_Guid_ProducesNonEmptyGuid()
    {
        var id = TestGuidEntity.CreateNewId();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void GenerateNewId_String_ProducesNonEmptyString()
    {
        var id = TestStringEntity.CreateNewId();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.Equal(32, id.Length); // "N" format Guid
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestGuidEntity(Guid.NewGuid());
        Assert.True(entity.Equals((object)entity));
        Assert.True(entity.Equals(entity));
        Assert.True(entity == entity);
        Assert.False(entity != entity);
    }

    [Fact]
    public void Equals_NullComparison_ReturnsFalse()
    {
        var entity = new TestGuidEntity(Guid.NewGuid());

        Assert.False(entity.Equals((object?)null));
        Assert.False(entity.Equals((TestGuidEntity?)null));
        Assert.False(entity == null);
        Assert.False(null == entity);
        Assert.True(entity != null);
        Assert.True(null != entity);
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        TestGuidEntity? left = null;
        TestGuidEntity? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Equals_SameTypeSameId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestGuidEntity(id) { Name = "First" };
        var entity2 = new TestGuidEntity(id) { Name = "Second" };

        Assert.True(entity1.Equals((object)entity2));
        Assert.True(entity1.Equals(entity2));
        Assert.True(entity1 == entity2);
        Assert.False(entity1 != entity2);
    }

    [Fact]
    public void Equals_SameTypeDifferentId_ReturnsFalse()
    {
        var entity1 = new TestGuidEntity(Guid.NewGuid());
        var entity2 = new TestGuidEntity(Guid.NewGuid());

        Assert.False(entity1.Equals((object)entity2));
        Assert.False(entity1.Equals(entity2));
        Assert.False(entity1 == entity2);
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void Equals_DifferentTypeSameId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestGuidEntity(id);
        var entity2 = new AnotherEntityWithSameId(id);

        Assert.False(entity1.Equals((object)entity2));
        Assert.False(entity1.Equals(entity2 as dynamic));
    }

    [Fact]
    public void Equals_BothTransient_ReturnsFalse()
    {
        var entity1 = new TestGuidEntity();
        var entity2 = new TestGuidEntity();

        Assert.False(entity1.Equals((object)entity2));
        Assert.False(entity1.Equals(entity2));
        Assert.False(entity1 == entity2);
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void Equals_OneTransientOnePersisted_ReturnsFalse()
    {
        var entity1 = new TestGuidEntity();
        var entity2 = new TestGuidEntity(Guid.NewGuid());

        Assert.False(entity1.Equals(entity2));
        Assert.False(entity2.Equals(entity1));
        Assert.False(entity1 == entity2);
    }

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHashCode()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestGuidEntity(id);
        var entity2 = new TestGuidEntity(id);

        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_TransientEntity_DoesNotThrow()
    {
        var entity = new TestGuidEntity();
        var hash = entity.GetHashCode();
        Assert.NotEqual(0, hash);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(99999)]
    [InlineData(-5)]
    public void IntegerKeyEntity_EqualityTests(int id)
    {
        var entity1 = new TestIntEntity(id);
        var entity2 = new TestIntEntity(id);
        var entity3 = new TestIntEntity(id + 1);

        Assert.True(entity1.Equals(entity2));
        Assert.False(entity1.Equals(entity3));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        Assert.False(entity1.IsTransient());
    }

    [Theory]
    [InlineData("ORDER_001")]
    [InlineData("USER_ABC")]
    [InlineData("CUSTOM_SLUG_123")]
    [InlineData("TENANT_99")]
    public void StringKeyEntity_EqualityTests(string id)
    {
        var entity1 = new TestStringEntity(id);
        var entity2 = new TestStringEntity(id);
        var entity3 = new TestStringEntity(id + "_DIFFERENT");

        Assert.True(entity1.Equals(entity2));
        Assert.False(entity1.Equals(entity3));
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        Assert.False(entity1.IsTransient());
    }
}
