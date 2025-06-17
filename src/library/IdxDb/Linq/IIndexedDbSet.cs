using System.Linq.Expressions;

namespace IdxDb;

/// <summary>
/// Represents a queryable collection of entities from an IndexedDB object store.
/// Provides LINQ-like query capabilities for IndexedDB.
/// </summary>
/// <typeparam name="TEntity">The type of entity in the collection.</typeparam>
public interface IIndexedDbSet<TEntity> : IQueryable<TEntity>
{
    /// <summary>
    /// Adds a new entity to the object store.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// Adds a collection of entities to the object store.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates an existing entity in the object store.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(TEntity entity);

    /// <summary>
    /// Deletes an entity from the object store.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Deletes an entity from the object store by its key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key of the entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync<TKey>(TKey key);

    /// <summary>
    /// Finds an entity by its key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="key">The key of the entity to find.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> FindAsync<TKey>(TKey key);

    /// <summary>
    /// Executes the query and returns all results as an array.
    /// </summary>
    /// <returns>An array of entities.</returns>
    Task<TEntity[]> ToArrayAsync();

    /// <summary>
    /// Executes the query and returns all results as a list.
    /// </summary>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> ToListAsync();

    /// <summary>
    /// Returns the first element of the sequence.
    /// </summary>
    /// <returns>The first element.</returns>
    Task<TEntity> FirstAsync();

    /// <summary>
    /// Returns the first element of the sequence that satisfies a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first element that satisfies the condition.</returns>
    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Returns the first element of the sequence, or a default value if no element is found.
    /// </summary>
    /// <returns>The first element or default value.</returns>
    Task<TEntity?> FirstOrDefaultAsync();

    /// <summary>
    /// Returns the first element of the sequence that satisfies a condition, or a default value if no such element is found.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first element that satisfies the condition or default value.</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Returns the only element of the sequence.
    /// </summary>
    /// <returns>The single element.</returns>
    Task<TEntity> SingleAsync();

    /// <summary>
    /// Returns the only element of the sequence that satisfies a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The single element that satisfies the condition.</returns>
    Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Returns the only element of the sequence, or a default value if no element exists.
    /// </summary>
    /// <returns>The single element or default value.</returns>
    Task<TEntity?> SingleOrDefaultAsync();

    /// <summary>
    /// Returns the only element of the sequence that satisfies a condition, or a default value if no such element exists.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The single element that satisfies the condition or default value.</returns>
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Returns the number of elements in the sequence.
    /// </summary>
    /// <returns>The number of elements.</returns>
    Task<int> CountAsync();

    /// <summary>
    /// Returns the number of elements in the sequence that satisfy a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The number of elements that satisfy the condition.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Determines whether the sequence contains any elements.
    /// </summary>
    /// <returns>true if the sequence contains any elements; otherwise, false.</returns>
    Task<bool> AnyAsync();

    /// <summary>
    /// Determines whether any element of the sequence satisfies a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>true if any element satisfies the condition; otherwise, false.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Clears all entities from the object store.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync();
}