using Microsoft.JSInterop;

namespace IdxDb;

/// <summary>
/// Helper for loading any JavaScript (ES6) module and calling its exports
/// </summary>
public abstract class JsModule : IAsyncDisposable
{
    private readonly AsyncLazy<IJSObjectReference> _jsModuleProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isDisposed;

    /// <summary>
    /// On construction, we start loading the JS module 
    /// </summary>
    /// <param name="js"></param>
    /// <param name="moduleUrl">javascript web uri</param>
    protected JsModule(IJSRuntime js, string moduleUrl)
        => _jsModuleProvider = new AsyncLazy<IJSObjectReference>(async () =>
            await js.InvokeAsync<IJSObjectReference>("import", moduleUrl));

    /// <summary>
    /// invoke exports from the module
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="args"></param>
    protected async ValueTask InvokeVoidAsync(string identifier, params object[]? args)
    {
        var jsModule = await _jsModuleProvider.Value;
        await jsModule.InvokeVoidAsync(identifier, _cancellationTokenSource.Token, args);
    }

    /// <summary>
    /// invoke exports from the module with an expected return type T
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="args"></param>
    /// <typeparam name="T">Return Type</typeparam>
    /// <returns></returns>
    protected async ValueTask<T> InvokeAsync<T>(string identifier, params object[]? args)
    {
        var jsModule = await _jsModuleProvider.Value;
        return await jsModule.InvokeAsync<T>(identifier, _cancellationTokenSource.Token, args);
    }

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Perform the asynchronous cleanup the JS module.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_isDisposed)
            return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        if (_jsModuleProvider.IsValueCreated)
        {
            var module = await _jsModuleProvider.Value;
            await module.DisposeAsync().ConfigureAwait(false);
        }
        _isDisposed = true;
    }

    /// <summary>
    ///     Asynchronous initialization to delay the creation of a resource until it’s absolutely needed.
    /// </summary>
    /// <remarks>
    ///     This naive approach is much quicker than attempting to explain the use of a DotNext implementation. 
    ///     Alternatively, DotNext threading could be added via NuGet: `dotnet add package DotNext.Threading`.
    /// </remarks>
    /// <see href="https://devblogs.microsoft.com/pfxteam/asynclazyt/"/>
    /// <see href="https://github.com/dotnet/dotNext/blob/master/src/DotNext.Threading/Threading/AsyncLazy.cs"/>
    /// <see href="https://dev.azure.com/vercodev/Venso/_git/Vanguard?path=%2FVanguard%2FVanguard.Core%2FAsyncLazy.cs" />
    /// <typeparam name="T">Resource to be initialized asynchronously</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(taskFactory).Unwrap())
        {
        }
    }
}