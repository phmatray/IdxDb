namespace IdxDb;

public class StoreDefinition
{
    public required string Name { get; set; }
    public required StoreOptions Options { get; set; }
    public required IndexDefinition[] Indexes { get; set; }
}

public class StoreOptions
{
    public required string KeyPath { get; set; }
    public bool AutoIncrement { get; set; }
}

public class IndexDefinition
{
    public required string Name { get; set; }
    public required string KeyPath { get; set; }
    public bool Unique { get; set; }
}