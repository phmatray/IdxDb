using Microsoft.JSInterop;

namespace IdxDb;

/// <summary>
/// Provides methods for interacting with IndexedDB via JavaScript interop.
/// </summary>
public class IndexedDbInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private bool _isDbInitialized = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbInterop"/> class.
    /// </summary>
    /// <param name="jsRuntime">An instance of <see cref="IJSRuntime"/> for JavaScript interop.</param>
    public IndexedDbInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/IdxDb/idb-old.mjs")
            .AsTask());
    }
    
    /// <summary>
    /// Opens the database and initializes object stores if needed.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <param name="version">The database version.</param>
    /// <param name="stores">An array of store definitions.</param>
    public async Task OpenIndexedDbAsync(string dbName, int version, StoreDefinition[] stores)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("openIndexedDB", dbName, version, stores);
        _isDbInitialized = true;
    }

    // Ensure that the database is initialized before performing operations
    private async Task EnsureDbInitializedAsync()
    {
        if (!_isDbInitialized)
        {
            throw new InvalidOperationException("Database is not initialized. Please call OpenIndexedDbAsync first.");
        }
    }

    /// <summary>
    /// Adds a single item to the specified object store.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="item">The item to add.</param>
    public async Task AddOneAsync(string dbName, string storeName, object item)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("addOne", dbName, storeName, item);
    }

    /// <summary>
    /// Retrieves all items from the specified object store.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the object store.</typeparam>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of items.</returns>
    public async Task<T[]> GetAllAsync<T>(string dbName, string storeName)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T[]>("getAll", dbName, storeName);
    }

    /// <summary>
    /// Retrieves a single item by its key from the specified object store.
    /// </summary>
    /// <typeparam name="TRecord">The type of the item to retrieve.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify the item.</typeparam>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="id">The key of the item to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the item, or <c>null</c> if not found.</returns>
    public async Task<TRecord?> GetOneAsync<TRecord, TKey>(string dbName, string storeName, TKey id)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<TRecord>("getOne", dbName, storeName, id);
    }

    /// <summary>
    /// Updates an existing item in the specified object store.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="item">The item to update.</param>
    public async Task UpdateOneAsync(string dbName, string storeName, object item)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("updateOne", dbName, storeName, item);
    }

    /// <summary>
    /// Deletes an item from the specified object store by its key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to identify the item.</typeparam>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="id">The key of the item to delete.</param>
    public async Task DeleteOneAsync<TKey>(string dbName, string storeName, TKey id)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("deleteOne", dbName, storeName, id);
    }

    /// <summary>
    /// Upgrades the database schema by adding or modifying object stores and indexes.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="newVersion">The new version number for the database.</param>
    /// <param name="storeSchemas">An array of store schema definitions.</param>
    public async Task UpgradeDatabaseAsync(string dbName, int newVersion, object[] storeSchemas)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("upgradeDatabase", dbName, newVersion, storeSchemas);
    }

    /// <summary>
    /// Adds multiple items to the specified object store in a single operation.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="items">An array of items to add.</param>
    public async Task AddManyAsync(string dbName, string storeName, object[] items)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("addMany", dbName, storeName, items);
    }

    /// <summary>
    /// Creates an index on the specified object store.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="indexName">The name of the index to create.</param>
    /// <param name="keyPath">The key path for the index.</param>
    /// <param name="unique">Indicates whether the index should enforce unique values.</param>
    public async Task CreateIndexAsync(string dbName, string storeName, string indexName, string keyPath,
        bool unique = false)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("createIndex", dbName, storeName, indexName, keyPath, unique);
    }

    /// <summary>
    /// Retrieves all items matching the specified query on a given index.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the object store.</typeparam>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <param name="indexName">The name of the index to query.</param>
    /// <param name="query">The query value or range to match.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of matching items.</returns>
    public async Task<T[]> GetAllByIndexAsync<T>(string dbName, string storeName, string indexName, object query)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T[]>("getAllByIndex", dbName, storeName, indexName, query);
    }

    /// <summary>
    /// Executes multiple operations within a single transaction.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeNames">An array of object store names involved in the transaction.</param>
    /// <param name="mode">The transaction mode ('readonly' or 'readwrite').</param>
    /// <param name="transactionBody">An asynchronous function containing the operations to execute within the transaction.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteTransactionAsync(string dbName, string[] storeNames, string mode,
        Func<Task> transactionBody)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("beginTransaction", dbName, storeNames, mode);
        await transactionBody();
        await module.InvokeVoidAsync("commitTransaction");
    }

    /// <summary>
    /// Counts the number of records in the specified object store.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count of records.</returns>
    public async Task<int> CountAsync(string dbName, string storeName)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<int>("count", dbName, storeName);
    }

    /// <summary>
    /// Clears all records from the specified object store.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="storeName">The name of the object store.</param>
    public async Task ClearStoreAsync(string dbName, string storeName)
    {
        await EnsureDbInitializedAsync();
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clearStore", dbName, storeName);
    }

    /// <summary>
    /// Disposes the JavaScript module reference.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}