using System.Linq.Expressions;

namespace IdxDb;

/// <summary>
/// Provides a simple API for configuring the IndexedDB model.
/// Similar to Entity Framework's ModelBuilder.
/// </summary>
public class ModelBuilder
{
    private readonly Dictionary<Type, EntityConfiguration> _entityConfigurations = new();

    /// <summary>
    /// Configures an entity type in the model.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure.</typeparam>
    /// <param name="configurationAction">An action to configure the entity.</param>
    /// <returns>The model builder for method chaining.</returns>
    public ModelBuilder Entity<TEntity>(Action<EntityBuilder<TEntity>> configurationAction) where TEntity : class
    {
        var entityBuilder = new EntityBuilder<TEntity>();
        configurationAction(entityBuilder);
        _entityConfigurations[typeof(TEntity)] = entityBuilder.Configuration;
        return this;
    }

    /// <summary>
    /// Gets the entity configurations.
    /// </summary>
    internal IReadOnlyDictionary<Type, EntityConfiguration> GetConfigurations() => _entityConfigurations;
}

/// <summary>
/// Provides configuration for an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public class EntityBuilder<TEntity> where TEntity : class
{
    internal EntityConfiguration Configuration { get; } = new();

    /// <summary>
    /// Configures the key path for the entity.
    /// </summary>
    /// <typeparam name="TProperty">The type of the key property.</typeparam>
    /// <param name="keyExpression">An expression identifying the key property.</param>
    /// <returns>The entity builder for method chaining.</returns>
    public EntityBuilder<TEntity> HasKeyPath<TProperty>(Expression<Func<TEntity, TProperty>> keyExpression)
    {
        var memberExpression = keyExpression.Body as MemberExpression
            ?? throw new ArgumentException("Key expression must be a property access expression.");
        
        Configuration.KeyPath = memberExpression.Member.Name.ToLowerInvariant();
        return this;
    }

    /// <summary>
    /// Configures the key to auto-increment.
    /// </summary>
    /// <returns>The entity builder for method chaining.</returns>
    public EntityBuilder<TEntity> AutoIncrement()
    {
        Configuration.AutoIncrement = true;
        return this;
    }

    /// <summary>
    /// Adds an index to the entity.
    /// </summary>
    /// <typeparam name="TProperty">The type of the indexed property.</typeparam>
    /// <param name="indexExpression">An expression identifying the indexed property.</param>
    /// <param name="unique">Whether the index should be unique.</param>
    /// <returns>The entity builder for method chaining.</returns>
    public EntityBuilder<TEntity> HasIndex<TProperty>(Expression<Func<TEntity, TProperty>> indexExpression, bool unique = false)
    {
        var memberExpression = indexExpression.Body as MemberExpression
            ?? throw new ArgumentException("Index expression must be a property access expression.");
        
        var propertyName = memberExpression.Member.Name;
        Configuration.Indexes.Add(new IndexConfiguration
        {
            Name = propertyName.ToLowerInvariant(),
            KeyPath = propertyName.ToLowerInvariant(),
            Unique = unique
        });
        
        return this;
    }
}

/// <summary>
/// Configuration for an entity.
/// </summary>
internal class EntityConfiguration
{
    public string? KeyPath { get; set; }
    public bool AutoIncrement { get; set; }
    public List<IndexConfiguration> Indexes { get; } = new();
}

/// <summary>
/// Configuration for an index.
/// </summary>
internal class IndexConfiguration
{
    public required string Name { get; set; }
    public required string KeyPath { get; set; }
    public bool Unique { get; set; }
}