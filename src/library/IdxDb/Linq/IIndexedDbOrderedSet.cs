using System.Linq;

namespace IdxDb;

/// <summary>
/// Represents an ordered queryable collection of entities from an IndexedDB object store.
/// </summary>
/// <typeparam name="TEntity">The type of entity in the collection.</typeparam>
public interface IIndexedDbOrderedSet<TEntity> : IIndexedDbSet<TEntity>, IOrderedQueryable<TEntity>
{
}