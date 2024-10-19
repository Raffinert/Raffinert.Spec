[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://stand-with-ukraine.pp.ua)

# Raffinert.Spec

`Raffinert.Spec` is a lightweight, composable specification library for building reusable query logic for Entity Framework.

## Features
- **Composable Specifications**: Easily combine specifications using `AND`, `OR`, and `NOT` operators.
- **Dynamic Expression Creation**: Build complex, reusable query logic with minimal boilerplate.
- **Debugger Support**: Enhanced debugging with `DebuggerDisplay` and `DebuggerTypeProxy`.
- **Predicate Testing**: Use `IsSatisfiedBy` to test if a specification is satisfied by a candidate.
- **Operator Overloading**: Use natural operators (`&&`, `||`, `!`) to compose specifications.

## Installation

![Nuget](https://img.shields.io/nuget/v/Raffinert.Spec)

To use `Raffinert.Spec`, add it as a dependency to your project. 

## Why Another Specification Library?

`Raffinert.Spec` was created after the analysis of existing libraries and source code like:
* [NSpecifications](https://github.com/miholler/NSpecifications), 
* [SpecificationPattern](https://github.com/vkhorikov/SpecificationPattern), 
* [Ardalis.Specification](https://github.com/ardalis/Specification), and 
* [LINQKit](https://github.com/scottksmith95/LINQKit).

`Raffinert.Spec` is closely aligned with [NSpecifications](https://github.com/miholler/NSpecifications) but has been streamlined for maximum simplicity and ease of use.

## Usage

### Defining a Specification

You can define specifications either inline, use predefined `True` and `False` specifications or create custom specification class.

```csharp
using Raffinert.Spec;

// Inline specification for checking if a number is positive
Spec<int> isPositive = Spec<int>.Create(x => x > 0);

// True specification
Spec<int> alwaysTrue = Spec<int>.True();

// False specification
Spec<int> alwaysFalse = Spec<int>.False();

// Custom specification class
class IsPositiveSpec : Spec<int>
{
	public override Expression<Func<int, bool>> ToExpression()
	{
		return x => x > 0;
	}
}
```

### Composing Specifications

You can combine specifications using logical operators (`AND`, `OR`, `NOT`) with method chaining or operator overloads.

#### AND Specification

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);
Spec<int> isEven = Spec<int>.Create(x => x % 2 == 0);

// Combine with AND (Both positive and even)
Spec<int> isPositiveAndEven = isPositive.And(isEven);

// Or using the && operator
Spec<int> isPositiveAndEvenOperator = isPositive && isEven;
```

#### OR Specification

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);
Spec<int> isEven = Spec<int>.Create(x => x % 2 == 0);

// Combine with OR (Either positive or even)
Spec<int> isPositiveOrEven = isPositive.Or(isEven);

// Or using the || operator
Spec<int> isPositiveOrEvenOperator = isPositive || isEven;
```

#### NOT Specification

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);

// Negate the positive specification
Spec<int> isNotPositive = isPositive.Not();

// Or using the ! operator
Spec<int> isNotPositiveOperator = !isPositive;
```

### Evaluating Specifications

You can evaluate if an object satisfies a specification using the `IsSatisfiedBy` method:

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);

// Check if the candidate satisfies the specification
bool result = isPositive.IsSatisfiedBy(5); // true
```

### Full Example

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);
Spec<int> isEven = Spec<int>.Create(x => x % 2 == 0);

// Complex specification: Positive and even, or not positive
Spec<int> complexSpec = (isPositive.And(isEven)).Or(!isPositive);

// Evaluate the specification
bool result = complexSpec.IsSatisfiedBy(4);  // true (4 is positive and even)
```

### Converting to Expressions or Delegates

You can convert a `Spec<T>` into an `Expression<Func<T, bool>>` or a compiled `Func<T, bool>` for direct execution:

```csharp
Spec<int> isPositive = Spec<int>.Create(x => x > 0);

// Convert to an expression
Expression<Func<int, bool>> expression = isPositive;

// Compile to a function
Func<int, bool> func = isPositive;
```

## Debugging

The `Spec<T>` class includes built-in debugging support with a custom debugger display, giving developers an immediate view of the underlying expression while debugging.

## License

This library is licensed under the MIT License.
