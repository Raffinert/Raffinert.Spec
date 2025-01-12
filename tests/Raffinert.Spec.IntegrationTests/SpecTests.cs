using AgileObjects.ReadableExpressions;
using Microsoft.EntityFrameworkCore;
using Raffinert.Spec.IntegrationTests.Infrastructure;
using System.Linq.Expressions;
using Raffinert.Spec.IntegrationTests.Generated;
using Raffinert.Spec.IntegrationTests.Model;

namespace Raffinert.Spec.IntegrationTests;

public class SpecTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    private readonly IReadOnlyTestDbContext _context = fixture.Context;

    [Fact]
    public async Task FilterProducts_ByName_Queryable_WithOperators_ShouldReturnCorrectProducts()
    {
        // Arrange
        var appleSpec = new ProductNameSpec("Apple");
        var bananaSpec = Spec<ReadOnlyProduct>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec || appleSpec;
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var productsQuery = _context.Products.Where(notBananaAndNotAppleSpec);
        var filteredProducts = await productsQuery.ToArrayAsync();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                CategoryId = 1,
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);

        Assert.Equal("p => !((p.Name == \"Banana\") || (p.Name == <name>P))",
            notBananaAndNotAppleSpec.GetExpression().ToReadableString());
    }

    [Fact]
    public void FilterProducts_ByName_Enumerable_WithOperators_ShouldReturnCorrectProducts()
    {
        // Arrange
        var appleSpec = new ProductNameSpec("Apple");
        var bananaSpec = Spec<ReadOnlyProduct>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec || appleSpec;
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var filteredProducts = _context.Products.ToArray().Where(notBananaAndNotAppleSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                CategoryId = 1,
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);

        Assert.Equal("p => !((p.Name == \"Banana\") || (p.Name == <name>P))",
            notBananaAndNotAppleSpec.GetExpression().ToReadableString());
    }

    [Fact]
    public async Task FilterProducts_ByName_Queryable_WithMethods_ShouldReturnCorrectProducts()
    {
        // Arrange
        var bananaSpec = Spec<ReadOnlyProduct>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec.Or(p => p.Name == "Apple");
        var notBananaAndNotAppleSpec = bananaOrAppleSpec.Not();

        // Act
        var productsQuery = _context.Products.Where(notBananaAndNotAppleSpec);
        var filteredProducts = await productsQuery.ToArrayAsync();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);

        Assert.Equal("p => !((p.Name == \"Banana\") || (p.Name == \"Apple\"))",
            notBananaAndNotAppleSpec.GetExpression().ToReadableString());
    }

    [Fact]
    public void FilterProducts_ByName_Enumerable_WithMethods_ShouldReturnCorrectProducts()
    {
        // Arrange
        var bananaSpec = Spec<ReadOnlyProduct>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec.Or(p => p.Name == "Apple");
        var notBananaAndNotAppleSpec = bananaOrAppleSpec.Not();

        // Act
        var filteredProducts = _context.Products.ToArray().Where(notBananaAndNotAppleSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);

        Assert.Equal("p => !((p.Name == \"Banana\") || (p.Name == \"Apple\"))",
            notBananaAndNotAppleSpec.GetExpression().ToReadableString());
    }

    [Fact]
    public async Task NestedSpecifications()
    {
        // Arrange
        var bananaStringSpec = Spec<string>.Create(n => n == "Banana");
        var categoryWithBanana = Spec<ReadOnlyCategory>.Create(c => c.Products.Any(p => bananaStringSpec.IsSatisfiedBy(p.Name)));

        var bananaSpec1 = Spec<ReadOnlyProduct>.Create(p => p.Name == "Banana");
        var categoryWithBananaProductMethodGroup = Spec<ReadOnlyCategory>.Create(c => c.Products.Any(bananaSpec1.IsSatisfiedBy));

        var appleSpec = new ProductNameSpec("Apple");
        var categoryWithAppleProduct = Spec<ReadOnlyCategory>.Create(c => c.Products.Any(p => appleSpec.IsSatisfiedBy(p)));

        var productName = "Apple";
        var categoryWithDynamicProductMethodGroup = Spec<ReadOnlyCategory>.Create(c => c.Products.Any(new ProductNameSpec(productName).IsSatisfiedBy));

        var productName1 = "Banana";
        var categoryWithDynamicProduct = Spec<ReadOnlyCategory>.Create(c => c.Products.Any(p => new ProductNameSpec(productName1).IsSatisfiedBy(p)));

        var categoryNestedSpec = new CategorySpec(productName1);

        // Act1
        var catQuery1 = _context.Categories.Where(categoryWithBanana);
        var filteredCategories1 = await catQuery1.ToListAsync();

        // Act2
        var catQuery2 = _context.Categories.Where(categoryWithBananaProductMethodGroup);
        var filteredCategories2 = await catQuery2.ToListAsync();

        // Act3
        var catQuery3 = _context.Categories.Where(categoryWithAppleProduct);
        var filteredCategories3 = await catQuery3.ToListAsync();

        // Act4
        var catQuery4 = _context.Categories.Where(categoryWithDynamicProductMethodGroup);
        var filteredCategories4 = await catQuery4.ToListAsync();

        // Act5
        var catQuery5 = _context.Categories.Where(categoryWithDynamicProduct);
        var filteredCategories5 = await catQuery5.ToListAsync();

        // Act6
        var catQuery6 = _context.Categories.Where(categoryNestedSpec);
        var filteredCategories6 = await catQuery6.ToListAsync();

        // Assert
        Assert.Equal("c => c.Products.Any(p => p.Name == \"Banana\")",
            categoryWithBanana.GetExpandedExpression().ToReadableString());

        Assert.Equal("c => c.Products.Any(p => p.Name == \"Banana\")",
            categoryWithBananaProductMethodGroup.GetExpandedExpression().ToReadableString());

        Assert.Equal("c => c.Products.Any(p => p.Name == <name>P)",
            categoryWithAppleProduct.GetExpandedExpression().ToReadableString());

        Assert.Equal("c => c.Products.Any(p => p.Name == <name>P)",
            categoryWithDynamicProductMethodGroup.GetExpandedExpression().ToReadableString());

        Assert.Equal("c => c.Products.Any(p => p.Name == <name>P)",
            categoryWithDynamicProduct.GetExpandedExpression().ToReadableString());

        Assert.Equal("c => c.Products.Any(p => p.Name == <name>P)",
            categoryNestedSpec.GetExpandedExpression().ToReadableString());

        var expectedCategories = new[]
        {
            new
            {
                Id = 1,
                Name = "Fruit"
            }
        };

        Assert.Equivalent(expectedCategories, filteredCategories1);
        Assert.Equivalent(expectedCategories, filteredCategories2);
        Assert.Equivalent(expectedCategories, filteredCategories3);
        Assert.Equivalent(expectedCategories, filteredCategories4);
        Assert.Equivalent(expectedCategories, filteredCategories5);
        Assert.Equivalent(expectedCategories, filteredCategories6);
    }

    private class ProductNameSpec(string name) : Spec<ReadOnlyProduct>
    {
        public override Expression<Func<ReadOnlyProduct, bool>> GetExpression()
        {
            return p => p.Name == name;
        }
    }

    private class CategorySpec(string productName) : Spec<ReadOnlyCategory>
    {
        private readonly Spec<ReadOnlyProduct> _productSpec = new ProductNameSpec(productName);

        public override Expression<Func<ReadOnlyCategory, bool>> GetExpression()
        {
            return c => c.Products.Any(_productSpec.IsSatisfiedBy);
        }
    }
}