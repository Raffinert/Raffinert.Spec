using Microsoft.EntityFrameworkCore;
using Raffinert.Spec.IntegrationTests.Model;

namespace Raffinert.Spec.IntegrationTests.Infrastructure;

public class TestDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    public Product[] ProductArray { get; } =
    [
        new Product { Id = 1, Name = "Apple", Price = 10.0m },
        new Product { Id = 2, Name = "Banana", Price = 15.0m },
        new Product { Id = 3, Name = "Cherry", Price = 8.0m }
    ];

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasData(ProductArray);
    }
}