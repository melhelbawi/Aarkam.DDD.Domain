using System.ComponentModel.DataAnnotations;

namespace Aarkam.DDD.Domain.Entities
{
    /// <summary>
    /// Defines the contract for a domain entity that requires tracking of its operational modification history, 
    /// building upon creation tracking to fulfill full lifecycle auditing.
    /// </summary>
    /// <remarks>
    /// This contract represents an evolutionary stage in your entity lifecycle auditing. While creation auditing 
    /// records immutable genesis details, modification auditing captures mutable state updates. The framework or 
    /// application services utilize this contract to guarantee that every subsequent mutation to the entity 
    /// is systematically accounted for within the domain layer.
    /// </remarks>
    public interface IModificationAuditableEntity : ICreationAuditableEntity
    {
        /// <summary>
        /// Gets the unique identifier of the user or security principal who performed the most recent state mutation on this entity.
        /// </summary>
        /// <value>
        /// A string representation of the modifier's identity, or <see langword="null"/> if the entity has not been altered since creation. 
        /// Restrained to 155 characters to uphold cross-platform datastore optimization.
        /// </value>
        [MaxLength(155)]
        string? ModifierId { get; }

        /// <summary>
        /// Gets the date and time when the entity's state was last changed.
        /// </summary>
        /// <value>
        /// A nullable <see cref="DateTimeOffset"/> denoting the exact, timezone-aware moment of the last save operation. 
        /// Returns <see langword="null"/> for freshly materialized entities that remain unchanged from their initial state.
        /// </value>
        DateTimeOffset? LastModificationTime { get; }

        /// <summary>
        /// Enforces the mutation audit state by updating the identity of the actor and logging the instant the modification occurred.
        /// </summary>
        /// <param name="modifierId">The unique identifier of the security principal orchestrating the current domain action.</param>
        /// <returns>The current <see cref="IModificationAuditableEntity"/> instance to maintain fluent configuration syntax.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="modifierId"/> is null or an empty string.</exception>
        IModificationAuditableEntity SetModification(string modifierId);
    }

    /// <summary>
    /// A base abstract implementation that adds modification auditing capabilities to a creation-auditable domain entity framework.
    /// </summary>
    /// <typeparam name="TKey">The primary key type designating the identity boundary of the entity.</typeparam>
    /// <remarks>
    /// This base class encapsulates modification metrics within the entity boundary, safely keeping the parameter 
    /// mutations private from external application manipulation except via the explicitly provided <see cref="SetModification"/> domain method.
    /// </remarks>
    public abstract class ModificationAuditableEntity<TKey> : CreationAuditableEntity<TKey>, IModificationAuditableEntity where TKey : IEquatable<TKey>
    {
        /// <inheritdoc />
        [MaxLength(155)]
        public string? ModifierId { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset? LastModificationTime { get; private set; }

        /// <summary>
        /// Updates the entity's inner auditing metrics to mark a distinct state mutation event.
        /// </summary>
        /// <param name="modifierId">The identity of the actor driving the state change.</param>
        /// <returns>The entity instance for operational chaining.</returns>
        public virtual IModificationAuditableEntity SetModification(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                throw new ArgumentException("Modifier identifier cannot be null or white space.", nameof(modifierId));
            }

            ModifierId = modifierId;
            LastModificationTime = DateTimeOffset.UtcNow; // Explicitly leverages standard timezone tracking
            return this;
        }
    }
}