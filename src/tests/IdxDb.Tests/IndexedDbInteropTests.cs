using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using FakeItEasy;

namespace IdxDb.Tests;

public class IndexedDbInteropTests : IAsyncLifetime
{
    private IJSRuntime _jsRuntime;
    private IndexedDbInterop _indexedDbInterop;
    private IJSObjectReference _module;

    public async ValueTask InitializeAsync()
    {
        _jsRuntime = A.Fake<IJSRuntime>();
        _module = A.Fake<IJSObjectReference>();

        // Setup the JSRuntime to return the module fake
        A.CallTo(() => _jsRuntime.InvokeAsync<IJSObjectReference>(
                A<string>.Ignored,
                A<object[]>.Ignored))
            .Returns(_module);

        _indexedDbInterop = new IndexedDbInterop(_jsRuntime);

        // Initialize the database to pass the check
        await _indexedDbInterop.OpenIndexedDbAsync("TestDb", 1, new[]
        {
            new StoreDefinition
            {
                Name = "TestStore",
                Options = new StoreOptions { KeyPath = "id", AutoIncrement = true },
                Indexes = Array.Empty<IndexDefinition>()
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _indexedDbInterop.DisposeAsync();
        if (_module is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_module is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task AddAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var item = new { Id = 1, Name = "TestItem" };

        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        await _indexedDbInterop.AddAsync(dbName, storeName, item);

        // Assert
        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteQueryAsync_Returns_Correct_Data()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var queryOptions = new { skip = 0, take = 10 };
        var expectedData = new[] { new { Id = 1, Name = "TestItem1" }, new { Id = 2, Name = "TestItem2" } };

        A.CallTo(() => _module.InvokeAsync<object[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _indexedDbInterop.ExecuteQueryAsync<object>(dbName, storeName, queryOptions);

        // Assert
        Assert.Equal(expectedData, result);
        A.CallTo(() => _module.InvokeAsync<object[]>(
                "executeQuery",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(queryOptions))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByKeyAsync_Returns_Correct_Item()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int key = 1;
        var expectedItem = new { Id = 1, Name = "TestItem1" };

        A.CallTo(() => _module.InvokeAsync<object?>(
                "getByKey",
                A<object[]>.Ignored))
            .Returns(expectedItem);

        // Act
        var result = await _indexedDbInterop.GetByKeyAsync<object, int>(dbName, storeName, key);

        // Assert
        Assert.Equal(expectedItem, result);
        A.CallTo(() => _module.InvokeAsync<object?>(
                "getByKey",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(key))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var item = new { Id = 1, Name = "UpdatedItem" };

        A.CallTo(() => _module.InvokeAsync<object>(
                "update",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        await _indexedDbInterop.UpdateAsync(dbName, storeName, item);

        // Assert
        A.CallTo(() => _module.InvokeAsync<object>(
                "update",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteByKeyAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int key = 1;

        // Act
        await _indexedDbInterop.DeleteByKeyAsync(dbName, storeName, key);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "deleteByKey",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(key))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int expectedCount = 5;

        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.Ignored))
            .Returns(expectedCount);

        // Act
        var result = await _indexedDbInterop.CountAsync(dbName, storeName);

        // Assert
        Assert.Equal(expectedCount, result);
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2] != null)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CountAsync_With_QueryOptions_Returns_Correct_Count()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var queryOptions = new { filter = new { IsActive = true } };
        int expectedCount = 3;

        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.Ignored))
            .Returns(expectedCount);

        // Act
        var result = await _indexedDbInterop.CountAsync(dbName, storeName, queryOptions);

        // Assert
        Assert.Equal(expectedCount, result);
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(queryOptions))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ClearAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";

        // Act
        await _indexedDbInterop.ClearAsync(dbName, storeName);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "clear",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task OpenIndexedDbAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "NewTestDb";
        int version = 2;
        var stores = new[]
        {
            new StoreDefinition
            {
                Name = "NewStore",
                Options = new StoreOptions { KeyPath = "id", AutoIncrement = true },
                Indexes = new[]
                {
                    new IndexDefinition { Name = "NameIndex", KeyPath = "name", Unique = false }
                }
            }
        };

        // Act
        await _indexedDbInterop.OpenIndexedDbAsync(dbName, version, stores);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "openDatabase",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(version) &&
                    args[2].Equals(stores))))
            .MustHaveHappened();
    }

    [Fact]
    public async Task CloseDatabaseAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";

        // Act
        await _indexedDbInterop.CloseDatabaseAsync(dbName);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "closeDatabase",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Operations_Without_OpenDatabase_Throw_InvalidOperationException()
    {
        // Arrange
        var uninitializedInterop = new IndexedDbInterop(_jsRuntime);
        string dbName = "UninitializedDb";
        string storeName = "TestStore";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.ExecuteQueryAsync<object>(dbName, storeName));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.GetByKeyAsync<object, int>(dbName, storeName, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.AddAsync(dbName, storeName, new { }));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.UpdateAsync(dbName, storeName, new { }));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.DeleteByKeyAsync(dbName, storeName, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.CountAsync(dbName, storeName));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.ClearAsync(dbName, storeName));
    }
}
