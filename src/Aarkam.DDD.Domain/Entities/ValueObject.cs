namespace Aarkam.DDD.Domain.Entities;

/// <summary>
/// Provides a base class implementation for immutable Value Objects within a Domain-Driven Design (DDD) model.
/// </summary>
/// <remarks>
/// <para>
/// <b>DESIGN PHILOSOPHY:</b> Unlike an <see cref="Entity{TKey}"/>, a Value Object has no distinct 
/// thread of continuity or runtime identity. It is defined entirely by the structural configuration of its primitive 
/// attributes (e.g., an address, an exchange volume, or a color matrix). Two Value Objects are completely 
/// interchangeable if all of their internal value parameters match identically.
/// </para>
/// <para>
/// <b>IMMUTABILITY PRINCIPLE:</b> Value Objects must be structurally read-only from initialization until disposal. 
/// Modifying a value object state requires creating a completely new structural instance with the altered configurations, 
/// rather than mutating properties on the existing reference instance.
/// </para>
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Enumerates the discrete structural components that jointly determine this Value Object's definition.
    /// </summary>
    /// <returns>An ordered collection of internal properties, values, fields, or nested structural types.</returns>
    /// <remarks>
    /// <b>WHY EXTEND THIS?</b> Derived implementations must override this method to feed variables to structural matching engines. 
    /// Order is significant: attributes are sequentially checked using structural equality rules in the order they are returned.
    /// </remarks>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Evaluates whether the targeted object reference satisfies domain structural structural symmetry.
    /// </summary>
    /// <param name="obj">The baseline object reference to analyze.</param>
    /// <returns><c>true</c> if the types are perfectly symmetrical and internal variables align; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null || GetType() != obj.GetType())
        {
            return false;
        }

        return Equals((ValueObject)obj);
    }

    /// <summary>
    /// High-performance typed comparison checking item-by-item parameter alignment.
    /// </summary>
    /// <param name="other">The competing target Value Object instance.</param>
    /// <returns><c>true</c> if all nested components match sequentially; otherwise, <c>false</c>.</returns>
    public virtual bool Equals(ValueObject? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Computes a structural hash code distribution by blending individual property hashes using high-performance combining loops.
    /// </summary>
    /// <returns>A signed 32-bit integer distribution representing the collection variables structure.</returns>
    /// <remarks>
    /// <b>PERFORMANCE NOTE:</b> This implementation uses the modern <see cref="HashCode"/> type introduced in .NET Core. 
    /// This structure generates significantly fewer hash collisions compared to legacy prime-multiplication algorithms, 
    /// ensuring optimal retrieval performance when value objects serve as composite dictionary keys or hash set items.
    /// </remarks>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var component in GetEqualityComponents())
        {
            hashCode.Add(component);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Static internal handler evaluating equivalence logic across explicit parameters.
    /// </summary>
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Static internal handler evaluating non-equivalence constraints across parameters.
    /// </summary>
    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
    {
        return !EqualOperator(left, right);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => EqualOperator(left, right);
    public static bool operator !=(ValueObject? left, ValueObject? right) => NotEqualOperator(left, right);
}