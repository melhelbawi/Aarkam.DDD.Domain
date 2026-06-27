using System.ComponentModel.DataAnnotations;

namespace Aarkam.DDD.Domain.Entities
{
    /// <summary>
    /// Defines the contract that requires tracking of its creation metadata.
    /// This is a fundamental auditing concern within the Domain Layer to ensure historical traceability.
    /// </summary>
    /// <remarks>
    /// Implement this interface on Aggregate Roots or Entities where knowing "who" created it and "when" 
    /// is a strict business or regulatory requirement. The domain framework typically orchestrates the 
    /// population of these properties automatically during the entity's persistence lifecycle.
    /// </remarks>
    public interface ICreationAuditableEntity
    {
        /// <summary>
        /// Gets the unique identifier of the user or actor who created this entity.
        /// </summary>
        /// <value>
        /// A string representing the creator's identity, bounded to a maximum length of 155 characters 
        /// to ensure database portability.
        /// </value>
        [MaxLength(155)]
        string CreatorId { get; }

        /// <summary>
        /// Gets the date and time when this entity was materialized into the domain.
        /// </summary>
        /// <value>
        /// A <see cref="DateTimeOffset"/> representing the exact point in time of creation, 
        /// inherently preserving timezone context (ideally captured in UTC).
        /// </value>
        DateTimeOffset CreationTime { get; }

        /// <summary>
        /// Initializes the creation audit metadata for the entity.
        /// </summary>
        /// <param name="creatorId">The unique identifier of the actor initiating the creation.</param>
        /// <returns>The current <see cref="ICreationAuditableEntity"/> instance to support fluent configuration.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="creatorId"/> is null or empty.</exception>
        ICreationAuditableEntity SetCreation(string creatorId);
    }

    /// <summary>
    /// A base abstract implementation of an <see cref="IEntity{TKey}"/> that includes creation auditing capabilities.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key for the entity.</typeparam>
    /// <remarks>
    /// Inheriting from this class encapsulates the auditing behavior, preventing boilerplate code duplication 
    /// across your Domain Model. It enforces encapsulation by keeping setter accessors private to the domain boundary.
    /// </remarks>
    public abstract class CreationAuditableEntity<TKey> : Entity<TKey>, ICreationAuditableEntity where TKey : IEquatable<TKey>
    {
        /// <inheritdoc />
        [MaxLength(155)]
        public string CreatorId { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset CreationTime { get; private set; }

        /// <summary>
        /// Encapsulates the domain logic for establishing the entity's creation audit state.
        /// </summary>
        /// <param name="creatorId">The identity of the creator.</param>
        /// <returns>The entity instance for fluent chaining.</returns>
        public virtual ICreationAuditableEntity SetCreation(string creatorId)
        {
            // Enforce basic invariant protection within the domain method
            if (string.IsNullOrWhiteSpace(creatorId))
            {
                throw new ArgumentException("Creator identifier cannot be null or white space.", nameof(creatorId));
            }

            CreatorId = creatorId;
            CreationTime = DateTimeOffset.UtcNow; // DateTimeOffset.UtcNow is preferred over DateTime.UtcNow for DateTimeOffset types
            return this;
        }
    }
}
