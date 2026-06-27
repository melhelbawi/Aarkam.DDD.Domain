using System.ComponentModel.DataAnnotations;

namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines a simple contract to determine if an entity supports a logical/soft deletion state within the domain layer.
/// Used primarily by data access interceptors to apply global query filters.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets a value indicating whether this entity has been logically deleted from the active domain state.
    /// </summary>
    bool IsDeleted { get; }
}

/// <summary>
/// Defines the contract for an entity that requires tracking of its logical deletion metadata, completing the full lifecycle audit capabilities.
/// </summary>
/// <remarks>
/// Soft deletion ensures that records are preserved for historical audit or regulatory purposes rather than being physically purged from the persistent store.
/// </remarks>
public interface IDeletionAuditableEntity : IModificationAuditableEntity, ISoftDelete
{
    /// <summary>
    /// Gets the unique identifier of the user or actor who logically deleted this entity.
    /// </summary>
    /// <value>A string representing the deleter's identity, or <see langword="null"/> if the entity is active.</value>
    [MaxLength(155)]
    string? DeleterId { get; }

    /// <summary>
    /// Gets the date and time when the entity was logically deleted.
    /// </summary>
    /// <value>A timezone-aware <see cref="DateTimeOffset"/> pointing to the exact moment of deletion, or <see langword="null"/> if active.</value>
    DateTimeOffset? DeletionTime { get; }

    /// <summary>
    /// Transitions the entity into a logically deleted state and records the associated audit metadata.
    /// </summary>
    /// <param name="deleterId">The unique identifier of the actor executing the deletion.</param>
    /// <returns>The current <see cref="IDeletionAuditableEntity"/> instance for operational chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="deleterId"/> is null or empty.</exception>
    IDeletionAuditableEntity SetSoftDeletion(string deleterId);

    /// <summary>
    /// Restores the logically deleted entity back into an active state, purging its historical deletion metadata.
    /// </summary>
    /// <returns>The current <see cref="IDeletionAuditableEntity"/> instance for operational chaining.</returns>
    IDeletionAuditableEntity Restore();
}

/// <summary>
/// A base abstract implementation of a domain entity encapsulating creation, modification, and soft-deletion auditing capabilities.
/// </summary>
/// <typeparam name="TKey">The primary key type specifying the identity boundary of the entity.</typeparam>
public abstract class DeletionAuditableEntity<TKey> : ModificationAuditableEntity<TKey>, IDeletionAuditableEntity where TKey : IEquatable<TKey>
{
    /// <inheritdoc />
    [MaxLength(155)]
    public string? DeleterId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletionTime { get; private set; }

    /// <inheritdoc />
    /// <value>The protected setter allows framework infrastructure to flag deletion status without breaking domain encapsulation.</value>
    public bool IsDeleted { get; protected set; } = false;

    /// <summary>
    /// Encapsulates the domain state transition for soft deleting this entity.
    /// </summary>
    /// <param name="deleterId">The identity of the actor driving the deletion.</param>
    /// <returns>The entity instance for operational chaining.</returns>
    public virtual IDeletionAuditableEntity SetSoftDeletion(string deleterId)
    {
        if (string.IsNullOrWhiteSpace(deleterId))
        {
            throw new ArgumentException("Deleter identifier cannot be null or white space.", nameof(deleterId));
        }

        IsDeleted = true;
        DeleterId = deleterId;
        DeletionTime = DateTimeOffset.UtcNow;

        return this;
    }

    /// <summary>
    /// Encapsulates the domain state transition for reversing a soft deletion, resurrecting the entity back into active boundaries.
    /// </summary>
    /// <returns>The entity instance for operational chaining.</returns>
    /// <remarks>
    /// This method resets the deletion markers to their default null states. Tracking who restored the entity 
    /// should naturally flow through the <see cref="ModificationAuditableEntity{TKey}.SetModification"/> method.
    /// </remarks>
    public virtual IDeletionAuditableEntity Restore()
    {
        IsDeleted = false;
        DeleterId = null;
        DeletionTime = null;

        return this;
    }
}