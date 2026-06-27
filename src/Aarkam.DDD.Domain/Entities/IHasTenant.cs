namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines the architectural contract for domain components or entities that are constrained to a multi-tenant isolation boundary.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Multi-tenancy isolation is a critical cross-cutting concern in enterprise cloud applications. 
/// Implement this interface on Aggregate Roots or Entities that belong to a specific tenant space to guarantee logical data partitioning.
/// </para>
/// <para>
/// <b>INFRASTRUCTURE ORCHESTRATION:</b> Implementing this contract allows global data access query filters 
/// (e.g., in your EF Core <c>DbContext</c>) to automatically inject a tenant identifier constraint (such as 
/// <c>HasQueryFilter(e => e.TenantId == currentTenantId)</c>) on all outbound queries. This prevents catastrophic data leaks between clients.
/// </para>
/// </remarks>
public interface IHasTenant
{
    /// <summary>
    /// Gets the unique identifier of the tenant context that owns this instance.
    /// </summary>
    /// <value>
    /// A string token identifying the tenant partition boundary, or <see langword="null"/> if the entity belongs 
    /// to a host or global system configuration.
    /// </value>
    string? TenantId { get; }
}