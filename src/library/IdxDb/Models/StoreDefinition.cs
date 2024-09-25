namespace IdxDb;

public class StoreDefinition
{
    public string Name { get; set; }
    public StoreOptions Options { get; set; }
    public IndexDefinition[] Indexes { get; set; }
}

public class StoreOptions
{
    public string KeyPath { get; set; }
    public bool AutoIncrement { get; set; }
}

public class IndexDefinition
{
    public string Name { get; set; }
    public string KeyPath { get; set; }
    public bool Unique { get; set; }
}
