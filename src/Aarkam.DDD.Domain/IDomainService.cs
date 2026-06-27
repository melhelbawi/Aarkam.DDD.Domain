namespace Aarkam.DDD.Domain;

/// <summary>
/// Defines a marker contract for Domain Services within the Domain Layer.
/// </summary>
/// <remarks>
/// <para>
/// <b>CORE DOMAIN ROLE:</b> 
/// In Domain-Driven Design (DDD), a Domain Service encapsulates business logic, computations, or orchestrations 
/// that do not naturally belong inside a single Entity or Value Object. Domain Services are entirely stateless; 
/// they focus purely on business behavior, applying domain invariants across multiple aggregate streams or executing 
/// domain-specific calculations.
/// </para>
/// <para>
/// <b>WHEN TO ENCAPSULATE LOGIC HERE:</b>
/// <list type="bullet">
///   <item><description>The process intrinsically involves multiple independent entities or distinct aggregate root boundaries.</description></item>
///   <item><description>The operation represents a significant, highly strategic piece of the Ubiquitous Language that isn't cleanly tied to an identity lifecycle.</description></item>
///   <item><description>The logic requires specific domain-driven read access to external lookup dependencies or calculations before mutating state.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>CRITICAL DDD BOUNDARY DISTINCTIONS:</b>
/// <list type="table">
///   <listheader>
///     <term>Concept</term>
///     <description>Architectural Boundary and Rule</description>
///   </listheader>
///   <item>
///     <term>Domain Service vs. Application Service</term>
///     <description>Domain Services contain core business calculations and guard domain invariants. Application Services do not contain business logic; they orchestrate the execution flow, handle data transfer objects (DTOs), open database transaction boundaries, and interact with infrastructure configurations.</description>
///   </item>
///   <item>
///     <term>Domain Service vs. Entity Method</term>
///     <description>Always favor putting state-mutation methods directly on the Entity if the operation applies strictly to its own properties. Avoid bleeding entity methods into an anemic domain service pattern.</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>UBIQUITOUS LANGUAGE EXAMPLES:</b>
/// <list type="bullet">
///   <item><description><c>ITransferService</c> - Orchestrates cryptographic balances or money transfers across two distinct accounts.</description></item>
///   <item><description><c>IPricingEngine</c> - Computes dynamic order discount rules combining active structural promotions, client levels, and product tags.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>STANDARD FRAMEWORK USAGE:</b>
/// <code>
/// public interface IFundsTransferService : IDomainService
/// {
///     Result Transfer(Account source, Account destination, Money volume);
/// }
/// 
/// public class FundsTransferService : IFundsTransferService
/// {
///     public Result Transfer(Account source, Account destination, Money volume)
///     {
///         if (!source.CanWithdraw(volume))
///             return Result.Failure("Insufficient account liquidity.");
///             
///         source.Withdraw(volume);
///         destination.Deposit(volume);
///         
///         return Result.Success();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IDomainService
{
}