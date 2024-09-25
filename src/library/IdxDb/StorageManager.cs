using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace IdxDb;

public class StorageManager(IJSRuntime jsRuntime)
    : JsModule(jsRuntime, "./_content/IdxDb/idb-storage-manager.mjs")
{
    public async ValueTask<EstimateResult> Estimate()
    {
        return await InvokeAsync<EstimateResult>(JavaScriptMethods.Estimate);
    }
    
    public async ValueTask<object> GetDirectory()
    {
        var invokeAsync = await InvokeAsync<object>(JavaScriptMethods.GetDirectory);
        return invokeAsync;
    }

    private static class JavaScriptMethods
    {
        public const string Estimate = "estimate";
        public const string GetDirectory = "getDirectory";
    }
}

public record EstimateResult
{
    [JsonPropertyName("quota")]
    public long Quota { get; init; }
    
    [JsonPropertyName("usage")]
    public long Usage { get; init; }
    
    [JsonPropertyName("usageDetails")]
    public object UsageDetails { get; init; }
    
    public double PercentUsed
        => (Usage / Quota) * 100;
    
    public string QuotaInMB
        => $"{Quota / 1024 / 1024:F2}MB";
}

// FileSystemDirectoryHandle
