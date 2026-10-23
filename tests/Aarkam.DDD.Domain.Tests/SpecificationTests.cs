using System.Linq.Expressions;
using Xunit;

namespace Aarkam.DDD.Domain.Tests;

public class SpecificationTests
{
    private sealed class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsInStock { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    private sealed class PremiumProductSpecification : SpecificationBase<Product>
    {
        private readonly decimal _minimumPrice;

        public PremiumProductSpecification(decimal minimumPrice = 100m)
        {
            _minimumPrice = minimumPrice;
        }

        public override Expression<Func<Product, bool>> ToExpression()
        {
            return p => p.Price >= _minimumPrice;
        }
    }

    private sealed class InStockSpecification : SpecificationBase<Product>
    {
        public override Expression<Func<Product, bool>> ToExpression()
        {
            return p => p.IsInStock;
        }
    }

    private sealed class CategorySpecification : SpecificationBase<Product>
    {
        private readonly string _category;

        public CategorySpecification(string category)
        {
            _category = category;
        }

        public override Expression<Func<Product, bool>> ToExpression()
        {
            return p => p.Category == _category;
        }
    }

    private sealed class PagedAndSortedProductSpecification : SpecificationBase<Product>
    {
        public PagedAndSortedProductSpecification(int skip, int take)
        {
            Skip = skip;
            Take = take;
            OrderBy = p => p.Name;
            OrderByDescending = p => p.Price;
            AddInclude(p => p.Tags);
        }

        public override Expression<Func<Product, bool>> ToExpression()
        {
            return p => p.Price > 0;
        }
    }

    #region Base Specification & Criteria Tests

    [Theory]
    [InlineData(150, true)]
    [InlineData(100, true)]
    [InlineData(99.99, false)]
    [InlineData(0, false)]
    public void IsSatisfiedBy_SingleSpecification_EvaluatesCorrectly(decimal price, bool expectedResult)
    {
        var spec = new PremiumProductSpecification(100m);
        var product = new Product { Price = price };

        var result = spec.IsSatisfiedBy(product);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Includes_WhenSpecified_ContainsConfiguredExpressions()
    {
        var spec = new PagedAndSortedProductSpecification(0, 10);

        Assert.NotNull(spec.Includes);
        Assert.Single(spec.Includes);
    }

    [Fact]
    public void OrderBy_WhenSpecified_ReturnsConfiguredAscendingExpression()
    {
        var spec = new PagedAndSortedProductSpecification(0, 10);

        Assert.NotNull(spec.OrderBy);
    }

    [Fact]
    public void OrderByDescending_WhenSpecified_ReturnsConfiguredDescendingExpression()
    {
        var spec = new PagedAndSortedProductSpecification(0, 10);

        Assert.NotNull(spec.OrderByDescending);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(20, 50)]
    [InlineData(100, 25)]
    public void Pagination_SkipAndTake_AreCorrectlyAssigned(int skip, int take)
    {
        var spec = new PagedAndSortedProductSpecification(skip, take);

        Assert.Equal(skip, spec.Skip);
        Assert.Equal(take, spec.Take);
    }

    [Fact]
    public void Pagination_DefaultSpecification_HasNullPagingAndOrdering()
    {
        var spec = new PremiumProductSpecification();

        Assert.Null(spec.Skip);
        Assert.Null(spec.Take);
        Assert.Null(spec.OrderBy);
        Assert.Null(spec.OrderByDescending);
        Assert.Empty(spec.Includes);
    }

    #endregion

    #region And Specification Tests

    [Theory]
    [InlineData(150, true, true)]
    [InlineData(150, false, false)]
    [InlineData(50, true, false)]
    [InlineData(50, false, false)]
    public void And_CombinesTwoSpecificationsWithLogicalAnd(decimal price, bool inStock, bool expected)
    {
        var premiumSpec = new PremiumProductSpecification(100m);
        var inStockSpec = new InStockSpecification();

        var combinedSpec = premiumSpec.And(inStockSpec);
        var product = new Product { Price = price, IsInStock = inStock };

        var result = combinedSpec.IsSatisfiedBy(product);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void And_WithNullRightSpecification_ThrowsArgumentNullException()
    {
        var left = new InStockSpecification();

        Assert.Throws<ArgumentNullException>(() => left.And(null!));
    }

    [Fact]
    public void And_WorksWithLinqQueryableWhereClause()
    {
        var products = new List<Product>
        {
            new() { Name = "A", Price = 200m, IsInStock = true },
            new() { Name = "B", Price = 50m, IsInStock = true },
            new() { Name = "C", Price = 300m, IsInStock = false },
            new() { Name = "D", Price = 20m, IsInStock = false }
        };

        var spec = new PremiumProductSpecification(100m).And(new InStockSpecification());
        var matched = products.AsQueryable().Where(spec.ToExpression()).ToList();

        Assert.Single(matched);
        Assert.Equal("A", matched[0].Name);
    }

    #endregion

    #region Or Specification Tests

    [Theory]
    [InlineData(150, true, true)]
    [InlineData(150, false, true)]
    [InlineData(50, true, true)]
    [InlineData(50, false, false)]
    public void Or_CombinesTwoSpecificationsWithLogicalOr(decimal price, bool inStock, bool expected)
    {
        var premiumSpec = new PremiumProductSpecification(100m);
        var inStockSpec = new InStockSpecification();

        var combinedSpec = premiumSpec.Or(inStockSpec);
        var product = new Product { Price = price, IsInStock = inStock };

        var result = combinedSpec.IsSatisfiedBy(product);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Or_WithNullRightSpecification_ThrowsArgumentNullException()
    {
        var left = new InStockSpecification();

        Assert.Throws<ArgumentNullException>(() => left.Or(null!));
    }

    [Fact]
    public void Or_WorksWithLinqQueryableWhereClause()
    {
        var products = new List<Product>
        {
            new() { Name = "A", Price = 200m, IsInStock = false },
            new() { Name = "B", Price = 50m, IsInStock = true },
            new() { Name = "C", Price = 10m, IsInStock = false }
        };

        var spec = new PremiumProductSpecification(100m).Or(new InStockSpecification());
        var matched = products.AsQueryable().Where(spec.ToExpression()).ToList();

        Assert.Equal(2, matched.Count);
        Assert.Contains(matched, p => p.Name == "A");
        Assert.Contains(matched, p => p.Name == "B");
    }

    #endregion

    #region Not Specification Tests

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Not_NegatesInternalSpecification(bool inStock, bool expected)
    {
        var inStockSpec = new InStockSpecification();
        var outOfStockSpec = inStockSpec.Not();

        var product = new Product { IsInStock = inStock };
        var result = outOfStockSpec.IsSatisfiedBy(product);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Not_WorksWithLinqQueryableWhereClause()
    {
        var products = new List<Product>
        {
            new() { Name = "In1", IsInStock = true },
            new() { Name = "Out1", IsInStock = false },
            new() { Name = "In2", IsInStock = true },
            new() { Name = "Out2", IsInStock = false }
        };

        var spec = new InStockSpecification().Not();
        var matched = products.AsQueryable().Where(spec.ToExpression()).ToList();

        Assert.Equal(2, matched.Count);
        Assert.All(matched, p => Assert.False(p.IsInStock));
    }

    #endregion

    #region Complex Expression Chaining & Parameter Harmony

    [Theory]
    [InlineData("Electronics", 200, true, true)]   // Cat matches, Premium & InStock
    [InlineData("Electronics", 40, true, false)]   // Cat matches, but Price < 100, and not >= 50
    [InlineData("Books", 200, true, true)]         // Cat does not match, but InStock and Price >= 50
    [InlineData("Books", 200, false, false)]       // Cat does not match, out of stock
    public void ComplexChain_And_Or_Not_EvaluatesConsistently(string category, decimal price, bool inStock, bool expected)
    {
        // (Category == "Electronics" AND Price >= 100) OR (IsInStock AND NOT (Price < 50))
        var categorySpec = new CategorySpecification("Electronics");
        var premiumSpec = new PremiumProductSpecification(100m);
        var inStockSpec = new InStockSpecification();

        var complexSpec = (categorySpec.And(premiumSpec)).Or(inStockSpec.And(new PremiumProductSpecification(50m)));

        var product = new Product
        {
            Category = category,
            Price = price,
            IsInStock = inStock
        };

        var result = complexSpec.IsSatisfiedBy(product);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComplexChain_CanBeCompiledAndExecutedAcrossCollection()
    {
        var products = new List<Product>
        {
            new() { Name = "Laptop", Category = "Computers", Price = 1200m, IsInStock = true },
            new() { Name = "Monitor", Category = "Computers", Price = 300m, IsInStock = false },
            new() { Name = "Mouse", Category = "Accessories", Price = 25m, IsInStock = true },
            new() { Name = "Keyboard", Category = "Accessories", Price = 80m, IsInStock = true }
        };

        var spec = new CategorySpecification("Computers").Or(new CategorySpecification("Accessories").And(new InStockSpecification()));
        var results = products.AsQueryable().Where(spec.ToExpression()).ToList();

        Assert.Equal(4, results.Count);
    }

    #endregion
}
