# LINQKit vs Spec in Entity Framework Core

## Overview

This project demonstrates a comparison between **LINQKit** and **Spec** for building dynamic queries in **Entity Framework Core**. It highlights how `Spec<T>` can generate **pure expressions** without relying on **EF Core-specific tweaks** (like `AsExpandable()` in LINQKit).

This example is adapted from the original example found in the [LINQKit GitHub repository](https://github.com/scottksmith95/LINQKit/tree/master/examples/ConsoleAppNetCore3Ef3).

## Prerequisites

Ensure you have the following installed:

- .NET 9 or later
- Microsoft SQL Server (LocalDB or full instance)
- Required NuGet packages:
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `LinqKit.Microsoft.EntityFrameworkCore`
  - `Raffinert.Spec`

## Setup

1. Clone or download this repository.
2. Update the connection string in the `DbContextOptionsBuilder` to match your SQL Server setup.
3. Run the project.

## Code Structure

The program executes a series of queries using **LINQKit** and **Spec**, displaying:

- The generated SQL query
- The results from the database

### **Database Context Setup**

The database context (`MyHotelDbContext`) is initialized and ensured to be created:

```csharp
var options = new DbContextOptionsBuilder<MyHotelDbContext>()
    .UseSqlServer("Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MyHotel;Trusted_Connection=True;MultipleActiveResultSets=true")
    .Options;

using var context = new MyHotelDbContext(options);
context.Database.EnsureCreated();
```

### **Comparison of LINQKit and Spec**

We execute multiple queries to compare their behavior:

#### **1. LINQKit: OR Condition with PredicateBuilder**

```csharp
static IQueryable<Guest> OrConditionWithLinqKitPredicate(MyHotelDbContext context)
{
    var predicate = PredicateBuilder.New<Guest>(x => x.Id > 0).Or(x => x.Name.Contains("e"));
    return context.Guests.AsExpandable().Where(predicate);
}
```

- Requires `AsExpandable()` for EF Core compatibility.
- Generates **valid SQL but relies on EF-specific behavior**.

#### **2. Spec: OR Condition**

```csharp
static IQueryable<Guest> OrConditionWithWithSpec(MyHotelDbContext context)
{
    var spec = Spec<Guest>.Create(x => x.Id > 0).Or(x => x.Name.Contains("e"));
    return context.Guests.Where(spec);
}
```

- Works **without** `AsExpandable()`.
- Uses a **pure expression tree**.
- **More portable across LINQ providers.**

#### **3. LINQKit: Nested Conditions with Expressions**

```csharp
static IQueryable<Guest> NestedConditionsWithLinqKitExpressions(MyHotelDbContext context)
{
    Expression<Func<Guest, bool>> criteria1 = guest => guest.Name.Contains("af");
    Expression<Func<Guest, bool>> criteria2 = guest => criteria1.Invoke(guest) || guest.Id > 1;
    
    Console.WriteLine($"LINQKit expanded expression: {criteria2.Expand()}");
    return context.Guests.AsExpandable().Where(criteria2);
}
```

- Uses `.Expand()` to modify expressions at runtime.
- Requires `AsExpandable()`.

#### **4. Spec: Nested Conditions**

```csharp
static IQueryable<Guest> NestedConditionsWithSpec(MyHotelDbContext context)
{
    var criteria1 = Spec<Guest>.Create(guest => guest.Name.Contains("af"));
    var criteria2 = Spec<Guest>.Create(guest => criteria1.IsSatisfiedBy(guest) || guest.Id > 1);
    
    Console.WriteLine($"Spec<T> expanded expression: {criteria2.GetExpandedExpression()}");
    return context.Guests.Where(criteria2);
}
```

- Uses `GetExpandedExpression()` to evaluate nested expressions **without EF-specific modifications**.
- Maintains **full LINQ compatibility**.

#### **5. LINQKit: Query Syntax**

> **Uses `AsExpandable()` to allow EF Core compatibility.**

```csharp
static IQueryable<RoomDetail> QuerySyntaxWithLinqKit(Expression<Func<Room, bool>> roomCriteria, MyHotelDbContext context)
{
    var query = from room in context.Rooms.AsExpandable().Where(roomCriteria.And(r => r.RoomDetailId != 0))
                select room.RoomDetail;

    return query;
}
```

#### **6. Spec: Query Syntax**

> **Uses a pure expression-based approach, avoiding `AsExpandable()` and making it more portable.**

```csharp
static IQueryable<RoomDetail> QuerySyntaxWithSpec(Spec<Room> roomSpec, MyHotelDbContext context)
{
    var query = from room in context.Rooms.Where(roomSpec.And(r => r.RoomDetailId != 0))
                select room.RoomDetail;

    return query;
}
```

## **Conclusion**

This project demonstrates how **Spec** provides a cleaner, more portable alternative to **LINQKit**, allowing developers to build reusable query specifications without relying on **EF Core-specific tweaks**.

## **License**

MIT License
