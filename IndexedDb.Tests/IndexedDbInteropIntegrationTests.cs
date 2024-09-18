using Bunit;

namespace IndexedDb.Tests;

[TestFixture]
public class IndexedDbInteropIntegrationTests
{
    private Bunit.TestContext _testContext;
    private IndexedDbInterop _indexedDbInterop;

    [SetUp]
    public void Setup()
    {
        _testContext = new Bunit.TestContext();
        _testContext.JSInterop.Mode = JSRuntimeMode.Loose; // Allows any JS interop calls

        // Register IndexedDbInterop with the real IJSRuntime
        _indexedDbInterop = new IndexedDbInterop(_testContext.JSInterop.JSRuntime);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _indexedDbInterop.DisposeAsync();
        _testContext.Dispose();
    }

    [Test]
    public async Task AddAndGetOneAsync_Should_Add_Item_And_Retrieve_It()
    {
        // Arrange
        const string dbName = "IntegrationTestDb";
        const string storeName = "TestStore";
        var item = new { id = 1, name = "TestItem" };

        // Act
        await _indexedDbInterop.AddOneAsync(dbName, storeName, item);
        var retrievedItem = await _indexedDbInterop.GetOneAsync<dynamic, int>(dbName, storeName, 1);

        // Assert
        Assert.That(retrievedItem, Is.Not.Null);
        Assert.That(retrievedItem.id, Is.EqualTo(item.id));
        Assert.That(retrievedItem.name, Is.EqualTo(item.name));
    }

    [Test]
    public async Task AddManyAndGetAllAsync_Should_Add_Items_And_Retrieve_Them()
    {
        // Arrange
        const string dbName = "IntegrationTestDb";
        const string storeName = "TestStore";
        var items = new[]
        {
            new { id = 1, name = "Item1" },
            new { id = 2, name = "Item2" }
        };

        // Act
        await _indexedDbInterop.AddManyAsync(dbName, storeName, items);
        var retrievedItems = await _indexedDbInterop.GetAllAsync<dynamic>(dbName, storeName);

        // Assert
        Assert.That(retrievedItems, Is.Not.Null);
        Assert.That(retrievedItems, Has.Length.EqualTo(2));
        Assert.That(retrievedItems, Is.EquivalentTo(items));
    }

    // Add more tests for other methods
}