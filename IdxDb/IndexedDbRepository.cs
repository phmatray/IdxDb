using Microsoft.JSInterop;

namespace IdxDb;

/// <summary>
/// A generic repository for interacting with a specific object store in IndexedDB.
/// </summary>
/// <typeparam name="TItem">The type of items stored in the object store.</typeparam>
public class IndexedDbRepository<TItem> : IAsyncDisposable
{
    private readonly IndexedDbInterop _indexedDbInterop;
    private readonly string _dbName;
    private readonly string _storeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbRepository{TItem}"/> class.
    /// </summary>
    /// <param name="jsRuntime">An instance of <see cref="IJSRuntime"/> for JavaScript interop.</param>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    public IndexedDbRepository(IJSRuntime jsRuntime, string dbName, string storeName)
    {
        _indexedDbInterop = new IndexedDbInterop(jsRuntime);
        _dbName = dbName;
        _storeName = storeName;
    }

    /// <summary>
    /// Adds a single item to the object store.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public async Task AddOneAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        await _indexedDbInterop.AddOneAsync(_dbName, _storeName, item);
    }

    /// <summary>
    /// Adds multiple items to the object store in a single operation.
    /// </summary>
    /// <param name="items">An array of items to add.</param>
    public async Task AddManyAsync(TItem[] items)
    {
        var rawItems = items.Cast<object>().ToArray();
        await _indexedDbInterop.AddManyAsync(_dbName, _storeName, rawItems);
    }

    /// <summary>
    /// Retrieves all items from the object store.
    /// </summary>
    /// <returns>An array of items.</returns>
    public async Task<TItem[]> GetAllAsync()
    {
        return await _indexedDbInterop.GetAllAsync<TItem>(_dbName, _storeName);
    }

    /// <summary>
    /// Retrieves a single item by its key from the object store.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify the item.</typeparam>
    /// <param name="id">The key of the item to retrieve.</param>
    /// <returns>The item, or <c>null</c> if not found.</returns>
    public async Task<TItem?> GetOneAsync<TKey>(TKey id)
    {
        return await _indexedDbInterop.GetOneAsync<TItem, TKey>(_dbName, _storeName, id);
    }

    /// <summary>
    /// Updates an existing item in the object store.
    /// </summary>
    /// <param name="item">The item to update.</param>
    public async Task UpdateOneAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        await _indexedDbInterop.UpdateOneAsync(_dbName, _storeName, item);
    }

    /// <summary>
    /// Deletes an item from the object store by its key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify the item.</typeparam>
    /// <param name="id">The key of the item to delete.</param>
    public async Task DeleteOneAsync<TKey>(TKey id)
    {
        await _indexedDbInterop.DeleteOneAsync(_dbName, _storeName, id);
    }

    /// <summary>
    /// Counts the number of records in the object store.
    /// </summary>
    /// <returns>The count of records.</returns>
    public async Task<int> CountAsync()
    {
        return await _indexedDbInterop.CountAsync(_dbName, _storeName);
    }

    /// <summary>
    /// Clears all records from the object store.
    /// </summary>
    public async Task ClearStoreAsync()
    {
        await _indexedDbInterop.ClearStoreAsync(_dbName, _storeName);
    }

    /// <summary>
    /// Disposes the JavaScript module reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _indexedDbInterop.DisposeAsync();
    }
}