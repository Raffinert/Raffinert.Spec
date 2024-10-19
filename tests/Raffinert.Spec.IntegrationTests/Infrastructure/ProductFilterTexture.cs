using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Raffinert.Spec.IntegrationTests.Infrastructure;

public class ProductFilterFixture : IDisposable
{
    public TestDbContext Context { get; }
    private readonly SqliteConnection _connection;

    public ProductFilterFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new TestDbContext(options);

        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Close();
        GC.SuppressFinalize(this);
    }
}
