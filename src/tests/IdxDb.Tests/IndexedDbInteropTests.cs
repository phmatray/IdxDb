using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using FakeItEasy;

namespace IdxDb.Tests;

[TestFixture]
public class IndexedDbInteropTests
{
    private IJSRuntime _jsRuntime;
    private IndexedDbInterop _indexedDbInterop;
    private IJSObjectReference _module;

    [SetUp]
    public async Task Setup()
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

    [TearDown]
    public async Task TearDown()
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

    [Test]
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

    [Test]
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
        Assert.That(result, Is.EqualTo(expectedData));
        A.CallTo(() => _module.InvokeAsync<object[]>(
                "executeQuery",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(queryOptions))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
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
        Assert.That(result, Is.EqualTo(expectedItem));
        A.CallTo(() => _module.InvokeAsync<object?>(
                "getByKey",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(key))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
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

    [Test]
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

    [Test]
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
        Assert.That(result, Is.EqualTo(expectedCount));
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2] != null)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
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
        Assert.That(result, Is.EqualTo(expectedCount));
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(queryOptions))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public void Operations_Without_OpenDatabase_Throw_InvalidOperationException()
    {
        // Arrange
        var uninitializedInterop = new IndexedDbInterop(_jsRuntime);
        string dbName = "UninitializedDb";
        string storeName = "TestStore";

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.ExecuteQueryAsync<object>(dbName, storeName));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.GetByKeyAsync<object, int>(dbName, storeName, 1));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.AddAsync(dbName, storeName, new { }));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.UpdateAsync(dbName, storeName, new { }));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.DeleteByKeyAsync(dbName, storeName, 1));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.CountAsync(dbName, storeName));
        
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitializedInterop.ClearAsync(dbName, storeName));
    }
}