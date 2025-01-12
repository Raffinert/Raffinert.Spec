using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Raffinert.Spec.IntegrationTests.Generated;

namespace Raffinert.Spec.IntegrationTests.Infrastructure;

public class ProductFilterFixture : IDisposable
{
    public IReadOnlyTestDbContext Context { get; }
    private readonly SqliteConnection _connection;

    public ProductFilterFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ReadOnlyTestDbContext>()
            // see https://stackoverflow.com/questions/55983982/asp-net-core-2-2-returns-notsupportedexception-collection-was-of-a-fixed-size
            // Option1
            //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            .UseSqlite(_connection)
            .Options;

        var context = new ReadOnlyTestDbContext(options);
        context.Database.EnsureCreated();
        Context = context;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Close();
        GC.SuppressFinalize(this);
    }
}
