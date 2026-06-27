namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Defines a contract for domain components or entities that support dynamic, runtime-extensible properties.
/// This fulfills the Extensible Entity pattern commonly utilized in modular DDD architectures.
/// In Domain-Driven Design (DDD) frameworks, the IHasExtraProperties interface plays a critical role.
/// It enables a mechanism called Dynamic Properties or Extensible Entities, which allows application modules or 
/// developers to attach custom metadata to an existing core entity without modifying 
/// its database schema explicitly (usually serialized as a JSON string column like ExtraProperties in the database).
/// </summary>
/// <remarks>
/// Implement this interface on Aggregate Roots or core Entities to allow business applications or external modules 
/// to append arbitrary custom metadata to the object. This avoids invasive database schema structural updates 
/// for secondary or module-specific requirements, typically leveraging JSON serialization at the persistence layer.
/// </remarks>
public interface IHasExtraProperties
{
    /// <summary>
    /// Gets a dictionary container maintaining the dynamic metadata properties attached to this instance.
    /// </summary>
    /// <value>
    /// A collection of key-value pairs representing unstructured domain extensions.
    /// </value>
    Dictionary<string, object?> ExtraProperties { get; }
}

/// <summary>
/// Contains extension methods for the <see cref="IHasExtraProperties"/> interface to ensure standard, 
/// fluent domain manipulation of dynamic metadata properties.
/// </summary>
public static class HasExtraPropertiesExtensions
{
    /// <summary>
    /// Safely extracts a typed value from the entity's dynamic extra properties.
    /// </summary>
    /// <typeparam name="TProperty">The expected data type of the requested property value.</typeparam>
    /// <param name="source">The entity instance carrying the extensible properties boundary.</param>
    /// <param name="name">The unique lookup key for the target dynamic property.</param>
    /// <returns>The typed value if the property exists and matches; otherwise, its default value.</returns>
    public static TProperty? GetProperty<TProperty>(this IHasExtraProperties source, string name)
    {
        if (source.ExtraProperties == null || !source.ExtraProperties.TryGetValue(name, out var value))
        {
            return default;
        }

        if (value is TProperty typedValue)
        {
            return typedValue;
        }

        try
        {
            // Fallback for primitive casting or unboxing conversions
            return (TProperty)Convert.ChangeType(value, typeof(TProperty))!;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Fluently attaches or replaces a dynamic property within the entity boundary.
    /// </summary>
    /// <param name="source">The entity instance carrying the extensible properties boundary.</param>
    /// <param name="name">The unique identifier key for the property.</param>
    /// <param name="value">The dynamic metadata payload to map to the key.</param>
    /// <returns>The source entity for chainable domain operations.</returns>
    /// <exception cref="ArgumentException">Thrown when the property <paramref name="name"/> is null or empty.</exception>
    public static IHasExtraProperties SetProperty(this IHasExtraProperties source, string name, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Dynamic property key name cannot be null or whitespace.", nameof(name));
        }

        source.ExtraProperties[name] = value;
        return source;
    }

    /// <summary>
    /// Removes a dynamic property key entirely from the entity boundary if it is present.
    /// </summary>
    /// <param name="source">The entity instance carrying the extensible properties boundary.</param>
    /// <param name="name">The unique identifier key to delete.</param>
    /// <returns><c>true</c> if the key was found and removed successfully; otherwise, <c>false</c>.</returns>
    public static bool RemoveProperty(this IHasExtraProperties source, string name)
    {
        return source.ExtraProperties?.Remove(name) ?? false;
    }
}