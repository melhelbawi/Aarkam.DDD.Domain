namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines a contract for domain entities or aggregate roots that generate and track domain events.
/// This enables infrastructure components (such as EF Core interceptors, Unit of Work, and Outbox dispatchers)
/// to extract and dispatch events uniformly without coupling to full event-sourced aggregate roots.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of pending, uncommitted domain events raised by this instance.
    /// </summary>
    /// <returns>A read-only view of the internal domain change tracking collection.</returns>
    IReadOnlyCollection<IDomainEvent> GetChanges();

    /// <summary>
    /// Clears the pending domain events after they have been successfully dispatched or persisted.
    /// </summary>
    void MarkEventsAsCommitted();
}
