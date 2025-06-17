using System.Linq.Expressions;
using Microsoft.JSInterop;

namespace IdxDb;

/// <summary>
/// Extension methods for IndexedDB LINQ operations.
/// </summary>
public static class IndexedDbExtensions
{
    /// <summary>
    /// Converts an IQueryable to an array asynchronously.
    /// </summary>
    public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.ToArrayAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use ToArrayAsync");
    }

    /// <summary>
    /// Converts an IQueryable to a list asynchronously.
    /// </summary>
    public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.ToListAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use ToListAsync");
    }

    /// <summary>
    /// Returns the first element asynchronously.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.FirstAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use FirstAsync");
    }

    /// <summary>
    /// Returns the first element that satisfies a condition asynchronously.
    /// </summary>
    public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.FirstAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use FirstAsync");
    }

    /// <summary>
    /// Returns the first element or default asynchronously.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.FirstOrDefaultAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use FirstOrDefaultAsync");
    }

    /// <summary>
    /// Returns the first element that satisfies a condition or default asynchronously.
    /// </summary>
    public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.FirstOrDefaultAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use FirstOrDefaultAsync");
    }

    /// <summary>
    /// Returns the single element asynchronously.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.SingleAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use SingleAsync");
    }

    /// <summary>
    /// Returns the single element that satisfies a condition asynchronously.
    /// </summary>
    public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.SingleAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use SingleAsync");
    }

    /// <summary>
    /// Returns the single element or default asynchronously.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.SingleOrDefaultAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use SingleOrDefaultAsync");
    }

    /// <summary>
    /// Returns the single element that satisfies a condition or default asynchronously.
    /// </summary>
    public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.SingleOrDefaultAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use SingleOrDefaultAsync");
    }

    /// <summary>
    /// Counts the elements asynchronously.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.CountAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use CountAsync");
    }

    /// <summary>
    /// Counts the elements that satisfy a condition asynchronously.
    /// </summary>
    public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.CountAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use CountAsync");
    }

    /// <summary>
    /// Determines whether any elements exist asynchronously.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.AnyAsync();
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use AnyAsync");
    }

    /// <summary>
    /// Determines whether any elements satisfy a condition asynchronously.
    /// </summary>
    public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
    {
        if (source is IIndexedDbSet<TSource> dbSet)
        {
            return dbSet.AnyAsync(predicate);
        }
        
        throw new InvalidOperationException("Source must be an IIndexedDbSet to use AnyAsync");
    }

    /// <summary>
    /// Creates an IndexedDB context with a fluent configuration API.
    /// </summary>
    public static IndexedDbContextBuilder CreateContext(this IJSRuntime jsRuntime, string databaseName)
    {
        return new IndexedDbContextBuilder(jsRuntime, databaseName);
    }
}

/// <summary>
/// Builder for configuring an IndexedDB context.
/// </summary>
public class IndexedDbContextBuilder
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _databaseName;
    private readonly List<Type> _entityTypes = new();

    internal IndexedDbContextBuilder(IJSRuntime jsRuntime, string databaseName)
    {
        _jsRuntime = jsRuntime;
        _databaseName = databaseName;
    }

    /// <summary>
    /// Adds an entity type to the context.
    /// </summary>
    public IndexedDbContextBuilder WithEntity<TEntity>() where TEntity : class
    {
        _entityTypes.Add(typeof(TEntity));
        return this;
    }

    /// <summary>
    /// Builds and initializes the context.
    /// </summary>
    public async Task<IndexedDbContext> BuildAsync()
    {
        var context = new IndexedDbContext(_jsRuntime, _databaseName);
        
        if (_entityTypes.Any())
        {
            await context.InitializeAsync(_entityTypes.ToArray());
        }
        else
        {
            await context.InitializeAsync();
        }
        
        return context;
    }
}