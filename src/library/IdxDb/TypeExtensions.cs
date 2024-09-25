using System.Reflection;

namespace IdxDb;

public static class TypeExtensions
{
    public static StoreDefinition GenerateStoreDefinitionFromType(this Type type)
    {
        var storeDefinition = new StoreDefinition
        {
            Name = type.Name
        };

        var properties = type.GetProperties();
        var indexes = new List<IndexDefinition>();

        // Find the key path property
        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttribute<IndexedDbKeyPathAttribute>() != null);
        if (keyProperty == null)
        {
            throw new InvalidOperationException($"No key path defined in {type.Name}. Please annotate a property with [IndexedDbKeyPath].");
        }

        var keyAttribute = keyProperty.GetCustomAttribute<IndexedDbKeyPathAttribute>();
        storeDefinition.Options = new StoreOptions
        {
            KeyPath = keyProperty.Name,
            AutoIncrement = keyAttribute?.AutoIncrement ?? true
        };

        // Find index properties
        foreach (var prop in properties)
        {
            var indexAttribute = prop.GetCustomAttribute<IndexedDbIndexAttribute>();
            if (indexAttribute != null)
            {
                indexes.Add(new IndexDefinition
                {
                    Name = indexAttribute.Name,
                    KeyPath = prop.Name,
                    Unique = indexAttribute.Unique
                });
            }
        }

        storeDefinition.Indexes = indexes.ToArray();

        return storeDefinition;
    }
}