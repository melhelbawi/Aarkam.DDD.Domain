namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the structural contract for an Aggregate Root snapshot record used to optimize Event Sourcing rehydration performance.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> In an Event-Sourced architecture, an aggregate root's current state is derived by replaying its 
/// entire append-only history of <see cref="IDomainEvent"/> records. While highly expressive, aggregates with a massive volume of events 
/// (e.g., a long-running account ledger or a high-frequency inventory card) will experience significant data access latency over time. 
/// A Snapshot captures an optimized state delta marker at a specific point in time to establish a baseline threshold.
/// </para>
/// <para>
/// <b>RECONSTITUTION PIPELINE LIFECYCLE:</b>
/// <list type="number">
///   <item><description>The repository issues a data fetch request targeting the highest available <see cref="Version"/> checkpoint within the snapshot log.</description></item>
///   <item><description>If a snapshot is located, the infrastructure deserializes the <see cref="State"/> payload and passes it to the aggregate root for instant materialization.</description></item>
///   <item><description>The repository then queries the event log to stream only subsequent events where <c>Event.Version > Snapshot.Version</c>.</description></item>
///   <item><description>The aggregate replays this small event delta stream to achieve final transactional state equilibrium, avoiding full-history traversals.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>SCHEMA EVOLUTION WARNING:</b> Because snapshot states are serialized as static string documents, they are highly vulnerable to 
/// structural breaking changes when C# class definitions evolve (e.g., renaming a property or removing an embedded value object). 
/// Your infrastructure layer must deploy forward-compatible serialization routines, ignore unmapped properties gracefully, or 
/// execute json transformation migrations when old snapshots are rehydrated.
/// </para>
/// </remarks>
public interface ISnapshot
{
    /// <summary>
    /// Gets the string representation of the target aggregate instance identity.
    /// </summary>
    /// <value>
    /// Maps to the string format of your aggregate identity key. Tracking this as a string ensures 
    /// seamless framework compatibility regardless of whether the system utilizes a Guid, an integer sequence, or a string key.
    /// </value>
    string AggregateId { get; }

    /// <summary>
    /// Gets the unique text classification name of the Aggregate Root type boundary this snapshot captures.
    /// </summary>
    /// <example>"Order", "InventoryItem"</example>
    string AggregateType { get; }

    /// <summary>
    /// Gets the sequence version index number achieved by the aggregate root at the precise moment this snapshot was recorded.
    /// </summary>
    /// <value>The sequence high-water mark. Serves as the query offset filter when streaming the outstanding event delta during rehydration loops.</value>
    long Version { get; }

    /// <summary>
    /// Gets the serialized state configuration data payload capturing the aggregate's in-memory properties.
    /// </summary>
    /// <value>A self-contained text string, typically optimized as a JSON or binary-to-text string structure, representing the flat state snapshot.</value>
    string State { get; }

    /// <summary>
    /// Gets the exact timezone-aware point in time when this state snapshot envelope was committed to the datastore.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the multi-tenant logical partitioning identifier isolating this snapshot data boundary.
    /// </summary>
    /// <value>
    /// A string representation mapping to your multi-tenant tenant context, or <see langword="null"/> if processed 
    /// under global host infrastructure pipelines.
    /// </value>
    string? TenantId { get; }
}