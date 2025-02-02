using AgileObjects.ReadableExpressions;
using Raffinert.Spec.IntegrationTests.Infrastructure;
using Raffinert.Spec.IntegrationTests.Model;

namespace Raffinert.Spec.IntegrationTests;

public class SpecTemplatesTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    private readonly TestDbContext _context = fixture.Context;

    [Fact]
    public void SingleLineTemplate()
    {
        // Arrange
        var specTemplate = SpecTemplate<Product>.Create(p => new { p.Name }, arg => arg.Name == "Banana");

        var categorySpec = specTemplate.Adapt<Category>();
        var productSpec = specTemplate.Adapt<Product>();

        // Act
        var filteredProducts = _context.Products.Where(productSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Id = 2,
                Name = "Banana",
                Price = 15.0m
            }
        }, filteredProducts);

        Assert.Equal("arg => arg.Name == \"Banana\"",
            categorySpec.GetExpandedExpression().ToReadableString());

        Assert.Equal("arg => arg.Name == \"Banana\"",
            productSpec.GetExpandedExpression().ToReadableString());
    }

    [Fact]
    public void MultiLineTemplate()
    {
        // Arrange
        var bananaStringSpec = Spec<string>.Create(n => n == "Banana");

        var specTemplate = SpecTemplate<Product>.Create(
            p => new { p.Name, p.Id },
            arg => bananaStringSpec.IsSatisfiedBy(arg.Name) && arg.Id > 0);

        var categorySpec = specTemplate.Adapt<Category>("cat");
        var productSpec = specTemplate.Adapt<Product>("prod");

        // Act
        var filteredProducts = _context.Products.Where(productSpec).ToArray();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Id = 2,
                Name = "Banana",
                Price = 15.0m
            }
        }, filteredProducts);

        Assert.Equal("cat => (cat.Name == \"Banana\") && (cat.Id > 0)",
            categorySpec.GetExpandedExpression().ToReadableString());

        Assert.Equal("prod => (prod.Name == \"Banana\") && (prod.Id > 0)",
            productSpec.GetExpandedExpression().ToReadableString());
    }
}