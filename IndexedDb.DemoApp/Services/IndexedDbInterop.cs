using Microsoft.JSInterop;

namespace FormLang.BlazorApp.Services;

public class IndexedDbInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public IndexedDbInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(
            () => jsRuntime
                .InvokeAsync<IJSObjectReference>("import", "./js/idb.js")
                .AsTask());
    }

    public async Task AddOneAsync(string dbName, string storeName, object item)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("addOne", dbName, storeName, item);
    }

    public async Task<T[]> GetAllAsync<T>(string dbName, string storeName)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<T[]>("getAll", dbName, storeName);
    }

    public async Task<TRecord?> GetOneAsync<TRecord, TKey>(string dbName, string storeName, TKey id)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<TRecord>("getOne", dbName, storeName, id);
    }

    public async Task UpdateOneAsync(string dbName, string storeName, object item)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("updateOne", dbName, storeName, item);
    }

    public async Task DeleteOneAsync<TKey>(string dbName, string storeName, TKey id)
    {
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("deleteOne", dbName, storeName, id);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}