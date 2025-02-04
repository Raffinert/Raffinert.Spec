[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://stand-with-ukraine.pp.ua)

# Raffinert.Spec
[![NuGet version (Raffinert.Spec)](https://img.shields.io/nuget/v/Raffinert.Spec.svg?style=flat-square)](https://www.nuget.org/packages/Raffinert.Spec/)


# Raffinert.Spec

`Raffinert.Spec` is a rethinking of libraries and sources such as:

- [NSpecifications](https://github.com/miholler/NSpecifications)
- [SpecificationPattern](https://github.com/vkhorikov/SpecificationPattern)
- [Ardalis.Specification](https://github.com/ardalis/Specification)
- [LINQKit](https://github.com/scottksmith95/LINQKit)
- [SpeciVacation](https://github.com/joakimjm/specivacation)
- [LinqSpecs](https://github.com/navozenko/LinqSpecs)

## Why Another Specification Library?

The main goal was to create a simple replacement of [LINQKit](https://github.com/scottksmith95/LINQKit) library without any Entity Framework specific tweaks.

With Raffinert.Spec you can:

- Combine expressions with logical operators OR, AND, NOT.
- Use nested specifications by calling `IsSatisfiedBy` method.
- Create specification templates and apply them to different entities with similar signatures.

## Usage

Full examples can be found in [Integration Tests](https://github.com/Raffinert/Raffinert.Spec/blob/main/tests/Raffinert.Spec.IntegrationTests/SpecTests.cs)

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

### Specification Templates

Specification templates allow you to define reusable structures that can be adapted to different entities with similar properties.

#### Example: Creating and Adapting a Specification Template

```csharp
var template = SpecTemplate<Product>.Create(p => new { p.Name, p.Price }, t => t.Name == "Banana" && t.Price > 10);
var adaptedSpec = template.Adapt<InventoryItem>();
```

In this example, a specification template is created for `Product`, filtering based on `Name` and `Price`. The template is then adapted to an `InventoryItem` type with matching properties.

### **Roslyn Analyzers for Compile-Time Validation**

To prevent runtime errors when using `SpecTemplate`, we provide Roslyn [Raffinert.Spec.Analyzer](https://github.com/Raffinert/Raffinert.Spec/tree/main/src/Raffinert.Spec.Analyzer) that:

- Ensure `SpecTemplate<TSample>.Adapt<TN>()` only adapts to types that contain all required members.
- Validate that `SpecTemplate.Create(...)` uses either an anonymous type projection (e.g., `p => new { p.Name }`) or class initialize statement (e.g., `p => new Template{ Name = p.Name }`).

These analyzers catch issues at compile-time, improving reliability and maintainability.

### Debugging

The `Spec<T>` class includes built-in debugging support with a custom debugger display, giving developers an immediate view of the underlying expression while debugging.

See also [Raffinert.Proj](https://github.com/Raffinert/Raffinert.Proj) library.
