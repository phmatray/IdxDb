using Microsoft.JSInterop;
using Moq;

namespace IdxDb.Tests;

[TestFixture]
public class IndexedDbInteropTests
{
    private Mock<IJSRuntime> _jsRuntimeMock;
    private IndexedDbInterop _indexedDbInterop;
    private Mock<IJSObjectReference> _moduleMock;

    [SetUp]
    public void Setup()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _moduleMock = new Mock<IJSObjectReference>();

        // Setup the JSRuntime to return the module mock
        _jsRuntimeMock
            .Setup(js => js.InvokeAsync<IJSObjectReference>(
                It.IsAny<string>(),
                It.IsAny<object[]>()))
            .ReturnsAsync(_moduleMock.Object);

        _indexedDbInterop = new IndexedDbInterop(_jsRuntimeMock.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _indexedDbInterop.DisposeAsync();
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "addOne",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))),
            Times.Once);
    }

    [Test]
    public async Task GetAllAsync_Returns_Correct_Data()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        var expectedData = new[] { new { Id = 1, Name = "TestItem1" }, new { Id = 2, Name = "TestItem2" } };

        _moduleMock.Setup(m => m.InvokeAsync<object[]>(
                "getAll",
                It.IsAny<object[]>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _indexedDbInterop.GetAllAsync<object>(dbName, storeName);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        _moduleMock.Verify(m => m.InvokeAsync<object[]>(
                "getAll",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))),
            Times.Once);
    }

    [Test]
    public async Task GetOneAsync_Returns_Correct_Item()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int id = 1;
        var expectedItem = new { Id = 1, Name = "TestItem1" };

        _moduleMock.Setup(m => m.InvokeAsync<object>(
                "getOne",
                It.IsAny<object[]>()))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _indexedDbInterop.GetOneAsync<object, int>(dbName, storeName, id);

        // Assert
        Assert.That(result, Is.EqualTo(expectedItem));
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "getOne",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(id))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "updateOne",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(item))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "deleteOne",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(id))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "upgradeDatabase",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(newVersion) &&
                    args[2].Equals(storeSchemas))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "addMany",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(items))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "createIndex",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(indexName) &&
                    args[3].Equals(keyPath) &&
                    args[4].Equals(unique))),
            Times.Once);
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

        _moduleMock.Setup(m => m.InvokeAsync<object[]>(
                "getAllByIndex",
                It.IsAny<object[]>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _indexedDbInterop.GetAllByIndexAsync<object>(dbName, storeName, indexName, query);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        _moduleMock.Verify(m => m.InvokeAsync<object[]>(
                "getAllByIndex",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName) &&
                    args[2].Equals(indexName) &&
                    args[3].Equals(query))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "beginTransaction",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeNames) &&
                    args[2].Equals(mode))),
            Times.Once);

        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "commitTransaction",
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Test]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        string dbName = "TestDb";
        string storeName = "TestStore";
        int expectedCount = 5;

        _moduleMock.Setup(m => m.InvokeAsync<int>(
                "count",
                It.IsAny<object[]>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _indexedDbInterop.CountAsync(dbName, storeName);

        // Assert
        Assert.That(result, Is.EqualTo(expectedCount));
        _moduleMock.Verify(m => m.InvokeAsync<int>(
                "count",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))),
            Times.Once);
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
        _moduleMock.Verify(m => m.InvokeAsync<object>(
                "clearStore",
                It.Is<object[]>(args =>
                    args[0].Equals(dbName) &&
                    args[1].Equals(storeName))),
            Times.Once);
    }
}