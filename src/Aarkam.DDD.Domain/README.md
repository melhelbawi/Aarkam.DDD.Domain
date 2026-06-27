# Aarkam.DDD.Domain

[![Nuget](https://img.shields.io/nuget/v/Aarkam.DDD.Domain?style=flat-square)](https://www.nuget.org/packages/Aarkam.DDD.Domain)
[![GitHub License](https://img.shields.io/github/license/melhelbawi/Aarkam.DDD.Domain?style=flat-square)](https://github.com/melhelbawi/Aarkam.DDD.Domain/blob/main/LICENSE)
[![Platform](https://img.shields.io/badge/.NET-%3E%3D%20Standard%202.1%20%7C%206.0%20%7C%208.0%20%7C%2010.0-blue?style=flat-square)](#)
[![Build Status](https://img.shields.io/github/actions/workflow/status/melhelbawi/Aarkam.DDD.Domain/ci.yml?branch=main&style=flat-square)](https://github.com/melhelbawi/Aarkam.DDD.Domain/actions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aarkam.DDD.Domain?style=flat-square)](https://www.nuget.org/packages/Aarkam.DDD.Domain)

A high-performance, enterprise-grade, lightweight foundation package for **Domain-Driven Design (DDD)** and **Event Sourcing (ES)** patterns in modern .NET ecosystems.

Engineered with multi-target cross-runtime portability, zero-allocation micro-optimizations, and strict encapsulation semantics, **Aarkam.DDD.Domain** provides pristine abstractions without binding your domain core to concrete infrastructural dependencies—inspired by the robust architectural elements of **Volo.ABP Framework**, **MassTransit**, and **Marten**.

---

## 📖 Table of Contents

- [Quick Start](#quick-start-30-seconds)
- [Installation](#installation)
- [Core Concepts](#core-concepts)
- [Architectural Patterns](#architectural-patterns)
- [Usage Examples](#usage-examples)
- [Performance Characteristics](#performance-characteristics)
- [Security Considerations](#security-considerations)
- [Comparison to Alternatives](#comparison-to-alternatives)
- [Contributing](#contributing)
- [License](#license)

---

## ⚡ Quick Start (30 Seconds)

```bash
dotnet add package Aarkam.DDD.Domain
```

```csharp
// Define a Value Object (immutable, equality-by-value)
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        DomainGuard.AgainstNegative(amount, nameof(amount));
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Define an Entity (identity-based, mutable)
public class OrderItem : Entity<Guid>
{
    public string ProductSku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int Quantity { get; private set; }

    protected OrderItem() { }  // For EF Core

    public OrderItem(Guid id, string sku, Money price, int quantity) : base(id)
    {
        DomainGuard.AgainstNullOrEmpty(sku, nameof(sku));
        DomainGuard.AgainstNegativeOrZero(quantity, nameof(quantity));
        
        ProductSku = sku;
        Price = price;
        Quantity = quantity;
    }
}

// Define an Aggregate Root with Domain Events
public sealed class OrderPlacedEvent : DomainEvent<Order>
{
    public string CustomerId { get; init; } = null!;
    public decimal TotalValue { get; init; }
}

public class Order : AggregateRoot<Guid>
{
    public string CustomerId { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    protected Order() { }  // For EF Core

    public Order(Guid id, string customerId) : base(id)
    {
        DomainGuard.AgainstNullOrEmpty(customerId, nameof(customerId));
        RaiseEvent(new OrderPlacedEvent(id.ToString(), Version + 1, customerId, 0));
    }

    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is OrderPlacedEvent e)
        {
            Id = Guid.Parse(e.AggregateId);
            CustomerId = e.CustomerId;
            TotalAmount = e.TotalValue;
            Status = OrderStatus.Created;
        }
    }
}

public enum OrderStatus { Created, Processing, Shipped, Completed }
```

That's it! You now have type-safe, fully-audited, event-sourced domain models.

---

## 📦 Installation

### Via .NET CLI
```bash
dotnet add package Aarkam.DDD.Domain
```

### Via Package Manager
```powershell
Install-Package Aarkam.DDD.Domain
```

### Via NuGet Package Explorer
Search for `Aarkam.DDD.Domain` on [nuget.org](https://www.nuget.org/packages/Aarkam.DDD.Domain)

### Supported Platforms
- `.NET Standard 2.1` (Xamarin, Unity, legacy .NET Framework)
- `.NET 6.0` (LTS)
- `.NET 8.0` (LTS)
- `.NET 10.0` (Current)

---

## 🏛️ Core Concepts

### 1. **Entity<TKey>** — Thread of Continuity

An Entity is defined by its **identity**, not its attributes. Two entities with the same ID are considered the same, even if their attributes differ.

```csharp
public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>>
    where TKey : IEquatable<TKey>
{
    public virtual TKey Id { get; protected set; }
    
    public virtual bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default);
}
```

**Key Features:**
- ✅ Identity-based equality (not reference-based)
- ✅ Transient detection (unpersisted entities)
- ✅ ORM-friendly parameterless constructor
- ✅ Protected setters guard against external mutation

### 2. **ValueObject** — Structural Immutability

A Value Object has **no identity**. Two value objects are completely interchangeable if their attributes match. They must be immutable.

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        foreach (var component in GetEqualityComponents())
            hashCode.Add(component);
        return hashCode.ToHashCode();
    }
}
```

**Key Features:**
- ✅ Attribute-based equality (structural comparison)
- ✅ Works safely in HashSet<T> and Dictionary<K,V>
- ✅ Immutability enforced via design
- ✅ Composable (value objects can contain other value objects)

### 3. **AggregateRoot<TKey>** — Transactional Boundary

An Aggregate Root is the entry point for a cluster of entities. External code can only reference an aggregate via its root.

```csharp
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
{
    private readonly List<IDomainEvent> _changes = [];
    
    public virtual int Version { get; protected set; } = -1;
    public virtual Dictionary<string, object?> ExtraProperties { get; protected set; }
    public virtual bool IsDeleted { get; protected set; }
    public virtual string? TenantId { get; protected set; }
    
    protected void RaiseEvent(IDomainEvent domainEvent) => _changes.Add(domainEvent);
    protected abstract void ApplyEvent(IDomainEvent domainEvent);
    public IReadOnlyCollection<IDomainEvent> GetChanges() => _changes.AsReadOnly();
}
```

**Key Features:**
- ✅ Event sourcing support (RaiseEvent + ApplyEvent pattern)
- ✅ Multi-tenancy built-in (TenantId)
- ✅ Soft deletes (IsDeleted flag + EF Global Query Filters)
- ✅ Audit tracking (CreatorId, CreationTime, LastModifierId, LastModificationTime)
- ✅ Extensible properties (ExtraProperties for dynamic attributes)
- ✅ Optimistic concurrency (Version field for race condition detection)
- ✅ Zero-copy event collection (GetChanges returns direct list cast)

### 4. **DomainGuard** — Fail-Fast Validation

Guards prevent garbage collection (GC) thrashing via compiled lambda exception factories.

```csharp
public static class DomainGuard
{
    public static void AgainstNull([NotNull] object? value, string parameterName);
    public static void AgainstNullOrEmpty([NotNull] string? value, string parameterName);
    public static void AgainstNegative(long value, string parameterName);
    public static void AgainstNegativeOrZero(long value, string parameterName);
    public static void AgainstOutOfRange(int value, int min, int max, string parameterName);
    public static void CheckRule(IBusinessRule rule);
}
```

**Performance:** Uses compiled expression factories instead of Activator.CreateInstance(), eliminating reflection overhead.

### 5. **IBusinessRule** — Complex Invariants

Encapsulate multi-field business logic without polluting the aggregate with conditional chains.

```csharp
public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}

public sealed class CustomerMustHaveValidEmailRule : IBusinessRule
{
    private readonly string _email;
    public CustomerMustHaveValidEmailRule(string email) => _email = email;
    
    public bool IsBroken() => !_email.Contains("@");
    public string Message => "Email format is invalid.";
}

// Usage
public void UpdateEmail(string newEmail)
{
    DomainGuard.AgainstNullOrEmpty(newEmail, nameof(newEmail));
    DomainGuard.CheckRule(new CustomerMustHaveValidEmailRule(newEmail));
    Email = newEmail;
}
```

### 6. **IDomainEvent** — Immutable Change Facts

Events are the single source of truth for state changes. They are append-only and immutable.

```csharp
public interface IDomainEvent
{
    string AggregateId { get; }
    int Version { get; }
    DateTime OccurredAt { get; }
    Dictionary<string, object>? Metadata { get; }
}

public abstract class DomainEvent<TAggregate> : IDomainEvent
    where TAggregate : AggregateRoot<Guid>
{
    public string AggregateId { get; init; } = null!;
    public int Version { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; init; }
}
```

### 7. **ISpecification<T>** — Translatable Queries

Build complex queries that translate cleanly to SQL via EF Core.

```csharp
public sealed class ActiveOrdersSpecification : SpecificationBase<Order>
{
    public ActiveOrdersSpecification(string customerId, int pageIndex = 0, int pageSize = 10)
    {
        AddInclude(o => o.Items);
        AddInclude(o => o.Payments);
        OrderByDescending = o => o.CreationTime;
        Skip = pageIndex * pageSize;
        Take = pageSize;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.CustomerId == customerId 
            && !order.IsDeleted
            && order.Status != OrderStatus.Completed;
    }
}

// Usage in Repository
public async Task<List<Order>> GetActiveOrdersAsync(string customerId)
{
    var spec = new ActiveOrdersSpecification(customerId);
    return await _context.Orders
        .Where(spec.ToExpression())
        .ToListAsync();
}
```

---

## 🏗️ Architectural Patterns Reference

| Component | DDD Pattern | Use Case |
|-----------|-------------|----------|
| `Entity<TKey>` | Thread of Continuity | Objects with persistent identity |
| `ValueObject` | Structural Immutability | Money, Address, Email, Coordinates |
| `AggregateRoot<TKey>` | Transactional Boundary | Order, Customer, Invoice |
| `IDomainEvent` | Append-Only Facts | Order Placed, Payment Received |
| `IBusinessRule` | Invariant Validation | Email must be unique, quantity > 0 |
| `ISpecification<T>` | Query Translation | Filtered searches, pagination |
| `IDomainService` | Stateless Cross-Aggregate | Calculating discounts, validating policies |
| `IInboxMessage` / `IOutboxMessage` | Transactional Messaging | Idempotent message handling |
| `ISnapshot` | State Baseline Rehydration | Optimizing event stream replays |
| `ISaga<TSagaData>` | Distributed Orchestration | Long-running workflows |

**Reference Literature:**
- Eric Evans, *Domain-Driven Design* (Evans, 2003)
- Vaughn Vernon, *Implementing Domain-Driven Design* (Vernon, 2013)
- Greg Young, *CQRS and Event Sourcing* (Young, 2010)
- Martin Fowler, *Patterns of Enterprise Application Architecture* (Fowler, 2002)
- Gregor Hohpe, *Enterprise Integration Patterns* (Hohpe & Woolf, 2003)

---

## 💻 Usage Examples

### Example 1: E-Commerce Order Management

```csharp
namespace Ecommerce.Orders.Domain;

// Value Objects
public sealed class OrderNumber : ValueObject
{
    public string Value { get; }
    
    public OrderNumber(string value)
    {
        DomainGuard.AgainstNullOrEmpty(value, nameof(value));
        Value = value;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

public sealed class OrderLineItem : ValueObject
{
    public string Sku { get; }
    public int Quantity { get; }
    public Money UnitPrice { get; }

    public OrderLineItem(string sku, int quantity, Money unitPrice)
    {
        DomainGuard.AgainstNullOrEmpty(sku, nameof(sku));
        DomainGuard.AgainstNegativeOrZero(quantity, nameof(quantity));
        DomainGuard.AgainstNull(unitPrice, nameof(unitPrice));
        
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money GetLineTotal() => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Sku;
        yield return Quantity;
        yield return UnitPrice;
    }
}

// Domain Events
public sealed class OrderCreatedEvent : DomainEvent<Order>
{
    public string CustomerId { get; init; } = null!;
    public List<OrderLineItem> LineItems { get; init; } = [];
    public Money TotalAmount { get; init; } = null!;
}

public sealed class OrderConfirmedEvent : DomainEvent<Order>
{
    public string ConfirmedBy { get; init; } = null!;
    public DateTime ConfirmedAt { get; init; }
}

public sealed class OrderCancelledEvent : DomainEvent<Order>
{
    public string CancellationReason { get; init; } = null!;
}

// Aggregate Root
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLineItem> _lineItems = [];

    public OrderNumber OrderNumber { get; private set; } = null!;
    public string CustomerId { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderLineItem> LineItems => _lineItems.AsReadOnly();

    protected Order() { }  // For EF Core

    public Order(Guid id, OrderNumber orderNumber, string customerId) : base(id)
    {
        DomainGuard.AgainstNull(orderNumber, nameof(orderNumber));
        DomainGuard.AgainstNullOrEmpty(customerId, nameof(customerId));

        OrderNumber = orderNumber;
        CustomerId = customerId;
        Status = OrderStatus.Draft;

        RaiseEvent(new OrderCreatedEvent
        {
            AggregateId = id.ToString(),
            Version = Version + 1,
            CustomerId = customerId
        });
    }

    public void AddLineItem(OrderLineItem lineItem)
    {
        DomainGuard.Against<InvalidOperationException>(
            Status != OrderStatus.Draft,
            "Cannot add items to a confirmed order."
        );
        DomainGuard.AgainstNull(lineItem, nameof(lineItem));

        _lineItems.Add(lineItem);
        RecalculateTotal();
    }

    public void Confirm(string confirmedBy)
    {
        DomainGuard.AgainstNullOrEmpty(confirmedBy, nameof(confirmedBy));
        DomainGuard.Against<InvalidOperationException>(
            _lineItems.Count == 0,
            "Cannot confirm an empty order."
        );

        Status = OrderStatus.Confirmed;
        RaiseEvent(new OrderConfirmedEvent
        {
            AggregateId = Id.ToString(),
            Version = Version + 1,
            ConfirmedBy = confirmedBy,
            ConfirmedAt = DateTime.UtcNow
        });
    }

    public void Cancel(string reason)
    {
        DomainGuard.AgainstNullOrEmpty(reason, nameof(reason));
        DomainGuard.Against<InvalidOperationException>(
            Status == OrderStatus.Completed,
            "Cannot cancel a completed order."
        );

        Status = OrderStatus.Cancelled;
        RaiseEvent(new OrderCancelledEvent
        {
            AggregateId = Id.ToString(),
            Version = Version + 1,
            CancellationReason = reason
        });
    }

    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderCreatedEvent e:
                OrderNumber = new(e.AggregateId);
                CustomerId = e.CustomerId;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Draft;
                break;

            case OrderConfirmedEvent:
                Status = OrderStatus.Confirmed;
                break;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                break;
        }
    }

    private void RecalculateTotal()
    {
        var total = _lineItems.Sum(li => li.GetLineTotal().Amount);
        TotalAmount = new(total, "USD");
    }
}

public enum OrderStatus { Draft, Confirmed, Processing, Shipped, Completed, Cancelled }
```

### Example 2: Multi-Tenant SaaS Application

```csharp
// Inject current tenant from authentication middleware
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ICurrentTenant _currentTenant;

    public OrderService(IOrderRepository repository, ICurrentTenant currentTenant)
    {
        _repository = repository;
        _currentTenant = currentTenant;
    }

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order(Guid.NewGuid(), request.OrderNumber, request.CustomerId);
        order.TenantId = _currentTenant.Id;  // Automatically scoped to tenant

        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();
    }

    // Tenant context switching for background jobs
    public async Task ProcessCrossTenantSyncAsync(string targetTenantId, Guid orderId)
    {
        using (_currentTenant.Change(targetTenantId))
        {
            var order = await _repository.GetAsync(orderId);
            order.Confirm("system");
            await _repository.UpdateAsync(order);
        }
    }
}
```

---

## ⚡ Performance Characteristics

### Zero-Allocation Optimizations

1. **GetChanges() Smart Cast**
   ```csharp
   public IReadOnlyCollection<IDomainEvent> GetChanges() 
       => _changes.AsReadOnly();  // Direct cast, no copy
   ```
   **Impact:** Eliminates allocation when collecting uncommitted events.

2. **Compiled Exception Factories**
   ```csharp
   private static class ExceptionFactory<TException> where TException : Exception
   {
       private static readonly Func<string, TException> _factory = CreateFactory();
       
       public static TException Create(string message) => _factory(message);
   }
   ```
   **Impact:** Guard clauses don't trigger reflection overhead; useful in tight loops.

3. **ValueObject HashCode Computation**
   - Uses modern `HashCode` struct (low collision rates)
   - Avoids legacy prime-multiplication algorithms
   - Safe for use in HashSet<T> and Dictionary<K,V>

### Benchmarks (Estimated)

| Operation | Time | Allocations |
|-----------|------|-------------|
| Entity.Equals() | <1 µs | 0 bytes |
| ValueObject.Equals() | 1-5 µs | 0 bytes |
| DomainGuard.AgainstNull() | <1 µs | 0 bytes (cached lambda) |
| Order.RaiseEvent() | <1 µs | 64 bytes (event object) |

**Note:** Benchmarks are illustrative. Run `dotnet benchmark` for precise measurements in your environment.

---

## 🔒 Security Considerations

### 1. Tenant Isolation

**Risk:** Accidental cross-tenant data leakage

**Mitigation:**
```csharp
// ✅ CORRECT: Inject TenantId from authenticated context
public void Create(CreateOrderRequest request)
{
    var order = new Order(Guid.NewGuid(), request.OrderNumber, request.CustomerId);
    order.TenantId = _currentTenant.Id;  // Set from middleware, not user input
    _repository.Add(order);
}

// ❌ WRONG: TenantId from user input
public void Create(CreateOrderRequest request)
{
    var order = new Order(Guid.NewGuid(), request.OrderNumber, request.CustomerId);
    order.TenantId = request.TenantId;  // SECURITY BUG: trusting user input!
}
```

**Best Practice:** Use `ICurrentTenant` middleware that derives TenantId from JWT claims or session.

### 2. Entity Serialization

**Risk:** Sensitive data exposed via JSON serialization

**Mitigation:**
```csharp
public class Order : AggregateRoot<Guid>
{
    [JsonIgnore]
    public string? InternalNotes { get; private set; }

    [JsonIgnore]
    public Dictionary<string, object?> ExtraProperties { get; protected set; }
}
```

### 3. Guard Clause Validation

**Always validate at aggregate boundaries:**
```csharp
public void UpdatePrice(Money newPrice)
{
    DomainGuard.AgainstNull(newPrice, nameof(newPrice));
    DomainGuard.AgainstNegative(newPrice.Amount, nameof(newPrice.Amount));
    
    // Only here is the price safe to mutate
    Price = newPrice;
}
```

---

## 🎯 Comparison to Alternatives

### vs. Volo.ABP Framework

| Aspect | Aarkam.DDD.Domain | Volo.ABP |
|--------|-------------------|---------|
| **Dependency Injection** | 0 (pure types) | Full integration |
| **Database Abstraction** | None (works with any ORM) | Built-in repositories |
| **Authorization** | 0 (leave to middleware) | RBAC + ABP.Authorization |
| **Performance** | Optimized (zero-allocation) | Full-featured (some overhead) |
| **Learning Curve** | Minimal | Steep (more to learn) |
| **Use Case** | Foundation library | Complete framework |

**When to use Aarkam:** You want DDD patterns without framework lock-in.  
**When to use ABP:** You need a complete, opinionated platform.

### vs. MassTransit

| Aspect | Aarkam.DDD.Domain | MassTransit |
|--------|-------------------|------------|
| **Purpose** | Domain modeling | Message orchestration |
| **Event Sourcing** | Built-in contracts | External (via Marten) |
| **Sagas** | Interface defined | Fully implemented |
| **Message Bus** | 0 (BYOB) | Integrated |

**Complementary:** Use both! Aarkam for domain, MassTransit for distribution.

### vs. Marten

| Aspect | Aarkam.DDD.Domain | Marten |
|--------|-------------------|--------|
| **Database** | Agnostic (any DB) | PostgreSQL only |
| **Event Store** | Contracts only | Full implementation |
| **ACID Snapshots** | Via user code | Built-in |
| **Query Optimization** | EF Core specs | Marten projections |

**Complementary:** Use Aarkam for domain types, Marten for PostgreSQL event store backend.

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Report issues** via GitHub Issues (include reproduction steps)
2. **Fork and branch** from `develop` branch
3. **Write tests** for new features (xUnit)
4. **Follow code style** (nullable annotations, XML docs, 120-char lines)
5. **Commit messages** in conventional format: `feat: add ISaga interface`
6. **Submit PR** with description and linked issue

**Development Setup:**
```bash
git clone https://github.com/melhelbawi/Aarkam.DDD.Domain.git
cd Aarkam.DDD.Domain
dotnet restore
dotnet test
dotnet build
```

---

## 📋 Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** (X.0.0): Breaking API changes
- **MINOR** (0.X.0): New features, backward-compatible
- **PATCH** (0.0.X): Bug fixes, no API changes

**Stability:**
- `1.0.0+`: Production-ready
- `0.x.x`: Preview (API may change)

---

## 📄 License

Distributed under the **MIT License**. See [LICENSE](https://github.com/melhelbawi/Aarkam.DDD.Domain/blob/main/LICENSE) for details.

```
Copyright (c) 2026 Mohamed Elhelbawi / Aarkam

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions...
```

---

## 🙏 Acknowledgments

Built with inspiration from:

- **Eric Evans** — Domain-Driven Design (Blue Book)
- **Vaughn Vernon** — Implementing DDD
- **Greg Young** — Event Sourcing Patterns
- **Martin Fowler** — Enterprise Architecture Patterns
- **Volo.ABP Team** — ABP Framework design philosophy
- **MassTransit Contributors** — Distributed systems patterns

---

## 📞 Support

- **Documentation:** [GitHub Wiki](https://github.com/melhelbawi/Aarkam.DDD.Domain/wiki)
- **Issues:** [GitHub Issues](https://github.com/melhelbawi/Aarkam.DDD.Domain/issues)
- **Discussions:** [GitHub Discussions](https://github.com/melhelbawi/Aarkam.DDD.Domain/discussions)

---

**Made with ❤️ by Mohamed Elhelbawi**

---

## 📚 Further Reading

- [Martin Fowler on DDD](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [Vaughn Vernon's IDDD Learning Path](https://vaughnvernon.com/implementing-domain-driven-design/)
- [Greg Young on Event Sourcing](https://www.youtube.com/watch?v=JHGkaShoyNs)
- [CQRS & Event Sourcing Patterns](https://learn.microsoft.com/en-us/dotnet/architecture/cqrs-microservices-saga/)

