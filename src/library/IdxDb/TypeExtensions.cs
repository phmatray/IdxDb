using System.Reflection;

namespace IdxDb;

public static class TypeExtensions
{
    public static StoreDefinition GenerateStoreDefinitionFromType(this Type type)
    {
        var storeDefinition = new StoreDefinition
        {
            Name = GetStoreNameForType(type),
            Options = null!, // Will be set below
            Indexes = null!  // Will be set below
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
    
    private static string GetStoreNameForType(Type type)
    {
        // Use a pluralized, lowercase version of the type name as the store name
        var name = type.Name;
        
        // Simple pluralization
        if (!name.EndsWith("s"))
        {
            if (name.EndsWith("y"))
            {
                name = name[..^1] + "ies";
            }
            else
            {
                name += "s";
            }
        }
        
        return name.ToLowerInvariant();
    }
}