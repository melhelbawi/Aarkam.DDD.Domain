namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines the abstraction for accessing the ambient multi-tenant execution context.
/// </summary>
/// <remarks>
/// This interface isolates the Domain and Application layers from direct dependencies on infrastructure components 
/// (such as HTTP Contexts, gRPC metadata, or security claims arrays), fulfilling the Dependency Inversion Principle.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Gets the unique logical identifier of the currently resolved tenant.
    /// </summary>
    /// <value>
    /// A string token identifying the active tenant partition, or <see langword="null"/> if the execution 
    /// is running under the global **Host/System Administrator** context.
    /// </value>
    string? TenantId { get; }
}

/// <summary>
/// Defines the primary application contract for reading and transiently modifying the active tenant context.
/// </summary>
/// <remarks>
/// This is the primary interface recommended for injection into Domain Services, Application Services, 
/// and EF Core interceptors to evaluate multi-tenant data filters.
/// </remarks>
public interface ICurrentTenant : ITenantContext
{
    /// <summary>
    /// Gets a value indicating whether the current execution execution loop is running under a valid tenant boundary.
    /// </summary>
    bool IsAvailable => !string.IsNullOrWhiteSpace(TenantId);

    /// <summary>
    /// Temporarily shifts the ambient execution flow into a specified tenant context boundary.
    /// </summary>
    /// <param name="tenantId">The target tenant identifier token, or <see langword="null"/> to switch to the Host context.</param>
    /// <returns>An <see cref="IDisposable"/> token that automatically reverts back to the previous tenant context upon disposal.</returns>
    /// <example>
    /// <code>
    /// using (_currentTenant.Change(targetTenantId))
    /// {
    ///     // Execution here is isolated safely inside the target tenant partition
    ///     await _repository.UpdateAsync(entity);
    /// }
    /// </code>
    /// </example>
    IDisposable Change(string? tenantId);
}

/// <summary>
/// Provides ambient, execution-context-aware tracking to manage the active tenant state.
/// </summary>
/// <remarks>
/// This infrastructure component leverages an internal <see cref="AsyncLocal{T}"/> stream to safely flow the tenant identity 
/// across asynchronous await boundaries, blocking cross-thread tenant data corruption inside high-throughput engines.
/// </remarks>
public class CurrentTenant : ICurrentTenant
{
    private readonly AsyncLocal<string?> _currentTenantId = new();

    /// <inheritdoc />
    public string? TenantId => _currentTenantId.Value;

    /// <inheritdoc />
    public IDisposable Change(string? tenantId)
    {
        return new DisposeAction(() => SetCurrent(tenantId));
    }

    /// <summary>
    /// Explicitly overwrites the active tracking token mapped to the ambient thread context.
    /// </summary>
    /// <param name="tenantId">The target tenant identity token to assign.</param>
    protected virtual void SetCurrent(string? tenantId)
    {
        _currentTenantId.Value = tenantId;
    }

    /// <summary>
    /// Private utility class implementing the structural scope restoration wrapper pattern.
    /// </summary>
    private sealed class DisposeAction : IDisposable
    {
        private readonly Action _action;

        public DisposeAction(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Dispose() => _action();
    }
}

/// <summary>
/// Provides ambient static access to the active multi-tenant context for situations where constructor 
/// dependency injection is unavailable or structurally blocked by legacy patterns.
/// </summary>
public static class AmbientTenantContext
{
    private static readonly AsyncLocal<string?> _ambientTenantId = new();

    /// <summary>
    /// Gets the globally tracked tenant token bound to the active asynchronous thread execution tree.
    /// </summary>
    public static string? CurrentTenantId => _ambientTenantId.Value;

    /// <summary>
    /// Updates the static thread identity tracking token.
    /// </summary>
    /// <param name="tenantId">The target tenant value to record.</param>
    public static void SetCurrent(string? tenantId)
    {
        _ambientTenantId.Value = tenantId;
    }

    /// <summary>
    /// Resets the thread context completely, reverting the tracking parameter back to a Host/Global state.
    /// </summary>
    public static void Clear()
    {
        _ambientTenantId.Value = null;
    }
}