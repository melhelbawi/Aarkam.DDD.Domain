using System.Diagnostics.CodeAnalysis;

namespace Aarkam.DDD.Domain;

/// <summary>
/// Provides a centralized static engine containing highly optimized Guard Clauses to enforce primitive domain invariants and structural parameters.
/// </summary>
/// <remarks>
/// <b>DESIGN PHILOSOPHY:</b> Guard clauses implement a <b>Fail-Fast</b> design strategy. They validate defensive boundary assertions 
/// at the absolute entry point of domain operations, shielding your internal Aggregate Root logic from executing with corrupt, unassigned, 
/// or structurally invalid parameter states.
/// </remarks>
public static class DomainGuard
{
    /// <summary>
    /// Evaluates a boolean assertion and throws a structurally matched exception if the evaluated check satisfies true.
    /// </summary>
    /// <typeparam name="TException">The target exception type to instantiate (must inherit from <see cref="Exception"/>).</typeparam>
    /// <param name="assertion">The conditional state expression check that, if <see langword="true"/>, trips the panic threshold.</param>
    /// <param name="message">The message injected into the exception instance explaining the failed domain invariant condition.</param>
    /// <remarks>
    /// <b>PERFORMANCE NOTE:</b> To prevent heavy garbage collection (GC) and reflection latency under heavy load loops, this method 
    /// bypasses legacy <see cref="Activator.CreateInstance(Type)"/> execution paths, utilizing a compiled expression factory wrapper instead.
    /// </remarks>
    public static void Against<TException>(bool assertion, string message)
        where TException : Exception
    {
        if (assertion)
        {
            throw ExceptionFactory<TException>.Create(message);
        }
    }

    /// <summary>
    /// Asserts that a targeted object reference state is not null.
    /// </summary>
    /// <param name="value">The variable or object reference to verify.</param>
    /// <param name="parameterName">The explicit code parameter identifier text mapped to the checked variable.</param>
    /// <exception cref="ArgumentNullException">Thrown instantly if the parameter reference evaluates to <see langword="null"/>.</exception>
    public static void AgainstNull([NotNull] object? value, string parameterName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName, $"The required parameter '{parameterName}' cannot evaluate to null.");
        }
    }

    /// <summary>
    /// Asserts that a targeted text parameter contains valid structural character strings.
    /// </summary>
    /// <param name="value">The target string reference to verify.</param>
    /// <param name="parameterName">The explicit code parameter identifier text mapped to the checked string.</param>
    /// <exception cref="ArgumentException">Thrown if the target text contains null reference nodes, empty strings, or spaces.</exception>
    public static void AgainstNullOrEmpty([NotNull] string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The required string parameter value cannot be null, empty, or consist entirely of whitespace.", parameterName);
        }
    }

    /// <summary>
    /// Asserts that a numerical integer value does not cross into a negative mathematical balance boundary.
    /// </summary>
    /// <param name="value">The long integer primitive value to analyze.</param>
    /// <param name="parameterName">The explicit code parameter identifier text mapped to the checked variable.</param>
    /// <exception cref="ArgumentException">Thrown if the value evaluates lower than 0.</exception>
    public static void AgainstNegative(long value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"The numerical parameter '{parameterName}' cannot be assigned a negative value.", parameterName);
        }
    }

    /// <summary>
    /// Asserts that a numerical integer value resides strictly above a zero mathematical index limit.
    /// </summary>
    /// <param name="value">The long integer primitive value to analyze.</param>
    /// <param name="parameterName">The explicit code parameter identifier text mapped to the checked variable.</param>
    /// <exception cref="ArgumentException">Thrown if the value evaluates to 0 or lower.</exception>
    public static void AgainstNegativeOrZero(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"The numerical parameter '{parameterName}' must represent a strict mathematical positive value greater than zero.", parameterName);
        }
    }

    /// <summary>
    /// Asserts that an integer numerical property resides safely inside inclusive mathematical boundary constraints.
    /// </summary>
    /// <param name="value">The input integer primitive value to test.</param>
    /// <param name="min">The lowest mathematically acceptable inclusive numeric value boundary floor.</param>
    /// <param name="max">The highest mathematically acceptable inclusive numeric value boundary ceiling.</param>
    /// <param name="parameterName">The explicit code parameter identifier text mapped to the checked variable.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the target input lands outside the configured parameters range window.</exception>
    public static void AgainstOutOfRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"The numerical value parameter must fit inside the inclusive range boundary window: [{min} to {max}].");
        }
    }

    /// <summary>
    /// Centralized assertion interceptor verifying complete structural safety compliance across explicit business specifications.
    /// </summary>
    /// <param name="rule">The complex encapsulated business invariant policy rule wrapper to analyze.</param>
    /// <exception cref="BusinessRuleValidationException">Thrown if the evaluate logic loop of <see cref="IBusinessRule.IsBroken"/> satisfies true.</exception>
    public static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }

    /// <summary>
    /// Internal, high-performance optimization utility caching dynamic lambda compilation delegates for exception generation.
    /// </summary>
    private static class ExceptionFactory<TException> where TException : Exception
    {
        private static readonly Func<string, TException> _factory = CreateFactory();

        public static TException Create(string message) => _factory(message);

        private static Func<string, TException> CreateFactory()
        {
            var type = typeof(TException);
            var constructor = type.GetConstructor([typeof(string)]);

            if (constructor is null)
            {
                return (msg) => (TException)Activator.CreateInstance(type, msg)!;
            }

            var param = System.Linq.Expressions.Expression.Parameter(typeof(string), "message");
            var body = System.Linq.Expressions.Expression.New(constructor, param);
            return System.Linq.Expressions.Expression.Lambda<Func<string, TException>>(body, param).Compile();
        }
    }
}