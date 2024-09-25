namespace IdxDb;

[AttributeUsage(AttributeTargets.Property)]
public class IndexedDbKeyPathAttribute : Attribute
{
    public bool AutoIncrement { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Property)]
public class IndexedDbIndexAttribute : Attribute
{
    public string Name { get; set; }
    public bool Unique { get; set; } = false;

    public IndexedDbIndexAttribute(string name)
    {
        Name = name;
    }
}