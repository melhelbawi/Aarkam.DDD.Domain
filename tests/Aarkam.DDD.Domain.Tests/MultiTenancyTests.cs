using Aarkam.DDD.Domain.Entities;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class MultiTenancyTests
{
    private sealed class MultiTenantOrder : Entity<Guid>, IHasTenant
    {
        public string? TenantId { get; private set; }
        public string OrderNumber { get; set; } = string.Empty;

        public MultiTenantOrder(Guid id, string? tenantId) : base(id)
        {
            TenantId = tenantId;
        }
    }

    #region IHasTenant Tests

    [Fact]
    public void HasTenant_WhenTenantIdIsSet_ReturnsAssignedTenant()
    {
        var order = new MultiTenantOrder(Guid.NewGuid(), "tenant-corp-1");

        Assert.Equal("tenant-corp-1", order.TenantId);
    }

    [Fact]
    public void HasTenant_WhenTenantIdIsNull_RepresentsHostEntity()
    {
        var order = new MultiTenantOrder(Guid.NewGuid(), null);

        Assert.Null(order.TenantId);
    }

    #endregion

    #region CurrentTenant Tests

    [Fact]
    public void CurrentTenant_InitialState_IsHostWithNoTenant()
    {
        var currentTenant = new CurrentTenant();

        Assert.Null(currentTenant.TenantId);
        Assert.False(currentTenant.IsAvailable);
    }

    [Theory]
    [InlineData("tenant-100", true)]
    [InlineData("enterprise_abc", true)]
    [InlineData("t1", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void CurrentTenant_IsAvailable_EvaluatesCorrectly(string? tenantId, bool expectedIsAvailable)
    {
        var currentTenant = new CurrentTenant();

        using (currentTenant.Change(tenantId))
        {
            Assert.Equal(expectedIsAvailable, currentTenant.IsAvailable);
        }
    }

    [Fact]
    public void Change_SwitchesTenantWithinUsingBlockAndRevertsOnDispose()
    {
        var currentTenant = new CurrentTenant();

        Assert.Null(currentTenant.TenantId);

        using (currentTenant.Change("tenant-xyz"))
        {
            Assert.Equal("tenant-xyz", currentTenant.TenantId);
            Assert.True(currentTenant.IsAvailable);
        }

        Assert.Null(currentTenant.TenantId);
        Assert.False(currentTenant.IsAvailable);
    }

    [Fact]
    public void Change_NestedScopes_RestoresHierarchically()
    {
        var currentTenant = new CurrentTenant();

        Assert.Null(currentTenant.TenantId);

        using (currentTenant.Change("tenant-1"))
        {
            Assert.Equal("tenant-1", currentTenant.TenantId);

            using (currentTenant.Change("tenant-2"))
            {
                Assert.Equal("tenant-2", currentTenant.TenantId);

                using (currentTenant.Change("tenant-3"))
                {
                    Assert.Equal("tenant-3", currentTenant.TenantId);
                }

                Assert.Equal("tenant-2", currentTenant.TenantId);
            }

            Assert.Equal("tenant-1", currentTenant.TenantId);
        }

        Assert.Null(currentTenant.TenantId);
    }

    [Fact]
    public void Change_FromTenantToHost_AndRevert()
    {
        var currentTenant = new CurrentTenant();

        using (currentTenant.Change("tenant-primary"))
        {
            Assert.Equal("tenant-primary", currentTenant.TenantId);

            using (currentTenant.Change(null))
            {
                Assert.Null(currentTenant.TenantId);
                Assert.False(currentTenant.IsAvailable);
            }

            Assert.Equal("tenant-primary", currentTenant.TenantId);
        }
    }

    [Fact]
    public void Change_DisposedTwice_IsSafe()
    {
        var currentTenant = new CurrentTenant();

        var token = currentTenant.Change("tenant-temp");
        Assert.Equal("tenant-temp", currentTenant.TenantId);

        token.Dispose();
        Assert.Null(currentTenant.TenantId);

        // Calling Dispose again should not throw
        var exception = Record.Exception(() => token.Dispose());
        Assert.Null(exception);
        Assert.Null(currentTenant.TenantId);
    }

    #endregion

    #region Async Concurrency & Ambient Context Isolation

    [Fact]
    public async Task CurrentTenant_AcrossAsyncAwaitBoundaries_PreservesContext()
    {
        var currentTenant = new CurrentTenant();

        using (currentTenant.Change("tenant-async-flow"))
        {
            await Task.Delay(20);
            Assert.Equal("tenant-async-flow", currentTenant.TenantId);

            await Task.Yield();
            Assert.Equal("tenant-async-flow", currentTenant.TenantId);
        }

        Assert.Null(currentTenant.TenantId);
    }

    [Fact]
    public async Task CurrentTenant_MultipleConcurrentTasks_MaintainIsolatedState()
    {
        var currentTenant = new CurrentTenant();

        var task1 = Task.Run(async () =>
        {
            using (currentTenant.Change("tenant-alpha"))
            {
                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal("tenant-alpha", currentTenant.TenantId);
                    await Task.Delay(10);
                }
            }
        });

        var task2 = Task.Run(async () =>
        {
            using (currentTenant.Change("tenant-beta"))
            {
                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal("tenant-beta", currentTenant.TenantId);
                    await Task.Delay(10);
                }
            }
        });

        await Task.WhenAll(task1, task2);
    }

    #endregion

    #region AmbientTenantContext Static Tests

    [Fact]
    public void AmbientTenantContext_InitialState_IsNull()
    {
        AmbientTenantContext.Clear();
        Assert.Null(AmbientTenantContext.CurrentTenantId);
    }

    [Theory]
    [InlineData("ambient-t1")]
    [InlineData("ambient-org-99")]
    [InlineData("ambient-saas-user")]
    public void AmbientTenantContext_SetCurrent_UpdatesGlobalContext(string expectedTenant)
    {
        AmbientTenantContext.SetCurrent(expectedTenant);
        Assert.Equal(expectedTenant, AmbientTenantContext.CurrentTenantId);

        AmbientTenantContext.Clear();
        Assert.Null(AmbientTenantContext.CurrentTenantId);
    }

    [Fact]
    public async Task AmbientTenantContext_AcrossAsyncTasks_MaintainsThreadLocalIsolation()
    {
        var taskA = Task.Run(async () =>
        {
            AmbientTenantContext.SetCurrent("task-A-tenant");
            await Task.Delay(15);
            Assert.Equal("task-A-tenant", AmbientTenantContext.CurrentTenantId);
            AmbientTenantContext.Clear();
        });

        var taskB = Task.Run(async () =>
        {
            AmbientTenantContext.SetCurrent("task-B-tenant");
            await Task.Delay(15);
            Assert.Equal("task-B-tenant", AmbientTenantContext.CurrentTenantId);
            AmbientTenantContext.Clear();
        });

        await Task.WhenAll(taskA, taskB);
    }

    #endregion
}
