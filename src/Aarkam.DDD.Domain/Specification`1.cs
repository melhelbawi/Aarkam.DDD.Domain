#nullable enable
using System.Linq.Expressions;

namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines a contract for the Specification Pattern, encapsulating strongly-typed domain query constraints, sorting policies, and pagination parameters.
/// </summary>
/// <typeparam name="T">The type of the domain entity or aggregate root evaluated against this criteria framework.</typeparam>
/// <remarks>
/// <b>DESIGN PHILOSOPHY:</b> The Specification pattern extracts messy, fragmented query filters out of application use cases 
/// and encapsulates them into reusable, highly testable, and business-named domain components. This preserves the 
/// <b>Single Responsibility Principle (SRP)</b> and satisfies the **Ubiquitous Language** by giving names to complex data constraints 
/// (e.g., <c>new PremiumCustomersWithOverdueInvoicesSpecification()</c>).
/// </remarks>
public interface ISpecification<T>
{
    /// <summary>
    /// Evaluates whether a specific in-memory domain entity instance satisfies the internal business criteria criteria.
    /// </summary>
    /// <param name="entity">The target domain instance to evaluate.</param>
    /// <returns><see langword="true"/> if the entity configuration satisfies the criteria; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <b>PERFORMANCE WARNING:</b> Calling this compiles the underlying expression tree at runtime. While perfectly safe and 
    /// recommended for TDD/Unit Testing assertions, invoking this in production loops can degrade performance. 
    /// For datastore queries, always supply <see cref="ToExpression"/> directly to your LINQ engine.
    /// </remarks>
    bool IsSatisfiedBy(T entity);

    /// <summary>
    /// Transforms the specification criteria into a translatable lambda expression tree suitable for ORMs like EF Core.
    /// </summary>
    /// <returns>A translatable expression mapping condition trees.</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Gets the list of navigational properties or related entities to eagerly include during materialization.
    /// </summary>
    /// <value>A collection of property paths mapping relationships inside the aggregate graph.</value>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the criteria expression designating an ascending sort order sequence.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the criteria expression designating a descending sort order sequence.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the row ceiling allocation limit utilized during pagination.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the collection item row offset utilized during pagination.
    /// </summary>
    int? Skip { get; }
}

/// <summary>
/// Provides a base class implementation for specifications supporting fluent combination chains, relational data inclusion, and pagination controls.
/// </summary>
/// <typeparam name="T">The type of the target domain object evaluated by the specification rules.</typeparam>
public abstract class SpecificationBase<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    /// <inheritdoc />
    public int? Take { get; protected set; }

    /// <inheritdoc />
    public int? Skip { get; protected set; }

    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <inheritdoc />
    public virtual bool IsSatisfiedBy(T entity)
    {
        Func<T, bool> predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Appends a relational entity tracking path to be eagerly fetched from storage during query execution.
    /// </summary>
    /// <param name="includeExpression">A lambda expression mapping to a navigation property path.</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Fluently combines this specification criteria with another using a conditional logical AND condition block.
    /// </summary>
    public ISpecification<T> And(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Fluently combines this specification criteria with another using a conditional logical OR condition block.
    /// </summary>
    public ISpecification<T> Or(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Negates this specification's internal filter parameters using a conditional logical NOT expression.
    /// </summary>
    public ISpecification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

/// <summary>
/// Combines two specifications using a logical conditional AND block, utilizing parameter replacement mapping to guarantee EF Core translation compliance.
/// </summary>
internal sealed class AndSpecification<T> : SpecificationBase<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ParameterReplacer(leftExpr.Parameters[0], parameter);
        var left = leftVisitor.Visit(leftExpr.Body);

        var rightVisitor = new ParameterReplacer(rightExpr.Parameters[0], parameter);
        var right = rightVisitor.Visit(rightExpr.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }
}

/// <summary>
/// Combines two specifications using a logical conditional OR block, utilizing parameter replacement mapping to guarantee EF Core translation compliance.
/// </summary>
internal sealed class OrSpecification<T> : SpecificationBase<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ParameterReplacer(leftExpr.Parameters[0], parameter);
        var left = leftVisitor.Visit(leftExpr.Body);

        var rightVisitor = new ParameterReplacer(rightExpr.Parameters[0], parameter);
        var right = rightVisitor.Visit(rightExpr.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left!, right!), parameter);
    }
}

/// <summary>
/// Negates a core specification using a logical conditional NOT block, fully translatable by EF Core.
/// </summary>
internal sealed class NotSpecification<T> : SpecificationBase<T>
{
    private readonly ISpecification<T> _specification;

    public NotSpecification(ISpecification<T> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));

        var visitor = new ParameterReplacer(expr.Parameters[0], parameter);
        var body = visitor.Visit(expr.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.Not(body!), parameter);
    }
}

/// <summary>
/// Internal expression visitor utility class that harmonizes parameter reference trees across multiple merged specifications.
/// </summary>
/// <remarks>
/// <b>EF CORE TRANSLATION TRANSLATION ROLE:</b> Merging separate expressions creates fragmented variable identifiers. 
/// Using <see cref="Expression.Invoke"/> creates dynamic unresolvable nodes for SQL map engines. 
/// This class merges disjoint parameters into a clean, unified tree structure that EF Core can parse without throwing errors.
/// </remarks>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return ReferenceEquals(node, _oldParameter) ? _newParameter : base.VisitParameter(node);
    }
}