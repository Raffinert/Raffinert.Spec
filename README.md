[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://stand-with-ukraine.pp.ua)

# Raffinert.Spec
[![NuGet version (Raffinert.Spec)](https://img.shields.io/nuget/v/Raffinert.Spec.svg?style=flat-square)](https://www.nuget.org/packages/Raffinert.Spec/)

`Raffinert.Spec` is a rethinking of libraries such as:
* [NSpecifications](https://github.com/miholler/NSpecifications). 
* [SpecificationPattern](https://github.com/vkhorikov/SpecificationPattern).
* [Ardalis.Specification](https://github.com/ardalis/Specification).
* [LINQKit](https://github.com/scottksmith95/LINQKit).
* [SpeciVacation](https://github.com/joakimjm/specivacation).

## Why Another Specification Library?

1. **Cleaner IDE**: `Raffinert.Spec` doesn’t add any extension methods to common classes like `object` or `Expression<Func<TEntity, bool>>`. This means you won’t see a lot of extra options in your IntelliSense.

2. **Simple Design**: All the 'magic' is incapsulated inside the Spec<T> and then can be explicitly or implicitly converted to Expression<Func<TEntity, bool>>. No Includes, Paginations and other extra features.

3. **Flexible Use**: It supports a mixed approach by allowing the use of separate specification classes as well as inline specifications. This makes it easy to combine expressions, including nested items, with no fragile code.


## Usage
Full examples see in [Integration Tests](https://github.com/Raffinert/Raffinert.Spec/blob/main/tests/Raffinert.Spec.IntegrationTests/SpecTests.cs)

### Defining a Specification

You can define specifications either inline or create custom specification classes. Below is an example of a custom specification for filtering products by name:

```csharp
using Raffinert.Spec;
using System.Linq.Expressions;

public class ProductNameSpec : Spec<Product>
{
    private readonly string _name;

    public ProductNameSpec(string name)
    {
        _name = name;
    }

    public override Expression<Func<Product, bool>> GetExpression()
    {
        return product => product.Name == _name;
    }
}
```

### Composing Specifications

You can combine specifications using logical operators (`AND`, `OR`, `NOT`) with method chaining or operator overloads. Here are some examples using a test context:

#### Example: Filtering Products

```csharp
// Arrange
var appleSpec = new ProductNameSpec("Apple");
var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
var bananaOrAppleSpec = bananaSpec || appleSpec; // OR specification
var notBananaAndNotAppleSpec = !bananaOrAppleSpec; // NOT specification

// Act
var productsQuery = _context.Products.Where(notBananaAndNotAppleSpec);
var filteredProducts = await productsQuery.ToArrayAsync();

// Assert
Assert.Equivalent(new[] 
{
    new Product { Id = 3, Name = "Cherry", Price = 8.0m }
}, filteredProducts);
```

### Filtering with Methods

You can also define specifications using methods:

```csharp
// Arrange
var bananaSpec = Spec<Product>.Create(p => p.Name == "Banana");
var bananaOrAppleSpec = bananaSpec.Or(p => p.Name == "Apple"); // OR specification
var notBananaAndNotAppleSpec = !bananaOrAppleSpec; // NOT specification

// Act
var filteredProducts = _context.ProductArray.Where(notBananaAndNotAppleSpec).ToArray();

// Assert
Assert.Equivalent(new[] 
{
    new Product { Id = 3, Name = "Cherry", Price = 8.0m }
}, filteredProducts);
```

### Complex Specification Composition

You can also compose specifications across multiple entities, as shown in the following example:

```csharp
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
Category[] expectedCategories = new[]
{
    new Category { Id = 1, Name = "Fruit" }
};

Assert.Equivalent(expectedCategories, filteredCategoriesMethodGroup);
Assert.Equivalent(expectedCategories, filteredCategories);
```

### Evaluating Specifications

You can evaluate if an object satisfies a specification using the `IsSatisfiedBy` method:

```csharp
// Example usage
var isSatisfied = bananaSpec.IsSatisfiedBy(new Product { Name = "Banana" }); // true
```

### Debugging

The `Spec<T>` class includes built-in debugging support with a custom debugger display, giving developers an immediate view of the underlying expression while debugging.