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

RunQueryAndPrintResult(OrConditionWithLinqKitPredicate(context));
RunQueryAndPrintResult(OrConditionWithSpec(context));
RunQueryAndPrintResult(NestedConditionsWithLinqKitExpressions(context));
RunQueryAndPrintResult(NestedConditionsWithSpec(context));
RunQueryAndPrintResult(QuerySyntaxWithSpec(Spec<Room>.Create(r => r.Number == 101), context));
RunQueryAndPrintResult(QuerySyntaxWithLinqKit(r => r.Number == 101, context));

return;

static IQueryable<Guest> OrConditionWithLinqKitPredicate(MyHotelDbContext context)
{
    var predicate = PredicateBuilder.New<Guest>(x => x.Id > 0).Or(x => x.Name.Contains("e"));
    return context.Guests.Where(predicate);
}

static IQueryable<Guest> OrConditionWithSpec(MyHotelDbContext context)
{
    var spec = Spec<Guest>.Create(x => x.Id > 0).Or(x => x.Name.Contains("e"));
    return context.Guests.Where(spec);
}

static IQueryable<Reservation> NestedConditionsWithLinqKitExpressions(MyHotelDbContext context)
{
    Expression<Func<Guest, bool>> criteria1 = guest => guest.Name.Contains("eo");
    Expression<Func<Reservation, bool>> criteria2 = reservation => criteria1.Invoke(reservation.Guest);

    Console.WriteLine($"LINQKit expanded expression: {criteria2.Expand()}");
    return context.Reservations.AsExpandable().Where(criteria2);
    // or
    // return context.Reservations.Where(criteria2.Expand());
}

static IQueryable<Reservation> NestedConditionsWithSpec(MyHotelDbContext context)
{
    var criteria1 = Spec<Guest>.Create(guest => guest.Name.Contains("eo"));
    var criteria2 = Spec<Reservation>.Create(reservation => criteria1.IsSatisfiedBy(reservation.Guest));

    Console.WriteLine($"Spec expanded expression: {criteria2.GetExpandedExpression()}");
    return context.Reservations.Where(criteria2);
    // or
    // return context.Reservations.Where(criteria2.GetExpandedExpression());
}

static IQueryable<RoomDetail> QuerySyntaxWithLinqKit(Expression<Func<Room, bool>> roomCriteria, MyHotelDbContext context)
{
    var query = from room in context.Rooms.Where(roomCriteria.And(r => r.RoomDetailId != 0))
                select room.RoomDetail;

    return query;
}

static IQueryable<RoomDetail> QuerySyntaxWithSpec(Spec<Room> roomSpec, MyHotelDbContext context)
{
    var query = from room in context.Rooms.Where(roomSpec.And(r => r.RoomDetailId != 0))
                select room.RoomDetail;

    return query;
}

static void RunQueryAndPrintResult<T>(IQueryable<T> queryable, [CallerArgumentExpression(nameof(queryable))] string methodName = "")
{
    Console.WriteLine($"### {methodName} ###\n");

    Console.WriteLine("SQL Query:");
    Console.WriteLine(queryable.ToQueryString());
    Console.WriteLine();

    Console.WriteLine("Results:");
    foreach (var item in queryable.ToArray())
    {
        Console.WriteLine(item);
    }
    Console.WriteLine("\n---------------------------------\n");
}
