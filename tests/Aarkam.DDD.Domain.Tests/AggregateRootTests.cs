using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class AggregateRootTests
{
    private sealed class OrderCreatedEvent : DomainEvent<TestOrder, Guid>
    {
        public string CustomerId { get; }

        public OrderCreatedEvent(Guid aggregateId, int version, string customerId)
            : base(aggregateId.ToString(), version)
        {
            CustomerId = customerId;
        }
    }

    private sealed class OrderStatusChangedEvent : DomainEvent<TestOrder, Guid>
    {
        public string NewStatus { get; }

        public OrderStatusChangedEvent(Guid aggregateId, int version, string newStatus)
            : base(aggregateId.ToString(), version)
        {
            NewStatus = newStatus;
        }
    }

    private sealed class TestOrder : AggregateRoot<Guid>
    {
        public string CustomerId { get; private set; } = string.Empty;
        public string Status { get; private set; } = "Draft";

        public TestOrder() : base() { }

        public TestOrder(Guid id, string customerId) : base(id)
        {
            RaiseEvent(new OrderCreatedEvent(id, Version + 1, customerId));
        }

        public void ChangeStatus(string newStatus)
        {
            RaiseEvent(new OrderStatusChangedEvent(Id, Version + 1, newStatus));
        }

        public void TryRaiseInvalidAggregateIdEvent()
        {
            RaiseEvent(new OrderCreatedEvent(Guid.NewGuid(), Version + 1, "invalid"));
        }

        public void TryRaiseInvalidVersionEvent()
        {
            RaiseEvent(new OrderStatusChangedEvent(Id, Version + 99, "invalid"));
        }

        public void ExecuteBrokenRule(IBusinessRule rule)
        {
            CheckRule(rule);
        }

        protected override void ApplyEvent(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case OrderCreatedEvent created:
                    CustomerId = created.CustomerId;
                    Status = "Created";
                    break;
                case OrderStatusChangedEvent changed:
                    Status = changed.NewStatus;
                    break;
            }
        }
    }

    private sealed class AlwaysBrokenRule : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "Order rule violation.";
        public string ErrorCode => "ERR_ORDER_RULE";
        public ExceptionSeverity Severity => ExceptionSeverity.Error;
    }

    private sealed class AlwaysValidRule : IBusinessRule
    {
        public bool IsBroken() => false;
        public string Message => string.Empty;
    }

    [Fact]
    public void Constructor_Default_InitializesInitialVersionAndCollections()
    {
        var order = new TestOrder();

        Assert.Equal(-1, order.Version);
        Assert.NotNull(order.ExtraProperties);
        Assert.Empty(order.ExtraProperties);
        Assert.NotNull(order.GetChanges());
        Assert.Empty(order.GetChanges());
        Assert.False(order.IsDeleted);
        Assert.Null(order.TenantId);
    }

    [Fact]
    public void Constructor_WithIdAndRaiseEvent_AppliesEventAndTracksChanges()
    {
        var id = Guid.NewGuid();
        var order = new TestOrder(id, "cust-1");

        Assert.Equal(0, order.Version);
        Assert.Equal("cust-1", order.CustomerId);
        Assert.Equal("Created", order.Status);
        Assert.Single(order.GetChanges());

        var change = order.GetChanges().First() as OrderCreatedEvent;
        Assert.NotNull(change);
        Assert.Equal(id.ToString(), change.AggregateId);
        Assert.Equal(0, change.Version);
    }

    [Fact]
    public void MultipleEvents_IncrementVersionSequentially()
    {
        var id = Guid.NewGuid();
        var order = new TestOrder(id, "cust-1"); // Version = 0

        order.ChangeStatus("Processing"); // Version = 1
        order.ChangeStatus("Shipped");    // Version = 2

        Assert.Equal(2, order.Version);
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(3, order.GetChanges().Count);
    }

    [Fact]
    public void MarkEventsAsCommitted_ClearsUncommittedChangesWithoutChangingVersion()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        order.ChangeStatus("Processing");

        Assert.Equal(2, order.GetChanges().Count);
        Assert.Equal(1, order.Version);

        order.MarkEventsAsCommitted();

        Assert.Empty(order.GetChanges());
        Assert.Equal(1, order.Version);
        Assert.Equal("Processing", order.Status);
    }

    [Fact]
    public void LoadFromHistory_ReplaysEventsWithoutAddingToUncommittedChanges()
    {
        var orderId = Guid.NewGuid();
        var history = new List<IDomainEvent>
        {
            new OrderCreatedEvent(orderId, 0, "customer-99"),
            new OrderStatusChangedEvent(orderId, 1, "Confirmed"),
            new OrderStatusChangedEvent(orderId, 2, "Shipped")
        };

        var rehydrated = new TestOrder();
        rehydrated.LoadFromHistory(history);

        Assert.Equal(2, rehydrated.Version);
        Assert.Equal("Shipped", rehydrated.Status);
        Assert.Equal("customer-99", rehydrated.CustomerId);
        Assert.Empty(rehydrated.GetChanges()); // Key ES invariant: playback produces no uncommitted changes
    }

    [Fact]
    public void LoadFromHistory_NullHistory_ThrowsArgumentNullException()
    {
        var order = new TestOrder();
        Assert.Throws<ArgumentNullException>(() => order.LoadFromHistory(null!));
    }

    [Fact]
    public void RaiseEvent_NullEvent_ThrowsArgumentNullException()
    {
        var order = new TestOrder();
        Assert.Throws<ArgumentNullException>(() => order.Apply(null!));
    }

    [Fact]
    public void RaiseEvent_MismatchedAggregateId_ThrowsInvalidOperationException()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        Assert.Throws<InvalidOperationException>(() => order.TryRaiseInvalidAggregateIdEvent());
    }

    [Fact]
    public void RaiseEvent_MismatchedVersionSequence_ThrowsInvalidOperationException()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        Assert.Throws<InvalidOperationException>(() => order.TryRaiseInvalidVersionEvent());
    }

    [Fact]
    public void CheckRule_BrokenRule_ThrowsBusinessRuleValidationException()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        var ex = Assert.Throws<BusinessRuleValidationException>(() => order.ExecuteBrokenRule(new AlwaysBrokenRule()));

        Assert.Equal("ERR_ORDER_RULE", ex.ErrorCode);
    }

    [Fact]
    public void CheckRule_ValidRule_DoesNotThrow()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        order.ExecuteBrokenRule(new AlwaysValidRule());
    }

    [Fact]
    public void SetCreation_UpdatesCreationMetadata()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        order.SetCreation("user-admin");

        Assert.Equal("user-admin", order.CreatorId);
        Assert.True(order.CreationTime > DateTimeOffset.MinValue);
    }

    [Fact]
    public void SetCreation_NullOrEmptyUser_ThrowsArgumentException()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        Assert.Throws<ArgumentException>(() => order.SetCreation(string.Empty));
        Assert.Throws<ArgumentException>(() => order.SetCreation("   "));
    }

    [Fact]
    public void SetModification_UpdatesModificationMetadata()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        order.SetModification("user-editor");

        Assert.Equal("user-editor", order.ModifierId);
        Assert.NotNull(order.LastModificationTime);
    }

    [Fact]
    public void SetSoftDeletion_And_Restore_LifecycleTests()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");

        Assert.False(order.IsDeleted);
        Assert.Null(order.DeleterId);
        Assert.Null(order.DeletionTime);

        order.SetSoftDeletion("user-remover");

        Assert.True(order.IsDeleted);
        Assert.Equal("user-remover", order.DeleterId);
        Assert.NotNull(order.DeletionTime);

        order.Restore();

        Assert.False(order.IsDeleted);
        Assert.Null(order.DeleterId);
        Assert.Null(order.DeletionTime);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ReturnsSameInstanceWithoutMutations()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        var result = order.Restore();

        Assert.Same(order, result);
        Assert.False(order.IsDeleted);
    }

    [Fact]
    public void GetAggregateType_ReturnsCompiledClassName()
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        Assert.Equal(nameof(TestOrder), order.GetAggregateType());
    }

    [Theory]
    [InlineData("AdminUser")]
    [InlineData("SystemWorker")]
    [InlineData("AuditBot_99")]
    public void SetCreation_Parameterized_AcceptsValidStrings(string creator)
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        order.SetCreation(creator);
        Assert.Equal(creator, order.CreatorId);
    }

    [Theory]
    [InlineData("Status_1")]
    [InlineData("Status_2")]
    [InlineData("Status_3")]
    [InlineData("Status_4")]
    public void VersionProgression_Parameterized_TracksMultipleEvents(string status)
    {
        var order = new TestOrder(Guid.NewGuid(), "cust-1");
        var initialVersion = order.Version;

        order.ChangeStatus(status);

        Assert.Equal(initialVersion + 1, order.Version);
        Assert.Equal(status, order.Status);
    }
}
