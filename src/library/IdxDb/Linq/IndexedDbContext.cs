using Microsoft.JSInterop;
using System.Reflection;

namespace IdxDb;

/// <summary>
/// Represents a session with the IndexedDB database and provides access to object stores.
/// Similar to Entity Framework's DbContext.
/// </summary>
public class IndexedDbContext : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _databaseName;
    private readonly Dictionary<Type, object> _dbSets = new();
    private IndexedDbInterop? _interop;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbContext"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for interop.</param>
    /// <param name="databaseName">The name of the IndexedDB database.</param>
    public IndexedDbContext(IJSRuntime jsRuntime, string databaseName)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    /// <summary>
    /// Gets a queryable set for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity.</typeparam>
    /// <returns>An IIndexedDbSet for the entity type.</returns>
    public IIndexedDbSet<TEntity> Set<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        
        if (!_dbSets.TryGetValue(type, out var dbSet))
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Context must be initialized before accessing sets. Call InitializeAsync first.");
            }

            var storeName = GetStoreName<TEntity>();
            dbSet = new IndexedDbSet<TEntity>(this, _interop!, _databaseName, storeName);
            _dbSets[type] = dbSet;
        }

        return (IIndexedDbSet<TEntity>)dbSet;
    }

    /// <summary>
    /// Initializes the database context and opens the IndexedDB database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _interop = new IndexedDbInterop(_jsRuntime);
        
        // Discover all entity types and create store definitions
        var storeDefinitions = DiscoverStoreDefinitions();
        
        // Open the database with all stores
        await _interop.OpenIndexedDbAsync(_databaseName, 1, storeDefinitions);
        
        _initialized = true;
    }

    /// <summary>
    /// Initializes the database context with specific entity types.
    /// </summary>
    /// <param name="entityTypes">The entity types to register.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(params Type[] entityTypes)
    {
        if (_initialized)
            return;

        _interop = new IndexedDbInterop(_jsRuntime);
        
        // Build the model
        var modelBuilder = new ModelBuilder();
        OnModelCreating(modelBuilder);
        var configurations = modelBuilder.GetConfigurations();
        
        // Create store definitions for specified types
        var storeDefinitions = entityTypes
            .Select(type =>
            {
                // Check if there's a custom configuration
                if (configurations.TryGetValue(type, out var config))
                {
                    return CreateStoreDefinitionFromConfig(type, config);
                }
                
                // Otherwise use the default generation
                return type.GenerateStoreDefinitionFromType();
            })
            .ToArray();
        
        // Open the database with specified stores
        await _interop.OpenIndexedDbAsync(_databaseName, 1, storeDefinitions);
        
        _initialized = true;
    }


    /// <summary>
    /// Deletes the entire database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteDatabaseAsync()
    {
        if (_interop != null)
        {
            await _interop.DeleteDatabaseAsync(_databaseName);
        }
        
        // Reset the context state
        _dbSets.Clear();
        _initialized = false;
    }

    /// <summary>
    /// Disposes the context and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var dbSet in _dbSets.Values)
        {
            if (dbSet is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        if (_interop != null)
        {
            await _interop.DisposeAsync();
        }

        _dbSets.Clear();
        _initialized = false;
    }

    /// <summary>
    /// Gets the IndexedDB interop instance.
    /// </summary>
    internal IndexedDbInterop GetInterop()
    {
        if (!_initialized || _interop == null)
        {
            throw new InvalidOperationException("Context must be initialized before accessing interop.");
        }

        return _interop;
    }

    /// <summary>
    /// Gets the store name for an entity type.
    /// </summary>
    private static string GetStoreName<TEntity>()
    {
        var type = typeof(TEntity);
        
        // Use the store definition to get the consistent store name
        var storeDefinition = type.GenerateStoreDefinitionFromType();
        return storeDefinition.Name;
    }

    /// <summary>
    /// Discovers store definitions from the assembly.
    /// </summary>
    private StoreDefinition[] DiscoverStoreDefinitions()
    {
        // In a real implementation, you might want to scan assemblies
        // or use configuration to determine which types to include
        // For now, we'll return an empty array and require explicit initialization
        return Array.Empty<StoreDefinition>();
    }
    
    /// <summary>
    /// Called when the model is being created.
    /// Override this method to configure the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected virtual void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Override in derived classes to configure the model
    }
    
    /// <summary>
    /// Creates a store definition from a configuration.
    /// </summary>
    private StoreDefinition CreateStoreDefinitionFromConfig(Type entityType, EntityConfiguration config)
    {
        var storeName = GetStoreNameForType(entityType);
        
        var storeDefinition = new StoreDefinition
        {
            Name = storeName,
            Options = new StoreOptions
            {
                KeyPath = config.KeyPath ?? "id",
                AutoIncrement = config.AutoIncrement
            },
            Indexes = config.Indexes.Select(idx => new IndexDefinition
            {
                Name = idx.Name,
                KeyPath = idx.KeyPath,
                Unique = idx.Unique
            }).ToArray()
        };
        
        return storeDefinition;
    }
    
    /// <summary>
    /// Gets the store name for a type using pluralization rules.
    /// </summary>
    private static string GetStoreNameForType(Type type)
    {
        var name = type.Name;
        if (!name.EndsWith("s"))
        {
            if (name.EndsWith("y"))
                name = name[..^1] + "ies";
            else
                name += "s";
        }
        return name.ToLowerInvariant();
    }
}