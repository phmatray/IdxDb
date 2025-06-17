using Microsoft.JSInterop;

namespace IdxDb.DemoApp.Data;

/// <summary>
/// Application database context following Entity Framework patterns
/// </summary>
public class AppDbContext : IndexedDbContext
{
    public AppDbContext(IJSRuntime jsRuntime) 
        : base(jsRuntime, "ProductDatabase")
    {
    }

    /// <summary>
    /// Gets the set of products in the database
    /// </summary>
    public IIndexedDbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKeyPath(p => p.Id)
                  .AutoIncrement();
            
            // Add any indexes if needed
            // entity.HasIndex(p => p.Name);
            // entity.HasIndex(p => p.Price);
        });
    }
}