using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace IdxDb;

public class IndexedDb(IJSRuntime jsRuntime)
    : JsModule(jsRuntime, "./_content/IdxDb/idb.mjs")
{
    public async ValueTask<object> Open(string dbName, int version = 1)
    {
        var invokeAsync = await InvokeAsync<object>(JavaScriptMethods.Open, dbName, version);
        return invokeAsync;
    }
    
    public async ValueTask<DatabaseInfo[]> Databases()
    {
        var invokeAsync = await InvokeAsync<DatabaseInfo[]>(JavaScriptMethods.Databases);
        return invokeAsync;
    }
    
    public async ValueTask<int> Cmp(string first, string second)
    {
        var invokeAsync = await InvokeAsync<int>(JavaScriptMethods.Cmp, first, second);
        return invokeAsync;
    }
    
    public async ValueTask DeleteDatabase(string dbName)
    {
        await InvokeVoidAsync(JavaScriptMethods.DeleteDatabase, dbName);
    }
    
    private static class JavaScriptMethods
    {
        public const string Open = "open";
        public const string Databases = "databases";
        public const string Cmp = "cmp";
        public const string DeleteDatabase = "deleteDatabase";
    }
    
    [JSInvokable]
    public static Task OnUpgradeNeeded(string message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
    
    [JSInvokable]
    public Task OnOpen(IJSObjectReference objRef)
    {
        Console.WriteLine(objRef);
        return Task.CompletedTask;
    }
}

public record DatabaseInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("version")]
    public required int Version { get; init; }
}



// interface IDBVersionChangeEvent extends Event {     readonly newVersion: number | null     readonly oldVersion: number }
