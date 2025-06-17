using Microsoft.JSInterop;

namespace IdxDb;

/// <summary>
/// Provides low-level methods for interacting with IndexedDB via JavaScript interop.
/// This class is primarily used internally by the LINQ provider.
/// </summary>
public class IndexedDbInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private readonly Dictionary<string, bool> _openDatabases = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbInterop"/> class.
    /// </summary>
    /// <param name="jsRuntime">An instance of <see cref="IJSRuntime"/> for JavaScript interop.</param>
    public IndexedDbInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() => jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/IdxDb/indexeddb-linq.mjs")
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
        await module.InvokeVoidAsync("openDatabase", dbName, version, stores);
        _openDatabases[dbName] = true;
    }

    /// <summary>
    /// Executes a query on an object store.
    /// </summary>
    /// <typeparam name="T">The type of records in the store.</typeparam>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="queryOptions">Query options including filters, sorting, and pagination.</param>
    /// <returns>An array of records matching the query.</returns>
    public async Task<T[]> ExecuteQueryAsync<T>(string dbName, string storeName, object? queryOptions = null)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T[]>("executeQuery", dbName, storeName, queryOptions ?? new { });
    }

    /// <summary>
    /// Gets a single record by its key.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="key">The record key.</param>
    /// <returns>The record if found, otherwise null.</returns>
    public async Task<T?> GetByKeyAsync<T, TKey>(string dbName, string storeName, TKey key)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<T?>("getByKey", dbName, storeName, key);
    }

    /// <summary>
    /// Adds a record to the store.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="record">The record to add.</param>
    /// <returns>The key of the added record.</returns>
    public async Task<object> AddAsync<T>(string dbName, string storeName, T record)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<object>("add", dbName, storeName, record);
    }

    /// <summary>
    /// Updates a record in the store.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="record">The record to update.</param>
    /// <returns>The key of the updated record.</returns>
    public async Task<object> UpdateAsync<T>(string dbName, string storeName, T record)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<object>("update", dbName, storeName, record);
    }

    /// <summary>
    /// Deletes a record by its key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="key">The record key.</param>
    public async Task DeleteByKeyAsync<TKey>(string dbName, string storeName, TKey key)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("deleteByKey", dbName, storeName, key);
    }

    /// <summary>
    /// Counts records in a store with optional query options.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    /// <param name="queryOptions">Optional query options for filtered counting.</param>
    /// <returns>The number of records matching the criteria.</returns>
    public async Task<int> CountAsync(string dbName, string storeName, object? queryOptions = null)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<int>("count", dbName, storeName, queryOptions ?? new { });
    }

    /// <summary>
    /// Clears all records from a store.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <param name="storeName">The store name.</param>
    public async Task ClearAsync(string dbName, string storeName)
    {
        EnsureDatabaseOpen(dbName);
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("clear", dbName, storeName);
    }

    /// <summary>
    /// Closes a database connection.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    public async Task CloseDatabaseAsync(string dbName)
    {
        if (_openDatabases.ContainsKey(dbName))
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("closeDatabase", dbName);
            _openDatabases.Remove(dbName);
        }
    }

    /// <summary>
    /// Deletes an entire database.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    public async Task DeleteDatabaseAsync(string dbName)
    {
        // Close it first if it's open
        await CloseDatabaseAsync(dbName);
        
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("deleteDatabase", dbName);
    }

    /// <summary>
    /// Disposes the JavaScript module reference.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Close all open databases
        var dbNames = _openDatabases.Keys.ToList();
        foreach (var dbName in dbNames)
        {
            await CloseDatabaseAsync(dbName);
        }

        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }

    private void EnsureDatabaseOpen(string dbName)
    {
        if (!_openDatabases.ContainsKey(dbName))
        {
            throw new InvalidOperationException($"Database '{dbName}' is not open. Call OpenIndexedDbAsync first.");
        }
    }
}