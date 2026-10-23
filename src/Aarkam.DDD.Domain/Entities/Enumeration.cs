using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Provides a base class for the Smart Enum / Domain Enumeration pattern in Domain-Driven Design.
/// Encapsulates discrete domain values with behavior, display formatting, and rich metadata.
/// Native AOT and IL Trimming compatible.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Primitive C# <c>enum</c> types are anemic: they cannot encapsulate business behavior, 
/// cannot be localized, cannot implement interfaces, and cannot carry rich metadata (like display names, descriptions, or calculation formulas). 
/// The <see cref="Enumeration"/> class solves this by treating discrete choices as strongly-typed, immutable value-like objects 
/// while preserving the simplicity of constant enumeration definitions.
/// </para>
/// <para>
/// <b>USAGE PATTERN:</b>
/// <code>
/// public sealed class OrderStatus : Enumeration
/// {
///     public static readonly OrderStatus Submitted = new(1, nameof(Submitted), "Order Submitted");
///     public static readonly OrderStatus AwaitingValidation = new(2, nameof(AwaitingValidation), "Awaiting Validation");
///     public static readonly OrderStatus Shipped = new(3, nameof(Shipped), "Shipped to Customer");
///     public static readonly OrderStatus Cancelled = new(4, nameof(Cancelled), "Cancelled by User");
/// 
///     private OrderStatus(int id, string name, string displayName) : base(id, name, displayName) { }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class Enumeration : IComparable, IEquatable<Enumeration>
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<object>> _cache = new();

    /// <summary>
    /// Gets the integer identifier corresponding to the enumeration option.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the programmatic name of the enumeration option (typically matching the identifier name).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets an optional human-readable display title for localization and UI presentation.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Enumeration"/> class.
    /// </summary>
    /// <param name="id">The persistent unique numeric value for this enumeration option.</param>
    /// <param name="name">The programmatic identity name.</param>
    /// <param name="displayName">An optional user-facing display label.</param>
    protected Enumeration(int id, string name, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Enumeration name cannot be null or whitespace.", nameof(name));
        }

        Id = id;
        Name = name;
        DisplayName = displayName ?? name;
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName ?? Name;

    /// <summary>
    /// Retrieves all declared enumeration options of the specified type using cached reflection.
    /// Native AOT compatible via member preservation annotations.
    /// </summary>
    /// <typeparam name="T">The concrete <see cref="Enumeration"/> subclass type.</typeparam>
    /// <returns>A read-only collection of all static instances declared on the type.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "T is annotated with DynamicallyAccessedMembers, preserving all public fields and properties.")]
    public static IEnumerable<T> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : Enumeration
    {
        var type = typeof(T);
        var items = _cache.GetOrAdd(type, static t =>
            t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => t.IsAssignableFrom(f.FieldType))
                .Select(f => f.GetValue(null)!)
                .Concat(
                    t.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(p => t.IsAssignableFrom(p.PropertyType))
                        .Select(p => p.GetValue(null)!))
                .ToList());

        return items.Cast<T>();
    }

    /// <summary>
    /// Parses an integer value into its corresponding concrete <see cref="Enumeration"/> instance.
    /// </summary>
    /// <typeparam name="T">The target enumeration type.</typeparam>
    /// <param name="value">The integer identity to look up.</param>
    /// <returns>The matching enumeration option instance.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if no matching enumeration option is defined for <paramref name="value"/>.</exception>
    public static T FromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int value) where T : Enumeration
    {
        if (TryFromValue<T>(value, out var result) && result is not null)
        {
            return result;
        }

        throw new EntityNotFoundException(
            $"'{value}' is not a valid numeric identifier for enumeration type '{typeof(T).Name}'.",
            "ERR_ENUMERATION_NOT_FOUND",
            ExceptionSeverity.Warning);
    }

    /// <summary>
    /// Parses a string representation into its corresponding concrete <see cref="Enumeration"/> instance.
    /// </summary>
    /// <typeparam name="T">The target enumeration type.</typeparam>
    /// <param name="name">The name or display name to match.</param>
    /// <param name="ignoreCase">Whether comparison is case-insensitive (defaults to true).</param>
    /// <returns>The matching enumeration option instance.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if no matching enumeration option is defined for <paramref name="name"/>.</exception>
    public static T FromName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string name, bool ignoreCase = true) where T : Enumeration
    {
        if (TryFromName<T>(name, ignoreCase, out var result) && result is not null)
        {
            return result;
        }

        throw new EntityNotFoundException(
            $"'{name}' is not a valid name for enumeration type '{typeof(T).Name}'.",
            "ERR_ENUMERATION_NOT_FOUND",
            ExceptionSeverity.Warning);
    }

    /// <summary>
    /// Attempts to parse an integer value into its corresponding concrete <see cref="Enumeration"/> instance.
    /// </summary>
    public static bool TryFromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(int value, out T? result) where T : Enumeration
    {
        result = GetAll<T>().FirstOrDefault(item => item.Id == value);
        return result is not null;
    }

    /// <summary>
    /// Attempts to parse a string representation into its corresponding concrete <see cref="Enumeration"/> instance.
    /// </summary>
    public static bool TryFromName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(string name, bool ignoreCase, out T? result) where T : Enumeration
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        result = GetAll<T>().FirstOrDefault(item =>
            string.Equals(item.Name, name, comparison) ||
            string.Equals(item.DisplayName, name, comparison));
        return result is not null;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other)
        {
            return false;
        }

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public bool Equals(Enumeration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <inheritdoc />
    public virtual int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is not Enumeration other)
        {
            throw new ArgumentException($"Object must be of type {nameof(Enumeration)}.", nameof(obj));
        }

        return Id.CompareTo(other.Id);
    }

    public static bool operator ==(Enumeration? left, Enumeration? right) => Equals(left, right);
    public static bool operator !=(Enumeration? left, Enumeration? right) => !Equals(left, right);
    public static bool operator <(Enumeration? left, Enumeration? right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(Enumeration? left, Enumeration? right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(Enumeration? left, Enumeration? right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(Enumeration? left, Enumeration? right) => left is null ? right is null : left.CompareTo(right) >= 0;
}
