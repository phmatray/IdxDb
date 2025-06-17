using System.Collections;
using System.Linq.Expressions;

namespace IdxDb;

/// <summary>
/// Represents an ordered queryable collection of entities from an IndexedDB object store.
/// </summary>
/// <typeparam name="TEntity">The type of entity in the collection.</typeparam>
internal class IndexedDbOrderedSet<TEntity> : IIndexedDbOrderedSet<TEntity>, IAsyncDisposable where TEntity : class
{
    private readonly IndexedDbSet<TEntity> _innerSet;

    public IndexedDbOrderedSet(IndexedDbSet<TEntity> innerSet)
    {
        _innerSet = innerSet ?? throw new ArgumentNullException(nameof(innerSet));
    }

    #region IQueryable Implementation

    public Type ElementType => _innerSet.ElementType;
    public Expression Expression => _innerSet.Expression;
    public IQueryProvider Provider => _innerSet.Provider;

    public IEnumerator<TEntity> GetEnumerator() => _innerSet.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region IIndexedDbSet Implementation

    public Task AddAsync(TEntity entity) => _innerSet.AddAsync(entity);
    public Task AddRangeAsync(IEnumerable<TEntity> entities) => _innerSet.AddRangeAsync(entities);
    public Task UpdateAsync(TEntity entity) => _innerSet.UpdateAsync(entity);
    public Task DeleteAsync(TEntity entity) => _innerSet.DeleteAsync(entity);
    public Task DeleteAsync<TKey>(TKey key) => _innerSet.DeleteAsync(key);
    public Task<TEntity?> FindAsync<TKey>(TKey key) => _innerSet.FindAsync(key);
    public Task ClearAsync() => _innerSet.ClearAsync();

    public Task<TEntity[]> ToArrayAsync() => _innerSet.ToArrayAsync();
    public Task<List<TEntity>> ToListAsync() => _innerSet.ToListAsync();
    public Task<TEntity> FirstAsync() => _innerSet.FirstAsync();
    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.FirstAsync(predicate);
    public Task<TEntity?> FirstOrDefaultAsync() => _innerSet.FirstOrDefaultAsync();
    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.FirstOrDefaultAsync(predicate);
    public Task<TEntity> SingleAsync() => _innerSet.SingleAsync();
    public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.SingleAsync(predicate);
    public Task<TEntity?> SingleOrDefaultAsync() => _innerSet.SingleOrDefaultAsync();
    public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.SingleOrDefaultAsync(predicate);
    public Task<int> CountAsync() => _innerSet.CountAsync();
    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.CountAsync(predicate);
    public Task<bool> AnyAsync() => _innerSet.AnyAsync();
    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate) => _innerSet.AnyAsync(predicate);

    #endregion

    public async ValueTask DisposeAsync()
    {
        await _innerSet.DisposeAsync();
    }
}