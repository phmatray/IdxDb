using Microsoft.JSInterop;

namespace IdxDb;

public class IndexedDb(IJSRuntime jsRuntime)
    : JsModule(jsRuntime, "./_content/IdxDb/idb.mjs")
{
    public async ValueTask<object> Open(string dbName, int version)
    {
        var invokeAsync = await InvokeAsync<object>(JavaScriptMethods.Open, dbName, version);
        return invokeAsync;
    }
    
    private static class JavaScriptMethods
    {
        public const string Open = "open";
    }
}