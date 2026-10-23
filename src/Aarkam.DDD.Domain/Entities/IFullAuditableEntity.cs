namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines a comprehensive lifecycle auditing contract for domain entities, uniting creation, modification,
/// and soft-deletion tracking into a single unified interface.
/// </summary>
public interface IFullAuditableEntity : IDeletionAuditableEntity
{
}

/// <summary>
/// Strongly-typed generic contract for full lifecycle auditable domain entities.
/// </summary>
/// <typeparam name="TKey">The identity type mapping back to the underlying <see cref="IEntity{TKey}"/> contract.</typeparam>
public interface IFullAuditableEntity<out TKey> : IFullAuditableEntity, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// A base abstract implementation providing complete creation, modification, and soft-deletion auditing
/// along with dynamic extra property extensibility.
/// </summary>
/// <typeparam name="TKey">The primary key type designating the identity boundary of the entity.</typeparam>
public abstract class FullAuditableEntity<TKey> : DeletionAuditableEntity<TKey>, IFullAuditableEntity<TKey>, IHasExtraProperties
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc />
    public virtual Dictionary<string, object?> ExtraProperties { get; protected set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FullAuditableEntity{TKey}"/> class.
    /// </summary>
    protected FullAuditableEntity() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FullAuditableEntity{TKey}"/> class with an explicit identity.
    /// </summary>
    /// <param name="id">The unique identifier to assign to this entity.</param>
    protected FullAuditableEntity(TKey id) : base(id)
    {
    }
}

/// <summary>
/// A convenient base abstract wrapper for full lifecycle auditable entities using standard <see cref="Guid"/> identities.
/// </summary>
public abstract class FullAuditableEntity : FullAuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the standard <see cref="FullAuditableEntity"/> class.
    /// </summary>
    protected FullAuditableEntity() : base() { }

    /// <summary>
    /// Initializes a new instance of the standard <see cref="FullAuditableEntity"/> class with a predefined client/domain generated <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The unique identity assigned to this entity domain instance.</param>
    protected FullAuditableEntity(Guid id) : base(id) { }
}
