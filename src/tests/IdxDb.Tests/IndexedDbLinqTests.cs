using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using FakeItEasy;
using System.Linq.Expressions;

namespace IdxDb.Tests;

public class IndexedDbLinqTests : IAsyncLifetime
{
    private IJSRuntime _jsRuntime = null!;
    private IJSObjectReference _module = null!;
    private IndexedDbContext _context = null!;
    private IIndexedDbSet<TestEntity> _dbSet = null!;

    public async ValueTask InitializeAsync()
    {
        _jsRuntime = A.Fake<IJSRuntime>();
        _module = A.Fake<IJSObjectReference>();

        // Setup the JSRuntime to return the module fake
        A.CallTo(() => _jsRuntime.InvokeAsync<IJSObjectReference>(
                A<string>.Ignored,
                A<object[]>.Ignored))
            .Returns(_module);

        // Setup default behavior for executeQuery to return empty array
        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(Array.Empty<TestEntity>());

        _context = new IndexedDbContext(_jsRuntime, "TestDb");
        await _context.InitializeAsync(typeof(TestEntity));

        _dbSet = _context.Set<TestEntity>();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();

        if (_module is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_module is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Where Tests

    [Fact]
    public async Task Where_WithSimpleCondition_ReturnsFilteredResults()
    {
        // Arrange
        var expectedData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 2, Name = "Jane", Age = 25 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _dbSet
            .Where(e => e.Age > 25)
            .ToArrayAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("John", result[0].Name);
    }

    [Fact]
    public async Task Where_WithMultipleConditions_ReturnsFilteredResults()
    {
        // Arrange
        var expectedData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30, IsActive = true },
            new TestEntity { Id = 2, Name = "Jane", Age = 25, IsActive = false },
            new TestEntity { Id = 3, Name = "Bob", Age = 35, IsActive = true }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _dbSet
            .Where(e => e.Age > 25 && e.IsActive)
            .ToArrayAsync();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(new[] { "Bob", "John" }, result.Select(r => r.Name).OrderBy(n => n).ToArray());
    }

    #endregion

    #region OrderBy Tests

    [Fact]
    public async Task OrderBy_WithSingleProperty_ReturnsSortedResults()
    {
        // Arrange
        var expectedData = new[]
        {
            new TestEntity { Id = 2, Name = "Jane", Age = 25 },
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 3, Name = "Bob", Age = 35 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _dbSet
            .OrderBy(e => e.Age)
            .ToArrayAsync();

        // Assert
        Assert.Equal(25, result[0].Age);
        Assert.Equal(30, result[1].Age);
        Assert.Equal(35, result[2].Age);
    }

    [Fact]
    public async Task OrderByDescending_ReturnsSortedResults()
    {
        // Arrange
        var expectedData = new[]
        {
            new TestEntity { Id = 3, Name = "Bob", Age = 35 },
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 2, Name = "Jane", Age = 25 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(expectedData);

        // Act
        var result = await _dbSet
            .OrderByDescending(e => e.Age)
            .ToArrayAsync();

        // Assert
        Assert.Equal(35, result[0].Age);
        Assert.Equal(30, result[1].Age);
        Assert.Equal(25, result[2].Age);
    }

    #endregion

    #region Select Tests

    [Fact]
    public async Task Select_WithProjection_ThrowsNotSupportedException()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 2, Name = "Jane", Age = 25 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act & Assert
        await Task.CompletedTask; // Satisfy async requirement
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await _dbSet
                .Select(e => new { e.Name, e.Age })
                .ToArrayAsync());
    }

    [Fact]
    public async Task Select_WithProjection_WorksAfterToArrayAsync()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 2, Name = "Jane", Age = 25 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act - First get array, then apply Select in memory
        var entities = await _dbSet.ToArrayAsync();
        var result = entities.Select(e => new { e.Name, e.Age }).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("John", result[0].Name);
        Assert.Equal(30, result[0].Age);
    }

    #endregion

    #region Skip/Take Tests

    [Fact]
    public async Task Skip_Take_ImplementsPagination()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John" },
            new TestEntity { Id = 2, Name = "Jane" },
            new TestEntity { Id = 3, Name = "Bob" },
            new TestEntity { Id = 4, Name = "Alice" },
            new TestEntity { Id = 5, Name = "Charlie" }
        };

        // Setup to return paginated results
        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(new[] { sourceData[2], sourceData[3] });

        // Act
        var result = await _dbSet
            .Skip(2)
            .Take(2)
            .ToArrayAsync();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal("Alice", result[1].Name);
    }

    #endregion

    #region First/Single Tests

    [Fact]
    public async Task FirstAsync_ReturnsFirstElement()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John" },
            new TestEntity { Id = 2, Name = "Jane" }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act
        var result = await _dbSet.FirstAsync();

        // Assert
        Assert.Equal("John", result.Name);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithNoResults_ReturnsNull()
    {
        // Arrange
        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(Array.Empty<TestEntity>());

        // Act
        var result = await _dbSet.FirstOrDefaultAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SingleAsync_WithOneResult_ReturnsElement()
    {
        // Arrange
        var sourceData = new[] { new TestEntity { Id = 1, Name = "John" } };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act
        var result = await _dbSet.SingleAsync();

        // Assert
        Assert.Equal("John", result.Name);
    }

    [Fact]
    public async Task SingleAsync_WithMultipleResults_ThrowsException()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John" },
            new TestEntity { Id = 2, Name = "Jane" }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _dbSet.SingleAsync());
    }

    #endregion

    #region Count/Any/All Tests

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.Ignored))
            .Returns(5);

        // Act
        var result = await _dbSet.CountAsync();

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsFilteredCount()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30 },
            new TestEntity { Id = 2, Name = "Jane", Age = 25 },
            new TestEntity { Id = 3, Name = "Bob", Age = 35 }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act
        var result = await _dbSet.CountAsync(e => e.Age > 30);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task AnyAsync_WithResults_ReturnsTrue()
    {
        // Arrange
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        var result = await _dbSet.AnyAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WithNoResults_ReturnsFalse()
    {
        // Arrange
        A.CallTo(() => _module.InvokeAsync<int>(
                "count",
                A<object[]>.Ignored))
            .Returns(0);

        // Act
        var result = await _dbSet.AnyAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Complex Query Tests

    [Fact]
    public async Task ComplexQuery_WithMultipleOperations_ReturnsCorrectResults()
    {
        // Arrange
        var sourceData = new[]
        {
            new TestEntity { Id = 1, Name = "John", Age = 30, IsActive = true },
            new TestEntity { Id = 2, Name = "Jane", Age = 25, IsActive = false },
            new TestEntity { Id = 3, Name = "Bob", Age = 35, IsActive = true },
            new TestEntity { Id = 4, Name = "Alice", Age = 28, IsActive = true },
            new TestEntity { Id = 5, Name = "Charlie", Age = 32, IsActive = false }
        };

        A.CallTo(() => _module.InvokeAsync<TestEntity[]>(
                "executeQuery",
                A<object[]>.Ignored))
            .Returns(sourceData);

        // Act
        var result = await _dbSet
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.Age)
            .Skip(1)
            .Take(2)
            .ToArrayAsync();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("John", result[0].Name);
        Assert.Equal("Alice", result[1].Name);
    }

    #endregion

    #region Add/Update/Delete Tests

    [Fact]
    public async Task AddAsync_AddsEntityToStore()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "John", Age = 30 };

        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        await _dbSet.AddAsync(entity);

        // Assert
        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.That.Matches(args =>
                    args.Length == 3 &&
                    args[2].Equals(entity))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TestEntity { Id = 1, Name = "John" },
            new TestEntity { Id = 2, Name = "Jane" }
        };

        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        await _dbSet.AddRangeAsync(entities);

        // Assert - Should call add for each entity
        A.CallTo(() => _module.InvokeAsync<object>(
                "add",
                A<object[]>.Ignored))
            .MustHaveHappened(2, Times.Exactly);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "John Updated", Age = 31 };

        A.CallTo(() => _module.InvokeAsync<object>(
                "update",
                A<object[]>.Ignored))
            .Returns(1);

        // Act
        await _dbSet.UpdateAsync(entity);

        // Assert
        A.CallTo(() => _module.InvokeAsync<object>(
                "update",
                A<object[]>.That.Matches(args =>
                    args.Length == 3 &&
                    args[2].Equals(entity))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteAsync_DeletesEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "John" };

        // Act
        await _dbSet.DeleteAsync(entity);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "deleteByKey",
                A<object[]>.That.Matches(args =>
                    args.Length == 3 &&
                    args[2].Equals(1))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteByIdAsync_DeletesEntityById()
    {
        // Act
        await _dbSet.DeleteAsync(1);

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "deleteByKey",
                A<object[]>.That.Matches(args =>
                    args.Length == 3 &&
                    args[2].Equals(1))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task FindAsync_ReturnsEntityByKey()
    {
        // Arrange
        var expectedEntity = new TestEntity { Id = 1, Name = "John" };

        A.CallTo(() => _module.InvokeAsync<TestEntity?>(
                "getByKey",
                A<object[]>.Ignored))
            .Returns(expectedEntity);

        // Act
        var result = await _dbSet.FindAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result!.Name);

        A.CallTo(() => _module.InvokeAsync<TestEntity?>(
                "getByKey",
                A<object[]>.That.Matches(args =>
                    args.Length == 3 &&
                    args[2].Equals(1))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ClearAsync_ClearsAllEntities()
    {
        // Act
        await _dbSet.ClearAsync();

        // Assert
        A.CallTo(() => _module.InvokeAsync<IJSVoidResult>(
                "clear",
                A<object[]>.That.Matches(args =>
                    args.Length == 2)))
            .MustHaveHappenedOnceExactly();
    }

    #endregion

    // Test entity class
    public class TestEntity
    {
        [IndexedDbKeyPath(AutoIncrement = true)]
        public int Id { get; set; }

        public required string Name { get; set; }

        public int Age { get; set; }

        public bool IsActive { get; set; }
    }
}
