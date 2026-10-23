# Aarkam.DDD.Domain

[![NuGet Package](https://img.shields.io/nuget/v/Aarkam.DDD.Domain.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Aarkam.DDD.Domain/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aarkam.DDD.Domain.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Aarkam.DDD.Domain/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://github.com/melhelbawi/Aarkam.DDD.Domain/blob/main/LICENSE)
[![Platform](https://img.shields.io/badge/.NET-%3E%3D%20Standard%202.1%20%7C%206.0%20%7C%208.0%20%7C%2010.0-blue?style=flat-square)](https://www.nuget.org/packages/Aarkam.DDD.Domain/)
[![AOT Ready](https://img.shields.io/badge/Native%20AOT-Supported-brightgreen?style=flat-square)](https://github.com/melhelbawi/Aarkam.DDD.Domain)

A high-performance, enterprise-grade, lightweight foundation package for **Domain-Driven Design (DDD)** and **Event Sourcing (ES)** abstractions in modern .NET ecosystems.

We are a unique, standalone, zero-dependency Domain foundation crafted from the ground up for modern enterprise architecture. Engineered with multi-target cross-runtime portability (`netstandard2.1`, `net6.0`, `net8.0`, `net10.0`), Native AOT & IL trimming compliance, zero-allocation micro-optimizations, strict domain encapsulation, and **Structured Error Handling** with machine-readable `ErrorCode` and `Severity` categorization.

**Repository:** [https://github.com/melhelbawi/Aarkam.DDD.Domain](https://github.com/melhelbawi/Aarkam.DDD.Domain)  
**NuGet Package:** [https://www.nuget.org/packages/Aarkam.DDD.Domain/](https://www.nuget.org/packages/Aarkam.DDD.Domain/)

---

## 📋 Executive Summary

In enterprise software engineering, enterprise microservices, and distributed cloud applications, domain models are frequently polluted by infrastructural frameworks, ORM coupling, and inconsistent error handling. This creates technical debt, slows down modernization, and makes unit testing unnecessarily complex.

**Aarkam.DDD.Domain** establishes a pure, pristine, and unified domain layer. It provides battle-tested building blocks—Entities, Value Objects, Smart Enums, Aggregate Roots, Domain Events, Specifications, Sagas, Auditing, and Multi-Tenancy—without forcing framework lock-in. Furthermore, it pioneers **Structured Domain Error Handling**, transitioning applications from brittle text-based exception messages to machine-readable error codes and impact-driven severity levels designed for modern observability stacks (e.g., Serilog, Datadog, OpenTelemetry) and frontend localization engines.

---

## 🚀 Key Features

- ⚡ **Native AOT & Trimming Supported**: Built with zero-reflection hot paths and explicit member preservation annotations (`[DynamicallyAccessedMembers]`), making it 100% compatible with .NET 8 / 10 Ahead-of-Time (AOT) compilation for lightning-fast container startup and minimal cloud memory footprints.
- 🛡️ **Structured Domain Error Handling**: Eliminates fragile string parsing. Every domain exception carries a standardized machine-readable `ErrorCode`, an operational `Severity` (`Warning`, `Error`, `Critical`), and an enriched contextual `Details` dictionary.
- 🏷️ **Smart Enums (`Enumeration`)**: Replaces anemic primitive C# `enum`s with strongly-typed, behavioral domain objects featuring display formatting, state-machine transitions, and cached O(1) lookups.
- 🔄 **Event Sourcing & Decoupled Domain Events**: Native support for both pure Event-Sourced aggregates (`AggregateRoot<TKey>`) with state rehydration (`LoadFromHistory`) and traditional entities that collect pending changes (`IHasDomainEvents`).
- 🔒 **Pure Domain Invariants & Guard Clauses**: Decouples business rules into isolated, testable specification objects (`IBusinessRule`) and provides high-performance, AOT-friendly guard clauses (`DomainGuard`).
- 🔍 **Composable Specifications (`ISpecification<T>`)**: Encapsulates query logic, eager navigation paths (`Includes`), sorting, and pagination into reusable domain components translatable to SQL by ORMs like EF Core.
- 📜 **Full Lifecycle Auditing**: Cleanly segregated contracts for Creation (`ICreationAuditableEntity`), Modification (`IModificationAuditableEntity`), Soft Deletion (`ISoftDelete`, `IDeletionAuditableEntity`), and composite Full Auditing (`IFullAuditableEntity<TKey>`).
- 🏢 **Multi-Tenancy Isolation**: First-class logical data partitioning with `IHasTenant`, ambient thread-safe context flow (`ICurrentTenant`), and scoped execution switches (`Change(string? tenantId)`).
- 📨 **Resilient Messaging & Distributed Consistency**: Ready-to-implement contracts for the Transactional Outbox pattern (`IOutboxMessage`), Idempotent Consumer Inbox (`IInboxMessage`), Process Managers / Sagas (`ISaga<TSagaData>`), Snapshots (`ISnapshot`), and CQRS Projections (`IProjection`).
- 🌐 **Multi-Target Modern .NET**: Fully supports `.NET Standard 2.1`, `.NET 6.0`, `.NET 8.0`, and `.NET 10.0`.

---

## 📦 Installation

Install via the .NET CLI:

```bash
dotnet add package Aarkam.DDD.Domain
```

Or via Package Manager:

```powershell
Install-Package Aarkam.DDD.Domain
```

---

## 🛡️ Structured Domain Exception Handling

Organizing custom exceptions in C# with machine-readable `ErrorCode` and `Severity` categorization is an industry best practice for enterprise systems, Web APIs, and microservices. It transitions your application from primitive text-based error handling to **Structured Error Handling**.

### Why Structured Error Handling?

| Property | Purpose | Benefit |
| :--- | :--- | :--- |
| **`ErrorCode`** | A unique, machine-readable identifier (e.g., `ERR_AUTH_001`, `USER_NOT_FOUND`, `ERR_INSUFFICIENT_FUNDS`). | Frontend applications and API consumers can display localized, user-friendly messages without relying on fragile string parsing. |
| **`Severity`** | Categorizes the operational impact (`Warning`, `Error`, `Critical`). | Allows logging and observability systems (Serilog, Datadog, Application Insights) to trigger immediate alerts for `Critical` issues while silently indexing `Warning` metrics. |
| **`Details`** | Key-value dictionary containing contextual debugging telemetry. | Provides enriched structured logging payloads (e.g., `EntityId`, `TenantId`, `BrokenRuleType`) without leaking sensitive data into public exception messages. |

### Exception Severity Architecture

```csharp
public enum ExceptionSeverity
{
    /// <summary>
    /// Non-fatal, expected business validation or client-side deviation (e.g., invalid input, transient condition).
    /// Typically logged as an informational or warning metric without triggering on-call alerts.
    /// </summary>
    Warning,

    /// <summary>
    /// Standard domain violation or business invariant failure (e.g., entity conflict, forbidden action).
    /// Typically logged as an application error and mapped to 4xx HTTP status codes.
    /// </summary>
    Error,

    /// <summary>
    /// High-impact or structural system condition (e.g., data corruption, concurrency write race, missing event).
    /// Triggers immediate logging alerts and potential retry or compensation workflows.
    /// </summary>
    Critical
}
```

### Base DomainException & Concrete Exceptions

All domain exceptions derive from the abstract base class `DomainException`:

```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public ExceptionSeverity Severity { get; }
    public IDictionary<string, object?> Details { get; }

    public DomainException WithDetail(string key, object? value);
}
```

#### Out-of-the-Box Structured Exceptions:

- **`BusinessRuleValidationException`**: Raised when an `IBusinessRule` fails. Automatically derives `ErrorCode` and `Severity` directly from the broken specification!
- **`EntityNotFoundException`**: Default ErrorCode: `ERR_ENTITY_NOT_FOUND` | Severity: `Warning`. Captures `EntityType` and `EntityId` in `Details`.
- **`DomainInvalidOperationException`**: Default ErrorCode: `ERR_DOMAIN_INVALID_OPERATION` | Severity: `Error`. For invalid state-machine transitions.
- **`DomainConflictException`**: Default ErrorCode: `ERR_DOMAIN_CONFLICT` | Severity: `Error`. For logical collisions.
- **`DomainForbiddenException`**: Default ErrorCode: `ERR_DOMAIN_FORBIDDEN` | Severity: `Warning`. For internal domain authorization rules.
- **`AggregateConcurrencyException`**: Default ErrorCode: `ERR_CONCURRENCY_CONFLICT` | Severity: `Critical`. Captures `AggregateType` and `AggregateId` in `Details`.
- **`DomainEventIsNullException`**: Default ErrorCode: `ERR_DOMAIN_EVENT_NULL` | Severity: `Critical`.
- **`DomainValidationException`**: Default ErrorCode: `ERR_DOMAIN_VALIDATION` | Severity: `Warning`. Contains `IReadOnlyList<DomainValidationError> Errors` to prevent single-error roundtrips.

### Observability & Logging Integration (Serilog / Datadog)

```csharp
// Example Global Middleware / Filter
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex)
    {
        // Enriched structured logging using ErrorCode and Severity
        switch (ex.Severity)
        {
            case ExceptionSeverity.Warning:
                logger.LogWarning(ex, "[{ErrorCode}] Domain Warning: {Message} | Details: {@Details}", ex.ErrorCode, ex.Message, ex.Details);
                break;
            case ExceptionSeverity.Error:
                logger.LogError(ex, "[{ErrorCode}] Domain Business Error: {Message} | Details: {@Details}", ex.ErrorCode, ex.Message, ex.Details);
                break;
            case ExceptionSeverity.Critical:
                logger.LogCritical(ex, "[{ErrorCode}] CRITICAL Domain Invariant Tripped: {Message} | Details: {@Details}", ex.ErrorCode, ex.Message, ex.Details);
                // Trigger alerting systems (PagerDuty / Opsgenie)
                break;
        }

        context.Response.StatusCode = ex switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            DomainForbiddenException => StatusCodes.Status403Forbidden,
            DomainConflictException or AggregateConcurrencyException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        await context.Response.WriteAsJsonAsync(new
        {
            errorCode = ex.ErrorCode,
            message = ex.Message,
            severity = ex.Severity.ToString(),
            details = ex.Details
        });
    }
});
```

---

## 🏛️ Core DDD Abstractions

### 1. Entity & IEntity (Thread of Continuity)

Entities are defined by persistent identity. `IEntity` provides a non-generic marker interface exposing `GetKeys()` for composite or simple keys, while `Entity<TKey>` handles equality:

```csharp
public class Customer : Entity<Guid>
{
    public string Name { get; private set; }

    public Customer(Guid id, string name) : base(id)
    {
        DomainGuard.AgainstNullOrEmpty(name, nameof(name));
        Name = name;
    }
}
```

### 2. ValueObject (Structural Immutability)

Value Objects have no identity; two instances are equal if all attributes match:

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        DomainGuard.AgainstNegative(amount, nameof(amount));
        DomainGuard.AgainstNullOrEmpty(currency, nameof(currency));
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### 3. Enumeration / Smart Enum (Behavioral Discrete Choices)

Native AOT-compatible smart enums that encapsulate business methods, display labels, and transitions:

```csharp
public sealed class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft = new(1, nameof(Draft), "Draft Order");
    public static readonly OrderStatus Confirmed = new(2, nameof(Confirmed), "Confirmed Order");
    public static readonly OrderStatus Shipped = new(3, nameof(Shipped), "Shipped to Carrier");
    public static readonly OrderStatus Cancelled = new(4, nameof(Cancelled), "Cancelled");

    private OrderStatus(int id, string name, string? displayName) 
        : base(id, name, displayName) { }

    public bool CanCancel() => this == Draft || this == Confirmed;
}

// O(1) Cached Lookup:
var status = Enumeration.FromValue<OrderStatus>(2); // Confirmed
```

### 4. AggregateRoot (Transactional Boundary & Event Sourcing)

Encapsulates aggregate graph invariants, event sourcing stream tracking, and optimistic concurrency versioning:

```csharp
public class Order : AggregateRoot<Guid>
{
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public Money TotalAmount { get; private set; } = new(0, "USD");

    protected Order() { } // EF Core / Deserialization Constructor

    public Order(Guid id, string customerId) : base(id)
    {
        RaiseEvent(new OrderCreatedEvent(id.ToString(), Version + 1, customerId));
    }

    public void Confirm()
    {
        CheckRule(new OrderMustBeInDraftStateRule(Status));
        RaiseEvent(new OrderConfirmedEvent(Id.ToString(), Version + 1));
    }

    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderCreatedEvent:
                Status = OrderStatus.Draft;
                break;
            case OrderConfirmedEvent:
                Status = OrderStatus.Confirmed;
                break;
        }
    }
}
```

### 5. Domain Events & IHasDomainEvents

- `IDomainEvent`: Immutable contract capturing historical business facts (`EventId`, `OccurredOn`, `AggregateType`, `AggregateId`, `Version`).
- `IHasDomainEvents`: Decoupled interface for any entity to expose uncommitted changes (`GetChanges()`, `MarkEventsAsCommitted()`).
- `IEventHandler<in TEvent>`: Contract for asynchronous event handlers.

### 6. Business Rules & DomainGuard

Encapsulate atomic invariants into reusable, testable specification objects:

```csharp
public sealed class OrderMustBeInDraftStateRule : IBusinessRule
{
    private readonly OrderStatus _status;
    public OrderMustBeInDraftStateRule(OrderStatus status) => _status = status;

    public bool IsBroken() => _status != OrderStatus.Draft;
    public string Message => $"Order cannot proceed because current state is '{_status}'.";
    public string ErrorCode => "ERR_ORDER_INVALID_STATE";
    public ExceptionSeverity Severity => ExceptionSeverity.Error;
}

// Inline assertion:
DomainGuard.CheckRule(new OrderMustBeInDraftStateRule(order.Status));
```

### 7. Composable Specifications

```csharp
public sealed class PendingOrdersSpecification : SpecificationBase<Order>
{
    public PendingOrdersSpecification(string customerId)
    {
        AddInclude(o => o.Items);
        OrderByDescending = o => o.CreationTime;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return o => o.CustomerId == customerId && !o.IsDeleted;
    }
}
```

Specifications support clean logical combinations: `specA.And(specB)`, `specA.Or(specB)`, `spec.Not()`.

### 8. Auditing & Full Lifecycle Tracking

- `ICreationAuditableEntity` & `CreationAuditableEntity<TKey>`
- `IModificationAuditableEntity` & `ModificationAuditableEntity<TKey>`
- `ISoftDelete`, `IDeletionAuditableEntity` & `DeletionAuditableEntity<TKey>`
- `IFullAuditableEntity<TKey>` & `FullAuditableEntity<TKey>` (Unites creation, modification, soft-deletion, and dynamic `ExtraProperties`)

### 9. Multi-Tenancy

- `IHasTenant`: Tags entities with `TenantId`.
- `ICurrentTenant`: Injected into services to read or scoped-change the active tenant boundary (`using (_currentTenant.Change(tenantId)) { ... }`).
- `AmbientTenantContext`: Safe `AsyncLocal` ambient thread context.

### 10. Event Sourcing, Sagas & Outbox Contracts

- `IIntegrationEvent`: Telemetry-enriched cross-boundary event contract (`CorrelationId`, `CausationId`).
- `IOutboxMessage`: Prevents distributed transaction dual-write failure.
- `IInboxMessage`: Enforces idempotent consumer deduplication.
- `ISaga<TSagaData>`: Process Manager abstraction with compensation stages (`SagaStatus`).
- `ISnapshot`: High-speed baseline state rehydration.
- `IProjection<in TEvent>`: CQRS read-model transformation.

---

## ⚡ Quick Start (30 Seconds)

```csharp
using Aarkam.DDD.Domain;
using Aarkam.DDD.Domain.Entities;

// 1. Define a Smart Enum
public sealed class Priority : Enumeration
{
    public static readonly Priority Normal = new(1, nameof(Normal), "Normal Priority");
    public static readonly Priority High = new(2, nameof(High), "High Priority");
    private Priority(int id, string name, string? displayName) : base(id, name, displayName) { }
}

// 2. Define a Domain Event
public sealed class TaskAssignedEvent : DomainEvent<ProjectTask>
{
    public string AssigneeId { get; init; }
    public TaskAssignedEvent(string aggregateId, int version, string assigneeId) 
        : base(aggregateId, version)
    {
        AssigneeId = assigneeId;
    }
}

// 3. Define an Aggregate Root
public class ProjectTask : AggregateRoot<Guid>
{
    public string Title { get; private set; } = null!;
    public Priority Priority { get; private set; } = Priority.Normal;
    public string? AssigneeId { get; private set; }

    protected ProjectTask() { } // For EF Core

    public ProjectTask(Guid id, string title, Priority priority) : base(id)
    {
        DomainGuard.AgainstNullOrEmpty(title, nameof(title));
        Title = title;
        Priority = priority ?? Priority.Normal;
    }

    public void AssignTo(string assigneeId)
    {
        DomainGuard.AgainstNullOrEmpty(assigneeId, nameof(assigneeId));
        RaiseEvent(new TaskAssignedEvent(Id.ToString(), Version + 1, assigneeId));
    }

    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is TaskAssignedEvent e)
        {
            AssigneeId = e.AssigneeId;
        }
    }
}
```

---

## 📊 Architectural Reference Matrix

| Component | DDD Classification | Enterprise Role |
| :--- | :--- | :--- |
| `Entity<TKey>` / `IEntity` | Entity | Persistent identity & continuity |
| `ValueObject` | Value Object | Structural attribute immutability |
| `Enumeration` | Smart Enum | Type-safe, behavioral domain constants with AOT support |
| `AggregateRoot<TKey>` | Aggregate Root | Transactional consistency boundary & event sourcing |
| `IDomainEvent` | Domain Event | Immutable record of business changes within the boundary |
| `IHasDomainEvents` | Event Provider | Decoupled event collection for unit-of-work dispatch |
| `DomainException` | Structured Error | Typed business exceptions with `ErrorCode`, `Severity`, & `Details` |
| `IBusinessRule` | Business Invariant | Encapsulated rule specification with custom severity |
| `ISpecification<T>` | Specification | Reusable, composable LINQ query criteria |
| `IDomainService` | Domain Service | Stateless cross-aggregate orchestration |
| `IFullAuditableEntity` | Audit Trail | Complete creation, mutation, and soft-deletion metadata |
| `IHasTenant` / `ICurrentTenant` | Multi-Tenancy | Logical data partitioning and tenant context switching |
| `IIntegrationEvent` | Distributed Event | Cross-service asynchronous facts with tracing telemetry |
| `IOutboxMessage` / `IInboxMessage` | Resilient Messaging | Dual-write protection & idempotent consumer deduplication |
| `ISaga<TSagaData>` | Process Manager | Distributed choreography and compensating workflows |
| `ISnapshot` | Performance Baseline | High-speed rehydration for long-running event streams |
| `IProjection<in TEvent>` | CQRS Projection | Read-model transformation from historical event facts |

---

## 👥 Authors & Maintainers

- **Eng. Mohamed ElHelbawi** ([@melhelbawi](https://github.com/melhelbawi))
- **Aarkam Team** ([aarkam.io](https://aarkam.io/))

---

## 📄 License

This project is licensed under the **[MIT License](https://github.com/melhelbawi/Aarkam.DDD.Domain/blob/main/LICENSE)**.

Copyright © 2026 [Aarkam](https://aarkam.io/) & [Mohamed Elhelbawi](https://github.com/melhelbawi). All rights reserved.
