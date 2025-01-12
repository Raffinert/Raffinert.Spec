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
