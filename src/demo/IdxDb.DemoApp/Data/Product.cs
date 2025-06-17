namespace IdxDb.DemoApp.Data;

/// <summary>
/// Product entity for the demo application
/// </summary>
public class Product
{
    [IndexedDbKeyPath(AutoIncrement = true)]
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public string Description { get; set; } = "";
    
    public decimal Price { get; set; }
    
    public int Stock { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
}