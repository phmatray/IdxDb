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
    public async Task AddOneAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var item = new { Id = 1, Name = "TestItem" };

        // Act
        await _indexedDbInterop.AddOneAsync(dbName, storeName, item);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "addOne",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetAllAsync_Returns_Correct_Data()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var expectedData = new[] { new { Id = 1, Name = "TestItem1" }, new { Id = 2, Name = "TestItem2" } };

        A.CallTo(() => _module.InvokeAsync<object[]>(
                "getAll",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _indexedDbInterop.GetAllAsync<object>(dbName, storeName);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        A.CallTo(() => _module.InvokeAsync<object[]>(
                "getAll",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetOneAsync_Returns_Correct_Item()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int id = 1;
        var expectedItem = new { Id = 1, Name = "TestItem1" };

        A.CallTo(() => _module.InvokeAsync<object>(
                "getOne",
                A<object[]>.Ignored))
            .Returns(expectedItem);

        // Act
        var result = await _indexedDbInterop.GetOneAsync<object, int>(dbName, storeName, id);

        // Assert
        Assert.That(result, Is.EqualTo(expectedItem));
        A.CallTo(() => _module.InvokeAsync<object>(
                "getOne",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(id))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdateOneAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var item = new { Id = 1, Name = "UpdatedItem" };

        // Act
        await _indexedDbInterop.UpdateOneAsync(dbName, storeName, item);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "updateOne",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task DeleteOneAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int id = 1;

        // Act
        await _indexedDbInterop.DeleteOneAsync(dbName, storeName, id);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "deleteOne",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(id))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpgradeDatabaseAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        int newVersion = 2;
        var storeSchemas = new[]
        {
            new
            {
                name = "NewStore",
                options = new { keyPath = "id", autoIncrement = true },
                indexes = new[]
                {
                    new { name = "NameIndex", keyPath = "name", unique = false }
                }
            }
        };

        // Act
        await _indexedDbInterop.UpgradeDatabaseAsync(dbName, newVersion, storeSchemas);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "upgradeDatabase",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(newVersion) &&
                    args[2].Equals(storeSchemas))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task AddManyAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var items = new[]
        {
            new { Id = 1, Name = "Item1" },
            new { Id = 2, Name = "Item2" }
        };

        // Act
        await _indexedDbInterop.AddManyAsync(dbName, storeName, items);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "addMany",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(items))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CreateIndexAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        string indexName = "NameIndex";
        string keyPath = "name";
        bool unique = false;

        // Act
        await _indexedDbInterop.CreateIndexAsync(dbName, storeName, indexName, keyPath, unique);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "createIndex",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(indexName) &&
                    args[3].Equals(keyPath) &&
                    args[4].Equals(unique))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetAllByIndexAsync_Returns_Correct_Data()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        string indexName = "NameIndex";
        string query = "TestItem";
        var expectedData = new[] { new { Id = 1, Name = "TestItem" } };

        A.CallTo(() => _module.InvokeAsync<object[]>(
                "getAllByIndex",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _indexedDbInterop.GetAllByIndexAsync<object>(dbName, storeName, indexName, query);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        A.CallTo(() => _module.InvokeAsync<object[]>(
                "getAllByIndex",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(indexName) &&
                    args[3].Equals(query))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ExecuteTransactionAsync_Performs_Operations_In_Transaction()
    {
        // Arrange
        string dbName = "TestDb";
        string[] storeNames = { "Store1", "Store2" };
        string mode = "readwrite";

        // Act
        await _indexedDbInterop.ExecuteTransactionAsync(dbName, storeNames, mode, async () =>
        {
            // Simulate operations within the transaction
            await Task.CompletedTask;
        });

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "beginTransaction",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeNames) &&
                    args[2].Equals(mode))))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "commitTransaction",
                A<object[]>.Ignored))
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
                    args[1].Equals(storeName))))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ClearStoreAsync_Calls_JS_Interop_With_Correct_Parameters()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";

        // Act
        await _indexedDbInterop.ClearStoreAsync(dbName, storeName);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "clearStore",
                A<object[]>.That.Matches(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))))
            .MustHaveHappenedOnceExactly();
    }
}