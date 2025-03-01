using ConsoleAppNetCore9Ef9.EntityFrameworkCore;
using ConsoleAppNetCore9Ef9.EntityFrameworkCore.Entities;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Raffinert.Spec;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

var options = new DbContextOptionsBuilder<MyHotelDbContext>()
    .UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=MyHotel;Trusted_Connection=True;MultipleActiveResultSets=true")
    .Options;

using var context = new MyHotelDbContext(options);
context.Database.EnsureCreated();

Console.WriteLine("Testing LINQKit vs Spec<T>:\n");

RunComparison(context, OrConditionWithLinqKitPredicate);
RunComparison(context, OrConditionWithWithSpec);
RunComparison(context, NestedConditionsWithLinqKitExpressions);
RunComparison(context, NestedConditionsWithSpec);
        

static IQueryable<Guest> OrConditionWithLinqKitPredicate(MyHotelDbContext context)
{
    var predicate = PredicateBuilder.New<Guest>(x => x.Id > 0).Or(x => x.Name.Contains("e"));

    // LINQKit requires AsExpandable() to work correctly with EF Core
    return context.Guests.AsExpandable().Where(predicate);
}

static IQueryable<Guest> OrConditionWithWithSpec(MyHotelDbContext context)
{
    var spec = Spec<Guest>.Create(x => x.Id > 0).Or(x => x.Name.Contains("e"));
    return context.Guests.Where(spec);
}

static IQueryable<Guest> NestedConditionsWithLinqKitExpressions(MyHotelDbContext context)
{
    Expression<Func<Guest, bool>> criteria1 = guest => guest.Name.Contains("af");
    Expression<Func<Guest, bool>> criteria2 = guest => criteria1.Invoke(guest) || guest.Id > 1;

    Console.WriteLine($"LINQKit expanded expression: {criteria2.Expand()}");
    return context.Guests.AsExpandable().Where(criteria2);
}

static IQueryable<Guest> NestedConditionsWithSpec(MyHotelDbContext context)
{
    var criteria1 = Spec<Guest>.Create(guest => guest.Name.Contains("af"));
    var criteria2 = Spec<Guest>.Create(guest => criteria1.IsSatisfiedBy(guest) || guest.Id > 1);

    Console.WriteLine($"Spec<T> expanded expression: {criteria2.GetExpandedExpression()}");
    return context.Guests.Where(criteria2);
}

static void RunComparison(MyHotelDbContext context, Func<MyHotelDbContext, IQueryable<Guest>> queryMethod, [CallerArgumentExpression(nameof(queryMethod))] string methodName = "")
{
    Console.WriteLine($"### {methodName} ###\n");

    var queryable = queryMethod(context);

    Console.WriteLine("SQL Query:");
    Console.WriteLine(queryable.ToQueryString());
    Console.WriteLine();

    Console.WriteLine("Results:");
    foreach (var guest in queryable.ToArray())
    {
        Console.WriteLine($" - {guest.Name}");
    }
    Console.WriteLine("\n---------------------------------\n");
}
    