using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class AuditingTests
{
    private sealed class TestCreationEntity : CreationAuditableEntity<Guid>
    {
        public TestCreationEntity() : base() { }
        public TestCreationEntity(Guid id) : base(id) { }
    }

    private sealed class TestModificationEntity : ModificationAuditableEntity<int>
    {
        public TestModificationEntity() : base() { }
        public TestModificationEntity(int id) : base(id) { }
    }

    private sealed class TestDeletionEntity : DeletionAuditableEntity<string>
    {
        public TestDeletionEntity() : base() { }
        public TestDeletionEntity(string id) : base(id) { }
    }

    private sealed class TestFullAuditableEntity : FullAuditableEntity<Guid>
    {
        public TestFullAuditableEntity() : base() { }
        public TestFullAuditableEntity(Guid id) : base(id) { }
    }

    private sealed class DefaultFullAuditableEntity : FullAuditableEntity
    {
        public DefaultFullAuditableEntity() : base() { }
        public DefaultFullAuditableEntity(Guid id) : base(id) { }
    }

    #region Creation Auditing Tests

    [Fact]
    public void CreationAuditing_InitialState_HasDefaultProperties()
    {
        var entity = new TestCreationEntity();

        Assert.Null(entity.CreatorId);
        Assert.Equal(default, entity.CreationTime);
    }

    [Fact]
    public void SetCreation_ValidCreator_SetsCreatorIdAndCreationTime()
    {
        var entity = new TestCreationEntity(Guid.NewGuid());
        var before = DateTimeOffset.UtcNow;

        var returned = entity.SetCreation("user-admin-123");
        var after = DateTimeOffset.UtcNow;

        Assert.Same(entity, returned);
        Assert.Equal("user-admin-123", entity.CreatorId);
        Assert.True(entity.CreationTime >= before && entity.CreationTime <= after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  ")]
    public void SetCreation_InvalidCreator_ThrowsArgumentException(string? invalidCreatorId)
    {
        var entity = new TestCreationEntity();

        var ex = Assert.Throws<ArgumentException>(() =>
            entity.SetCreation(invalidCreatorId!));

        Assert.Equal("creatorId", ex.ParamName);
    }

    #endregion

    #region Modification Auditing Tests

    [Fact]
    public void ModificationAuditing_InitialState_HasNullModificationProperties()
    {
        var entity = new TestModificationEntity(10);

        Assert.Null(entity.ModifierId);
        Assert.Null(entity.LastModificationTime);
    }

    [Fact]
    public void SetModification_ValidModifier_SetsModifierIdAndLastModificationTime()
    {
        var entity = new TestModificationEntity(10);
        entity.SetCreation("creator-001");

        var before = DateTimeOffset.UtcNow;
        var returned = entity.SetModification("modifier-999");
        var after = DateTimeOffset.UtcNow;

        Assert.Same(entity, returned);
        Assert.Equal("modifier-999", entity.ModifierId);
        Assert.NotNull(entity.LastModificationTime);
        Assert.True(entity.LastModificationTime >= before && entity.LastModificationTime <= after);

        // Creator information remains untampered
        Assert.Equal("creator-001", entity.CreatorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \r\n  ")]
    public void SetModification_InvalidModifier_ThrowsArgumentException(string? invalidModifierId)
    {
        var entity = new TestModificationEntity();

        var ex = Assert.Throws<ArgumentException>(() =>
            entity.SetModification(invalidModifierId!));

        Assert.Equal("modifierId", ex.ParamName);
    }

    #endregion

    #region Deletion Auditing Tests

    [Fact]
    public void DeletionAuditing_InitialState_IsNotDeleted()
    {
        var entity = new TestDeletionEntity("del-entity-1");

        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeleterId);
        Assert.Null(entity.DeletionTime);
    }

    [Fact]
    public void SetSoftDeletion_ValidDeleter_SetsIsDeletedAndAuditMetadata()
    {
        var entity = new TestDeletionEntity("del-entity-1");
        var before = DateTimeOffset.UtcNow;

        var returned = entity.SetSoftDeletion("deleter-777");
        var after = DateTimeOffset.UtcNow;

        Assert.Same(entity, returned);
        Assert.True(entity.IsDeleted);
        Assert.Equal("deleter-777", entity.DeleterId);
        Assert.NotNull(entity.DeletionTime);
        Assert.True(entity.DeletionTime >= before && entity.DeletionTime <= after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t ")]
    public void SetSoftDeletion_InvalidDeleter_ThrowsArgumentException(string? invalidDeleterId)
    {
        var entity = new TestDeletionEntity("del-entity-1");

        var ex = Assert.Throws<ArgumentException>(() =>
            entity.SetSoftDeletion(invalidDeleterId!));

        Assert.Equal("deleterId", ex.ParamName);
    }

    [Fact]
    public void Restore_ResetsIsDeletedAndClearsDeletionMetadata()
    {
        var entity = new TestDeletionEntity("del-entity-1");
        entity.SetSoftDeletion("deleter-777");

        var returned = entity.Restore();

        Assert.Same(entity, returned);
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeleterId);
        Assert.Null(entity.DeletionTime);
    }

    [Fact]
    public void DeletionAuditing_MultipleSoftDeleteAndRestoreCycles_OperatesCleanly()
    {
        var entity = new TestDeletionEntity("cycle-entity");

        entity.SetSoftDeletion("user1");
        Assert.True(entity.IsDeleted);
        Assert.Equal("user1", entity.DeleterId);

        entity.Restore();
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeleterId);

        entity.SetSoftDeletion("user2");
        Assert.True(entity.IsDeleted);
        Assert.Equal("user2", entity.DeleterId);
    }

    #endregion

    #region Full Auditable Entity Tests

    [Fact]
    public void FullAuditableEntity_ImplementsAllAuditingInterfaces()
    {
        var entity = new TestFullAuditableEntity(Guid.NewGuid());

        Assert.IsAssignableFrom<IFullAuditableEntity>(entity);
        Assert.IsAssignableFrom<IDeletionAuditableEntity>(entity);
        Assert.IsAssignableFrom<IModificationAuditableEntity>(entity);
        Assert.IsAssignableFrom<ICreationAuditableEntity>(entity);
        Assert.IsAssignableFrom<ISoftDelete>(entity);
        Assert.IsAssignableFrom<IHasExtraProperties>(entity);
    }

    [Fact]
    public void FullAuditableEntity_ExtraProperties_IsInitializedEmpty()
    {
        var entity = new TestFullAuditableEntity();

        Assert.NotNull(entity.ExtraProperties);
        Assert.Empty(entity.ExtraProperties);
    }

    [Fact]
    public void FullAuditableEntity_ExecutesFullLifecycleCycle()
    {
        var id = Guid.NewGuid();
        var entity = new DefaultFullAuditableEntity(id);

        Assert.Equal(id, entity.Id);
        Assert.False(entity.IsTransient());

        // 1. Create
        entity.SetCreation("user-creator");
        Assert.Equal("user-creator", entity.CreatorId);

        // 2. Modify
        entity.SetModification("user-modifier");
        Assert.Equal("user-modifier", entity.ModifierId);
        Assert.NotNull(entity.LastModificationTime);

        // 3. Soft Delete
        entity.SetSoftDeletion("user-deleter");
        Assert.True(entity.IsDeleted);
        Assert.Equal("user-deleter", entity.DeleterId);

        // 4. Restore
        entity.Restore();
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeleterId);
        Assert.Null(entity.DeletionTime);

        // 5. Modify After Restore
        entity.SetModification("user-modifier-2");
        Assert.Equal("user-modifier-2", entity.ModifierId);
    }

    [Fact]
    public void DefaultFullAuditableEntity_ParameterlessConstructor_CreatesTransientEntity()
    {
        var entity = new DefaultFullAuditableEntity();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.True(entity.IsTransient());
    }

    #endregion
}
