using Aarkam.DDD.Domain.Entities;

namespace Aarkam.DDD.Domain;

/// <summary>
/// Provides a foundational base class for an Event-Sourced, audited, multi-tenant Aggregate Root.
/// </summary>
/// <typeparam name="TKey">The structural type used for the aggregate root's identity boundary.</typeparam>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> An Aggregate Root encapsulates a graph of closely related domain objects
/// and maintains strict consistency invariants. External systems can only reference, query, or load 
/// the aggregate via this root object using its unique identity (<see cref="Entity{TKey}.Id"/>).
/// </para>
/// <para>
/// <b>EVENT SOURCING HYBRID PATTERN:</b> This implementation handles state transformations via an 
/// ordered stream of immutable <see cref="IDomainEvent"/> instances. To seamlessly blend Event Sourcing 
/// with relational ORMs like EF Core, the model retains concrete tracking state fields. 
/// However, state modifications should <i>only</i> occur inside the protected <see cref="ApplyEvent(IDomainEvent)"/> 
/// method during event execution or historical playback.
/// </para>
/// </remarks>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Holds the internal collection of uncommitted domain events raised during the active execution context.
    /// </summary>
    /// <remarks>
    /// <b>WHY A PRIVATE LIST?</b> Encapsulation ensures that external layers (like Application Services) 
    /// cannot inject, modify, or corrupt the aggregate's change log directly. It is read via <see cref="GetChanges"/> 
    /// and cleared via <see cref="MarkEventsAsCommitted"/> by infrastructure code.
    /// </remarks>
    private readonly List<IDomainEvent> _changes = [];

    /// <summary>
    /// Gets the current structural version sequence of the aggregate instance.
    /// </summary>
    /// <value>The current sequence index. Starts at -1 for completely new, unpersisted aggregate instances.</value>
    /// <remarks>
    /// <b>WHY A PROTECTED SETTER?</b> This property is a core mechanism for **Optimistic Concurrency Control**. 
    /// When storing events or state snapshots in relational databases via EF Core or specialized event stores, 
    /// this version number is checked against the database record to prevent overlapping write operations 
    /// (Race Conditions). The framework handles incrementing this value automatically during <see cref="Apply"/>.
    /// </remarks>
    public virtual int Version { get; protected set; } = -1;

    /// <summary>
    /// Gets a dictionary container maintaining unstructured, dynamic runtime metadata extension properties.
    /// </summary>
    /// <remarks>
    /// <b>WHY AN EXTENSIBLE DICTIONARY?</b> This satisfies the **Extensible Entity Pattern**. It allows broad, 
    /// application-specific, or multi-tenant custom fields to be attached to an entity without altering 
    /// the physical, compiled C# class structure. For EF Core, this dictionary is typically mapped to a 
    /// single, backing database column containing a raw JSON text payload via value converters.
    /// </remarks>
    public virtual Dictionary<string, object?> ExtraProperties { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this aggregate has been marked as logically deleted.
    /// </summary>
    /// <remarks>
    /// <b>WHY A PROTECTED SETTER?</b> Encapsulation requires that an entity cannot be arbitrarily killed 
    /// from outside the aggregate boundary. Deletion must happen strictly via domain actions or managed 
    /// data interceptors using <see cref="SetSoftDeletion"/>. The protected access also grants EF Core 
    /// reflection access to map Global Query Filters (e.g., <c>HasQueryFilter(e => !e.IsDeleted)</c>).
    /// </remarks>
    public virtual bool IsDeleted { get; protected set; }

    /// <summary>
    /// Gets the unique logical tenant identifier partitioning multi-tenant data structures.
    /// </summary>
    /// <remarks>
    /// <b>WHY A PROTECTED SETTER?</b> Restricting setter exposure ensures isolation integrity. 
    /// Tenancy changes are a rare, high-security operational context that should never happen implicitly 
    /// during standard business processing.
    /// </remarks>
    public virtual string? TenantId { get; protected set; }

    public virtual string? CreatorId { get; protected set; }
    public virtual DateTimeOffset CreationTime { get; protected set; }

    public virtual string? ModifierId { get; protected set; }
    public virtual DateTimeOffset? LastModificationTime { get; protected set; }

    public virtual string? DeleterId { get; protected set; }
    public virtual DateTimeOffset? DeletionTime { get; protected set; }

    /// <summary>
    /// Parameterless constructor initializing transient aggregate root profiles.
    /// </summary>
    /// <remarks>
    /// <b>WHY IS THIS REQUIRED?</b> Object-Relational Mappers like **EF Core require a parameterless constructor** 
    /// to materialize database records back into objects via reflection. It is marked <c>protected</c> to prevent 
    /// developers from instantiating invalid empty aggregates in their application layers.
    /// <b>TDD NOTE:</b> Eager instantiation of collections here prevents ugly <see cref="NullReferenceException"/> 
    /// loops in unit testing setups.
    /// </remarks>
    protected AggregateRoot()
    {
        ExtraProperties = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Explicit constructor initializing an aggregate instance with a client-assigned or predefined identification key.
    /// </summary>
    /// <param name="id">The unique identifier to bind permanently to this aggregate root context.</param>
    protected AggregateRoot(TKey id) : base(id)
    {
        ExtraProperties = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Exposes a secure, read-only collection containing all staging uncommitted domain events.
    /// </summary>
    /// <returns>A read-only view of the internal domain change tracking collection.</returns>
    /// <remarks>
    /// <para>
    /// <b>WHY A READ-ONLY VIEW?</b> Returning <see cref="IReadOnlyCollection{IDomainEvent}"/> honors the <b>Principle of Least Privilege</b>. 
    /// External infrastructure components can iterate through the payload to publish or persist it without gaining the 
    /// structural compiler capability to add or alter elements.
    /// </para>
    /// <para>
    /// <b>PERFORMANCE NOTE:</b> Instead of wrapping the collection via <c>.AsReadOnly()</c> (which allocates a new wrapper object 
    /// on the heap every time it is called), this method leverages C#'s native covariance by implicitly casting the private 
    /// <see cref="List{IDomainEvent}"/> to <see cref="IReadOnlyCollection{IDomainEvent}"/>. This achieves zero allocation overhead.
    /// </para>
    /// </remarks>
    public virtual IReadOnlyCollection<IDomainEvent> GetChanges() => _changes;

    /// <inheritdoc />
    public virtual string GetAggregateType() => GetType().Name;

    /// <summary>
    /// Reconstitutes the internal state parameters of the aggregate root by replaying a historical stream of domain events.
    /// </summary>
    /// <param name="history">An ordered sequence of chronological domain events belonging to this aggregate stream.</param>
    /// <remarks>
    /// <b>WHY IS THIS SEPARATE?</b> Replaying historical events updates internal states without re-generating 
    /// brand-new uncommitted events into the <see cref="_changes"/> tracking list. It ensures the playback loop 
    /// remains free of unintended side effects.
    /// </remarks>
    public virtual void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (history == null) throw new ArgumentNullException(nameof(history));

        foreach (var domainEvent in history)
        {
            Apply(domainEvent);
        }
    }

    /// <summary>
    /// Coordinates internal state updates and advances the tracking version index.
    /// </summary>
    /// <param name="domainEvent">The domain transaction event to parse.</param>
    public virtual void Apply(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        ApplyEvent(domainEvent);
        Version++; // Synchronizes version progression during both standard operations and history rehydration
    }

    /// <summary>
    /// Clears the pending mutations array following successful disk/database persistent transactions.
    /// </summary>
    /// <remarks>
    /// <b>WHY IS THIS NECESSARY?</b> This method prevents memory leaks and ensures that the same event 
    /// payload isn't committed multiple times by the Unit of Work or Outbox dispatch systems during subsequent updates.
    /// </remarks>
    public virtual void MarkEventsAsCommitted()
    {
        _changes.Clear();
    }

    /// <summary>
    /// Registers a newly occurring domain event, applying it instantly before appending it to the uncommitted tracking log.
    /// </summary>
    /// <param name="domainEvent">The brand new state change event generated by a domain business action.</param>
    /// <remarks>
    /// <b>WHY DO METADATA VALIDATIONS HERE?</b> This method enforces **Fail-Fast structural invariant integrity**. 
    /// If an aggregate generates an event targeting a different ID, type, or an out-of-sync version sequence, 
    /// it signals severe logic drift or memory corruption. Catching this inside the aggregate boundaries prevents invalid 
    /// or corrupt data streams from writing out to your event logging storage.
    /// </remarks>
    protected virtual void RaiseEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        if (domainEvent.AggregateId != Id?.ToString() ||
            domainEvent.AggregateType != GetAggregateType() ||
            domainEvent.Version != Version + 1)
        {
            throw new InvalidOperationException(
                $"Domain event structural mismatch found in tracking chain '{domainEvent.GetType().Name}'. " +
                $"Expected Target Stream [Id: {Id}, Type: {GetAggregateType()}, TargetVersion: {Version + 1}]. " +
                $"Encountered Context Metadata [Id: {domainEvent.AggregateId}, Type: {domainEvent.AggregateType}, EventSequence: {domainEvent.Version}].");
        }

        Apply(domainEvent);
        _changes.Add(domainEvent);
    }

    /// <summary>
    /// Abstract hook overridden within specialized domain models to process type-specific mutations safely.
    /// </summary>
    /// <param name="domainEvent">The unboxed domain event to switch pattern match against.</param>
    /// <remarks>
    /// <b>WHY IS THIS A NO-OP HOOK?</b> This isolates the **State Mutation** framework from infrastructure plumbing. 
    /// Derived concrete classes override this single method, usually containing a switch block or pattern match, to map properties 
    /// safely (e.g., <c>this.Status = orderCreated.Status</c>). It ensures that all state mutations occur through an identical code execution flow.
    /// </remarks>
    protected virtual void ApplyEvent(IDomainEvent domainEvent)
    {
    }

    /// <summary>
    /// Validates core operational domain specifications before processing state transitions.
    /// </summary>
    /// <param name="rule">The target business policy or constraint specification to evaluate.</param>
    protected void CheckRule(IBusinessRule rule)
    {
        BusinessRuleValidationException.Check(rule);
    }

    public virtual ICreationAuditableEntity SetCreation(string creatorId)
    {
        CreatorId = string.IsNullOrWhiteSpace(creatorId) ? throw new ArgumentException("Value cannot be null.", nameof(creatorId)) : creatorId;
        CreationTime = DateTimeOffset.UtcNow;
        return this;
    }

    public virtual IModificationAuditableEntity SetModification(string modifierId)
    {
        ModifierId = string.IsNullOrWhiteSpace(modifierId) ? throw new ArgumentException("Value cannot be null.", nameof(modifierId)) : modifierId;
        LastModificationTime = DateTimeOffset.UtcNow;
        return this;
    }

    public virtual IDeletionAuditableEntity SetSoftDeletion(string deleterId)
    {
        DeleterId = string.IsNullOrWhiteSpace(deleterId) ? throw new ArgumentException("Value cannot be null.", nameof(deleterId)) : deleterId;
        DeletionTime = DateTimeOffset.UtcNow;
        IsDeleted = true;
        return this;
    }

    /// <summary>
    /// Restores a soft-deleted aggregate instance, resetting metadata records to their active state parameters.
    /// </summary>
    /// <returns>The active aggregate root instance configuration.</returns>
    /// <remarks>
    /// <b>WHY RESET DELETER TO NULL?</b> If an entity is restored, it is no longer deleted. Keeping values inside 
    /// <see cref="DeleterId"/> or <see cref="DeletionTime"/> breaks the **Ubiquitous Language** of the model. 
    /// Recording *who* triggered the restoration is handled by passing that tracking responsibility to 
    /// <see cref="SetModification"/> automatically.
    /// </remarks>
    public virtual IDeletionAuditableEntity Restore()
    {
        if (!IsDeleted) return this;

        IsDeleted = false;
        DeleterId = null;
        DeletionTime = null;
        return this;
    }
}

/// <summary>
/// A convenient default abstract wrapper optimizing standard globally unique identifier system configurations (<see cref="Guid"/>).
/// </summary>
/// <remarks>
/// <b>WHY OFFER THIS SHORTCUT?</b> While a generic model accommodates custom tracking tokens or strings, 
/// over 90% of microservice enterprise infrastructures default entirely to <see cref="Guid"/> mappings. Exposing this 
/// non-generic variant cuts down boilerplate redundancy significantly inside everyday application modules.
/// </remarks>
public abstract class AggregateRoot : AggregateRoot<Guid>, IAggregateRoot
{
    protected AggregateRoot() : base() { }
    protected AggregateRoot(Guid id) : base(id) { }
}