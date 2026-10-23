using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class EventSourcingAndMessagingTests
{
    private sealed class OrderAggregate : AggregateRoot<Guid>
    {
        public OrderAggregate() : base() { }
        public OrderAggregate(Guid id) : base(id) { }
    }

    private sealed class OrderPlacedDomainEvent : DomainEvent<OrderAggregate, Guid>
    {
        public decimal TotalAmount { get; }

        public OrderPlacedDomainEvent(Guid orderId, decimal totalAmount, int version)
            : base(orderId.ToString(), version)
        {
            TotalAmount = totalAmount;
        }
    }

    private sealed class GenericGuidOrderEvent : DomainEvent<OrderAggregate>
    {
        public GenericGuidOrderEvent(Guid orderId, int version)
            : base(orderId.ToString(), version)
        {
        }
    }

    private sealed class CustomIntegrationEvent : IntegrationEvent
    {
        public string OrderNumber { get; }

        public CustomIntegrationEvent(string orderNumber, Guid? correlationId = null, Guid? causationId = null)
            : base(correlationId, causationId)
        {
            OrderNumber = orderNumber;
        }
    }

    private sealed class TestOutboxMessage : IOutboxMessage
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string EventType { get; set; } = "OrderPlaced";
        public string Payload { get; set; } = "{}";
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; private set; }
        public int RetryCount { get; private set; }
        public string? LastError { get; private set; }
        public string? TenantId { get; set; }
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        public void MarkAsProcessed()
        {
            ProcessedAt = DateTimeOffset.UtcNow;
        }

        public void LogFailure(string errorMessage)
        {
            RetryCount++;
            LastError = errorMessage;
        }
    }

    private sealed class TestInboxMessage : IInboxMessage
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public string EventType { get; init; } = "OrderPlaced";
        public string ConsumerName { get; init; } = "EmailNotifierConsumer";
        public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
        public string? TenantId { get; init; }
    }

    private sealed class OrderSagaData
    {
        public Guid OrderId { get; set; }
        public bool InventoryReserved { get; set; }
        public bool PaymentProcessed { get; set; }
    }

    private sealed class OrderSaga : ISaga<OrderSagaData>
    {
        public Guid SagaId { get; } = Guid.NewGuid();
        public OrderSagaData Data { get; } = new();
        public SagaStatus Status { get; private set; } = SagaStatus.Pending;
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; private set; }
        public long Version { get; private set; }
        public string? FailureReason { get; private set; }

        public void Start()
        {
            Status = SagaStatus.Running;
            Version++;
        }

        public void Complete()
        {
            Status = SagaStatus.Completed;
            CompletedAt = DateTimeOffset.UtcNow;
            Version++;
        }

        public void Fail(string reason)
        {
            Status = SagaStatus.Failed;
            FailureReason = reason;
            CompletedAt = DateTimeOffset.UtcNow;
            Version++;
        }

        public void Compensate()
        {
            Status = SagaStatus.Compensating;
            Version++;
        }
    }

    private sealed class TestSnapshot : ISnapshot
    {
        public string AggregateId { get; init; } = Guid.NewGuid().ToString();
        public string AggregateType { get; init; } = nameof(OrderAggregate);
        public long Version { get; init; } = 42;
        public string State { get; init; } = "{\"Status\":\"Shipped\"}";
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public string? TenantId { get; init; }
    }

    private sealed class TestProjection : IProjection<OrderPlacedDomainEvent>
    {
        public string ProjectionName => "OrderReadModelProjection";
        public long Position { get; private set; }
        public DateTimeOffset? LastProcessedAt { get; private set; }
        public List<OrderPlacedDomainEvent> ReceivedEvents { get; } = [];

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            Position = 0;
            LastProcessedAt = null;
            ReceivedEvents.Clear();
            return Task.CompletedTask;
        }

        public Task ApplyAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(@event);
            Position++;
            LastProcessedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
    }

    #region DomainEventBase Tests

    [Fact]
    public void DomainEventBase_ExplicitConstructor_InitializesProperties()
    {
        var evt = new OrderPlacedDomainEvent(Guid.NewGuid(), 250m, 3);

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.True(evt.OccurredOn <= DateTimeOffset.UtcNow);
        Assert.Equal(nameof(OrderAggregate), evt.AggregateType);
        Assert.Equal(3, evt.Version);
        Assert.Equal(250m, evt.TotalAmount);
    }

    [Theory]
    [InlineData(null, "agg-1")]
    [InlineData("", "agg-1")]
    [InlineData("   ", "agg-1")]
    [InlineData("Order", null)]
    [InlineData("Order", "")]
    [InlineData("Order", "   ")]
    public void DomainEventBase_InvalidAggregateInfo_ThrowsArgumentException(string? aggType, string? aggId)
    {
        Assert.Throws<ArgumentException>(() =>
            new TestCustomDomainEvent(aggType!, aggId!));
    }

    private sealed class TestCustomDomainEvent : DomainEventBase
    {
        public TestCustomDomainEvent(string aggType, string aggId) : base(aggType, aggId) { }
        public TestCustomDomainEvent(string aggType, string aggId, DateTimeOffset occurredOn, Guid eventId)
            : base(aggType, aggId, occurredOn, eventId) { }
    }

    [Fact]
    public void DomainEventBase_HistoricalConstructor_PreservesExactTimestampAndEventId()
    {
        var fixedEventId = Guid.NewGuid();
        var historicalDate = new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var evt = new TestCustomDomainEvent("CustomAgg", "id-42", historicalDate, fixedEventId);

        Assert.Equal(fixedEventId, evt.EventId);
        Assert.Equal(historicalDate, evt.OccurredOn);
        Assert.Equal("CustomAgg", evt.AggregateType);
        Assert.Equal("id-42", evt.AggregateId);
    }

    [Fact]
    public void DomainEventGenericGuid_SetsAggregateTypeAndVersion()
    {
        var id = Guid.NewGuid();
        var evt = new GenericGuidOrderEvent(id, 7);

        Assert.Equal(nameof(OrderAggregate), evt.AggregateType);
        Assert.Equal(id.ToString(), evt.AggregateId);
        Assert.Equal(7, evt.Version);
    }

    #endregion

    #region Built-In Audit Events Tests

    [Fact]
    public void AggregateCreatedEvent_ValidData_AssignsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = new AggregateCreatedEvent("Order", "order-1", "admin-user", "tenant-alpha", now);

        Assert.Equal("Order", evt.AggregateType);
        Assert.Equal("order-1", evt.AggregateId);
        Assert.Equal("admin-user", evt.CreatorId);
        Assert.Equal("tenant-alpha", evt.TenantId);
        Assert.Equal(now, evt.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AggregateCreatedEvent_InvalidCreatorId_ThrowsArgumentException(string? invalidCreator)
    {
        Assert.Throws<ArgumentException>(() =>
            new AggregateCreatedEvent("Order", "order-1", invalidCreator!, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AggregateModifiedEvent_ValidData_AssignsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = new AggregateModifiedEvent("Order", "order-1", "modifier-user", "tenant-alpha", now);

        Assert.Equal("Order", evt.AggregateType);
        Assert.Equal("order-1", evt.AggregateId);
        Assert.Equal("modifier-user", evt.ModifierId);
        Assert.Equal("tenant-alpha", evt.TenantId);
        Assert.Equal(now, evt.ModifiedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AggregateModifiedEvent_InvalidModifierId_ThrowsArgumentException(string? invalidModifier)
    {
        Assert.Throws<ArgumentException>(() =>
            new AggregateModifiedEvent("Order", "order-1", invalidModifier!, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AggregateSoftDeletedEvent_ValidData_AssignsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = new AggregateSoftDeletedEvent("Order", "order-1", "deleter-user", null, now);

        Assert.Equal("Order", evt.AggregateType);
        Assert.Equal("order-1", evt.AggregateId);
        Assert.Equal("deleter-user", evt.DeleterId);
        Assert.Null(evt.TenantId);
        Assert.Equal(now, evt.DeletedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AggregateSoftDeletedEvent_InvalidDeleterId_ThrowsArgumentException(string? invalidDeleter)
    {
        Assert.Throws<ArgumentException>(() =>
            new AggregateSoftDeletedEvent("Order", "order-1", invalidDeleter!, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AggregateRestoredEvent_ValidData_AssignsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = new AggregateRestoredEvent("Order", "order-1", "restorer-user", "tenant-beta", now);

        Assert.Equal("Order", evt.AggregateType);
        Assert.Equal("order-1", evt.AggregateId);
        Assert.Equal("restorer-user", evt.RestorerId);
        Assert.Equal("tenant-beta", evt.TenantId);
        Assert.Equal(now, evt.RestoredAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AggregateRestoredEvent_InvalidRestorerId_ThrowsArgumentException(string? invalidRestorer)
    {
        Assert.Throws<ArgumentException>(() =>
            new AggregateRestoredEvent("Order", "order-1", invalidRestorer!, null, DateTimeOffset.UtcNow));
    }

    #endregion

    #region IntegrationEvent Tests

    [Fact]
    public void IntegrationEvent_DefaultConstructor_GeneratesIdsAndTimestamp()
    {
        var evt = new CustomIntegrationEvent("ORD-999");

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.NotEqual(Guid.Empty, evt.CorrelationId);
        Assert.Null(evt.CausationId);
        Assert.True(evt.OccurredOn <= DateTimeOffset.UtcNow);
        Assert.Contains(nameof(CustomIntegrationEvent), evt.EventType);
        Assert.Equal("ORD-999", evt.OrderNumber);
    }

    [Fact]
    public void IntegrationEvent_WithCorrelationAndCausation_PreservesValues()
    {
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var evt = new CustomIntegrationEvent("ORD-100", correlationId, causationId);

        Assert.Equal(correlationId, evt.CorrelationId);
        Assert.Equal(causationId, evt.CausationId);
    }

    #endregion

    #region Outbox & Inbox Message Pattern Tests

    [Fact]
    public void OutboxMessage_MarkAsProcessed_SetsProcessedAt()
    {
        var msg = new TestOutboxMessage();
        Assert.Null(msg.ProcessedAt);

        msg.MarkAsProcessed();

        Assert.NotNull(msg.ProcessedAt);
    }

    [Fact]
    public void OutboxMessage_LogFailure_IncrementsRetryCountAndCapturesError()
    {
        var msg = new TestOutboxMessage();
        Assert.Equal(0, msg.RetryCount);
        Assert.Null(msg.LastError);

        msg.LogFailure("Network connection timeout.");
        Assert.Equal(1, msg.RetryCount);
        Assert.Equal("Network connection timeout.", msg.LastError);

        msg.LogFailure("Broker unreachable.");
        Assert.Equal(2, msg.RetryCount);
        Assert.Equal("Broker unreachable.", msg.LastError);
    }

    [Fact]
    public void InboxMessage_ContractCompliance_MaintainsIdempotentFields()
    {
        var eventId = Guid.NewGuid();
        var msg = new TestInboxMessage
        {
            EventId = eventId,
            ConsumerName = "BillingConsumer",
            TenantId = "tenant-billing"
        };

        Assert.Equal(eventId, msg.EventId);
        Assert.Equal("BillingConsumer", msg.ConsumerName);
        Assert.Equal("tenant-billing", msg.TenantId);
        Assert.Equal("OrderPlaced", msg.EventType);
    }

    #endregion

    #region Saga Process Manager Tests

    [Theory]
    [InlineData(SagaStatus.Pending, 0)]
    [InlineData(SagaStatus.Running, 1)]
    [InlineData(SagaStatus.Completed, 2)]
    [InlineData(SagaStatus.Failed, 3)]
    [InlineData(SagaStatus.Compensating, 4)]
    [InlineData(SagaStatus.TimedOut, 5)]
    public void SagaStatus_EnumValues_MatchArchitecturalSpec(SagaStatus status, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)status);
    }

    [Fact]
    public void Saga_Lifecycle_TransitionsSuccessfullyToCompleted()
    {
        var saga = new OrderSaga();
        Assert.Equal(SagaStatus.Pending, saga.Status);
        Assert.Equal(0, saga.Version);

        saga.Start();
        Assert.Equal(SagaStatus.Running, saga.Status);
        Assert.Equal(1, saga.Version);

        saga.Data.OrderId = Guid.NewGuid();
        saga.Data.InventoryReserved = true;
        saga.Data.PaymentProcessed = true;

        saga.Complete();
        Assert.Equal(SagaStatus.Completed, saga.Status);
        Assert.NotNull(saga.CompletedAt);
        Assert.Equal(2, saga.Version);
    }

    [Fact]
    public void Saga_Lifecycle_TransitionsToFailed()
    {
        var saga = new OrderSaga();
        saga.Start();

        saga.Fail("Payment gateway declined transaction.");

        Assert.Equal(SagaStatus.Failed, saga.Status);
        Assert.Equal("Payment gateway declined transaction.", saga.FailureReason);
        Assert.NotNull(saga.CompletedAt);
    }

    [Fact]
    public void Saga_Compensate_TransitionsToCompensating()
    {
        var saga = new OrderSaga();
        saga.Start();

        saga.Compensate();

        Assert.Equal(SagaStatus.Compensating, saga.Status);
    }

    #endregion

    #region Snapshot & Projection Tests

    [Fact]
    public void Snapshot_ContractCompliance_CapturesStateDelta()
    {
        var snapshot = new TestSnapshot();

        Assert.NotNull(snapshot.AggregateId);
        Assert.Equal(nameof(OrderAggregate), snapshot.AggregateType);
        Assert.Equal(42, snapshot.Version);
        Assert.Contains("Shipped", snapshot.State);
    }

    [Fact]
    public async Task Projection_ApplyAndReset_OperatesAsExpected()
    {
        var projection = new TestProjection();
        Assert.Equal(0, projection.Position);
        Assert.Null(projection.LastProcessedAt);

        var evt1 = new OrderPlacedDomainEvent(Guid.NewGuid(), 100m, 1);
        var evt2 = new OrderPlacedDomainEvent(Guid.NewGuid(), 200m, 2);

        await projection.ApplyAsync(evt1);
        Assert.Equal(1, projection.Position);
        Assert.NotNull(projection.LastProcessedAt);

        await projection.ApplyAsync(evt2);
        Assert.Equal(2, projection.Position);
        Assert.Equal(2, projection.ReceivedEvents.Count);

        await projection.ResetAsync();
        Assert.Equal(0, projection.Position);
        Assert.Null(projection.LastProcessedAt);
        Assert.Empty(projection.ReceivedEvents);
    }

    #endregion
}
