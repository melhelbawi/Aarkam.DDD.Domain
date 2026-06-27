namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines the fundamental contract for a Domain Entity with a strongly-typed unique identifier.
/// </summary>
/// <typeparam name="TKey">The type of the identity key for the entity. Must support equality comparison.</typeparam>
/// <remarks>
/// In Domain-Driven Design, an Entity is not defined by its attributes, but by a thread of continuity and identity.
/// This interface ensures that all domain entities expose their identity uniformly.
/// </remarks>
public interface IEntity<out TKey>
{
    TKey Id { get; }
}

/// <summary>
/// Provides a base class implementation for Domain Entities, encapsulating identity equality semantics 
/// and ensuring compatibility with Object-Relational Mappers (like EF Core).
/// </summary>
/// <typeparam name="TKey">The type used for the entity's primary identity key.</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the unique identity for this entity.
    /// </summary>
    /// <value>The identity value. The protected setter allows ORMs to materialize state while shielding the identity from external manipulation.</value>
    public virtual TKey Id { get; protected set; } = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TKey}"/> class.
    /// </summary>
    /// <remarks>
    /// Required for ORM change tracking proxies, lazy-loading materialization, and database-generated identities (e.g., auto-incrementing integers).
    /// </remarks>
    protected Entity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TKey}"/> class with an explicitly provided, client-generated identity.
    /// </summary>
    /// <param name="id">The predefined unique identifier for the entity.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="id"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided <paramref name="id"/> evaluates to its type's default value.</exception>
    protected Entity(TKey id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id), "The entity identity cannot be null.");
        }

        if (EqualityComparer<TKey>.Default.Equals(id, default))
        {
            throw new ArgumentException("An explicit identity cannot be assigned a default type value.", nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// Determines whether the entity instance is transient (not yet persisted and lacking a valid identity).
    /// </summary>
    /// <returns><c>true</c> if the identity matches the type's default state; otherwise, <c>false</c>.</returns>
    public virtual bool IsTransient()
    {
        return EqualityComparer<TKey>.Default.Equals(Id, default);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity based on domain identity rules.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns><c>true</c> if the specified object is an entity of the exact same type with an identical key; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
        {
            return false;
        }

        return Equals(other);
    }

    /// <summary>
    /// Strong-typed equality comparison checking domain identity constraints.
    /// </summary>
    /// <param name="other">The other entity instance to compare.</param>
    /// <returns><c>true</c> if the entities share the same type and persistent identity; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Two transient entities (unpersisted, with default IDs) are never considered equal by identity, 
    /// as they do not yet possess a persistent thread of continuity.
    /// </remarks>
    public virtual bool Equals(Entity<TKey>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (IsTransient() || other.IsTransient()) return false;

        return Id!.Equals(other.Id);
    }

    /// <summary>
    /// Serves as the default hash function, bound to the entity identity to guarantee consistent behavior within hashed collections.
    /// </summary>
    /// <returns>A hash code derived from the entity identity, or the base hash code if the entity is transient.</returns>
    public override int GetHashCode()
    {
        return IsTransient() ? base.GetHashCode() : Id!.GetHashCode();
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) => Equals(left, right);
    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !Equals(left, right);

    /// <summary>
    /// Centralized factory helper for generating new unique identity values within the application layer.
    /// </summary>
    /// <returns>A newly instantiated unique identifier of type <typeparamref name="TKey"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when automatic factory generation is unsupported for the requested key type.</exception>
    protected static TKey GenerateNewId()
    {
        return typeof(TKey) switch
        {
            { } t when t == typeof(Guid)
                => (TKey)(object)Guid.NewGuid(),

            { } t when t == typeof(string)
                => (TKey)(object)Guid.NewGuid().ToString("N"),

            _
                => throw new InvalidOperationException(
                    $"Automatic identity generation is not configured for key type '{typeof(TKey).Name}'. " +
                    "Use an explicit-identity constructor, or rely on database-driven store sequencing.")
        };
    }
}

/// <summary>
/// A convenient base abstract implementation prioritizing <see cref="Guid"/>-based identities for standard domain models.
/// </summary>
public abstract class Entity : Entity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the standard <see cref="Entity"/> class.
    /// </summary>
    protected Entity() : base() { }

    /// <summary>
    /// Initializes a new instance of the standard <see cref="Entity"/> class with a predefined client/domain generated <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The unique identity assigned to this entity domain instance.</param>
    protected Entity(Guid id) : base(id) { }
}