using Microsoft.EntityFrameworkCore;
using Raffinert.Spec.IntegrationTests.Infrastructure;
using Raffinert.Spec.IntegrationTests.Model;
using System.Linq.Expressions;

namespace Raffinert.Spec.IntegrationTests;

public class SpecTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    private readonly TestDbContext _context = fixture.Context;

    [Fact]
    public async Task FilterProducts_ByName_Queryable_WithOperators_ShouldReturnCorrectProducts()
    {
        // Arrange
        var appleSpec = new ProductNameSpec("Apple");
        var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec || appleSpec;
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var productsQuery = _context.Products.Where(notBananaAndNotAppleSpec);
        var filteredProducts = await productsQuery.ToArrayAsync();

        // Assert
        Assert.Equivalent(new[]
        {
            new Product
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);
    }

    [Fact]
    public void FilterProducts_ByName_Enumerable_WithOperators_ShouldReturnCorrectProducts()
    {
        // Arrange
        var appleSpec = new ProductNameSpec("Apple");
        var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec || appleSpec;
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var filteredProducts = _context.ProductArray.Where(notBananaAndNotAppleSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new Product
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);
    }

    [Fact]
    public async Task FilterProducts_ByName_Queryable_WithMethods_ShouldReturnCorrectProducts()
    {
        // Arrange
        var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec.Or(p => p.Name == "Apple");
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var productsQuery = _context.Products.Where(notBananaAndNotAppleSpec);
        var filteredProducts = await productsQuery.ToArrayAsync();

        // Assert
        Assert.Equivalent(new[]
        {
            new Product
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);
    }

    [Fact]
    public void FilterProducts_ByName_Enumerable_WithMethods_ShouldReturnCorrectProducts()
    {
        // Arrange
        var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
        var bananaOrAppleSpec = bananaSpec.Or(p => p.Name == "Apple");
        var notBananaAndNotAppleSpec = !bananaOrAppleSpec;

        // Act
        var filteredProducts = _context.ProductArray.Where(notBananaAndNotAppleSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new Product
            {
                Id = 3,
                Name = "Cherry",
                Price = 8.0m
            }
        }, filteredProducts);
    }

    [Fact]
    public async Task ComplexSpecificationComposition()
    {
        // Arrange
        var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
        var categoryWithBananaProductMethodGroup = Spec<Category>.Create(c => c.Products.Any(bananaSpec.IsSatisfiedBy));

        var appleSpec = new ProductNameSpec("Apple");
        var categoryWithAppleProduct = Spec<Category>.Create(c => c.Products.Any(p => appleSpec.IsSatisfiedBy(p)));
        
        // Act1
        var catQueryMethodGroup = _context.Categories.Where(categoryWithBananaProductMethodGroup);
        var filteredCategoriesMethodGroup = await catQueryMethodGroup.ToArrayAsync();

        // Act2
        var catQuery = _context.Categories.Where(categoryWithAppleProduct);
        var filteredCategories = await catQuery.ToArrayAsync();

        // Assert
        Category[] expectedCategories =
        [
            new Category
            {
                Id = 1,
                Name = "Fruit"
            }
        ];

        Assert.Equivalent(expectedCategories, filteredCategoriesMethodGroup);
        Assert.Equivalent(expectedCategories, filteredCategories);
    }

    private class ProductNameSpec(string name) : Spec<Product>
    {
        public override Expression<Func<Product, bool>> GetExpression()
        {
            return p => p.Name == name;
        }
    }
}