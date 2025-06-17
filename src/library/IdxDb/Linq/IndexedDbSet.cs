using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace IdxDb;

/// <summary>
/// Represents a queryable collection of entities from an IndexedDB object store.
/// Implements IQueryable to provide LINQ support.
/// </summary>
/// <typeparam name="TEntity">The type of entity in the collection.</typeparam>
public class IndexedDbSet<TEntity> : IIndexedDbSet<TEntity>, IAsyncDisposable where TEntity : class
{
    private readonly IndexedDbContext _context;
    private readonly IndexedDbInterop _interop;
    private readonly string _databaseName;
    private readonly string _storeName;
    private readonly IndexedDbQueryProvider _provider;
    private readonly Expression _expression;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedDbSet{TEntity}"/> class.
    /// </summary>
    internal IndexedDbSet(IndexedDbContext context, IndexedDbInterop interop, string databaseName, string storeName)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _storeName = storeName ?? throw new ArgumentNullException(nameof(storeName));
        
        _provider = new IndexedDbQueryProvider(this);
        _expression = Expression.Constant(this);
    }

    /// <summary>
    /// Initializes a new instance with a specific expression.
    /// </summary>
    internal IndexedDbSet(IndexedDbSet<TEntity> source, Expression expression)
    {
        _context = source._context;
        _interop = source._interop;
        _databaseName = source._databaseName;
        _storeName = source._storeName;
        _provider = source._provider;
        _expression = expression;
    }

    #region IQueryable Implementation

    public Type ElementType => typeof(TEntity);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;

    public IEnumerator<TEntity> GetEnumerator()
    {
        // For IndexedDB, we need to execute asynchronously
        // This synchronous method will throw to force async usage
        throw new NotSupportedException("Use ToArrayAsync or ToListAsync for IndexedDB queries.");
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region CRUD Operations

    public async Task AddAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _interop.AddAsync(_databaseName, _storeName, entity);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        var entityArray = entities?.ToArray() ?? throw new ArgumentNullException(nameof(entities));
        if (entityArray.Length == 0) return;
        
        // Add each entity individually for now
        foreach (var entity in entityArray)
        {
            await _interop.AddAsync(_databaseName, _storeName, entity);
        }
    }

    public async Task UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _interop.UpdateAsync(_databaseName, _storeName, entity);
    }

    public async Task DeleteAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        // Get the key value from the entity
        var keyValue = GetKeyValue(entity);
        await _interop.DeleteByKeyAsync(_databaseName, _storeName, keyValue);
    }

    public async Task DeleteAsync<TKey>(TKey key)
    {
        await _interop.DeleteByKeyAsync(_databaseName, _storeName, key!);
    }

    public async Task<TEntity?> FindAsync<TKey>(TKey key)
    {
        return await _interop.GetByKeyAsync<TEntity, TKey>(_databaseName, _storeName, key);
    }

    public async Task ClearAsync()
    {
        await _interop.ClearAsync(_databaseName, _storeName);
    }

    #endregion

    #region Query Execution

    public async Task<TEntity[]> ToArrayAsync()
    {
        var query = await BuildAndExecuteQueryAsync();
        return query;
    }

    public async Task<List<TEntity>> ToListAsync()
    {
        var array = await ToArrayAsync();
        return array.ToList();
    }

    public async Task<TEntity> FirstAsync()
    {
        var results = await ToArrayAsync();
        if (results.Length == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }
        return results[0];
    }

    public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).FirstAsync();
    }

    public async Task<TEntity?> FirstOrDefaultAsync()
    {
        var results = await ToArrayAsync();
        return results.Length > 0 ? results[0] : default;
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).FirstOrDefaultAsync();
    }

    public async Task<TEntity> SingleAsync()
    {
        var results = await ToArrayAsync();
        if (results.Length == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }
        if (results.Length > 1)
        {
            throw new InvalidOperationException("Sequence contains more than one element");
        }
        return results[0];
    }

    public async Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).SingleAsync();
    }

    public async Task<TEntity?> SingleOrDefaultAsync()
    {
        var results = await ToArrayAsync();
        if (results.Length > 1)
        {
            throw new InvalidOperationException("Sequence contains more than one element");
        }
        return results.Length == 1 ? results[0] : default;
    }

    public async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).SingleOrDefaultAsync();
    }

    public async Task<int> CountAsync()
    {
        // If we have a simple count query without filters, use the optimized count method
        if (_expression.NodeType == ExpressionType.Constant)
        {
            return await _interop.CountAsync(_databaseName, _storeName);
        }
        
        // Otherwise, execute the query and count results
        var results = await ToArrayAsync();
        return results.Length;
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).CountAsync();
    }

    public async Task<bool> AnyAsync()
    {
        var count = await CountAsync();
        return count > 0;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var filtered = this.Where(predicate);
        return await ((IndexedDbSet<TEntity>)filtered).AnyAsync();
    }

    #endregion

    #region Query Building

    private async Task<TEntity[]> BuildAndExecuteQueryAsync()
    {
        // Parse the expression tree to build the query
        var queryInfo = ParseExpression(_expression);
        
        // Build query options for the JavaScript side
        var queryOptions = BuildQueryOptions(queryInfo);
        
        // Execute the query
        var results = await _interop.ExecuteQueryAsync<TEntity>(_databaseName, _storeName, queryOptions);
        
        // Apply any remaining client-side operations that couldn't be translated
        IEnumerable<TEntity> result = results;
        
        foreach (var operation in queryInfo.Operations.Where(op => !op.IsServerSide))
        {
            result = operation.Apply(result);
        }
        
        return result.ToArray();
    }
    
    private object BuildQueryOptions(QueryInfo queryInfo)
    {
        var options = new Dictionary<string, object>();
        
        // Extract server-side operations
        var whereOps = queryInfo.Operations.OfType<WhereOperation>().ToList();
        var orderByOps = queryInfo.Operations.OfType<OrderByOperation>().ToList();
        var skipOp = queryInfo.Operations.OfType<SkipOperation>().FirstOrDefault();
        var takeOp = queryInfo.Operations.OfType<TakeOperation>().FirstOrDefault();
        
        // Build filter from Where operations
        if (whereOps.Any())
        {
            // For now, we'll mark complex where clauses as client-side
            // In a full implementation, we'd translate simple predicates to IndexedDB queries
            foreach (var where in whereOps)
            {
                where.IsServerSide = false;
            }
        }
        
        // Add skip/take for pagination
        // Only mark as server-side if there are no client-side operations before them
        bool hasClientSideOps = whereOps.Any() || orderByOps.Any();
        
        if (skipOp != null)
        {
            if (!hasClientSideOps)
            {
                options["skip"] = skipOp.Count;
                skipOp.IsServerSide = true;
            }
            else
            {
                skipOp.IsServerSide = false;
            }
        }
        
        if (takeOp != null)
        {
            if (!hasClientSideOps)
            {
                options["take"] = takeOp.Count;
                takeOp.IsServerSide = true;
            }
            else
            {
                takeOp.IsServerSide = false;
            }
        }
        
        // Add ordering
        if (orderByOps.Any())
        {
            // IndexedDB has limited ordering support, mark as client-side for now
            foreach (var orderBy in orderByOps)
            {
                orderBy.IsServerSide = false;
            }
        }
        
        return options;
    }

    private QueryInfo ParseExpression(Expression expression)
    {
        var queryInfo = new QueryInfo();
        var currentExpression = expression;
        
        // Walk up the expression tree to collect all operations
        while (currentExpression != null)
        {
            if (currentExpression is MethodCallExpression methodCall)
            {
                var operation = ParseMethodCall(methodCall);
                if (operation != null)
                {
                    queryInfo.Operations.Insert(0, operation); // Insert at beginning to maintain order
                }
                currentExpression = methodCall.Arguments[0];
            }
            else if (currentExpression is ConstantExpression)
            {
                // We've reached the root
                break;
            }
            else
            {
                currentExpression = null;
            }
        }
        
        return queryInfo;
    }

    private QueryOperation? ParseMethodCall(MethodCallExpression methodCall)
    {
        return methodCall.Method.Name switch
        {
            "Where" => new WhereOperation(methodCall.Arguments[1]),
            "OrderBy" => new OrderByOperation(methodCall.Arguments[1], false),
            "OrderByDescending" => new OrderByOperation(methodCall.Arguments[1], true),
            "ThenBy" => new ThenByOperation(methodCall.Arguments[1], false),
            "ThenByDescending" => new ThenByOperation(methodCall.Arguments[1], true),
            "Skip" => new SkipOperation(methodCall.Arguments[1]),
            "Take" => new TakeOperation(methodCall.Arguments[1]),
            "Select" => new SelectOperation(methodCall.Arguments[1]),
            _ => null
        };
    }

    private object? GetKeyValue(TEntity entity)
    {
        var type = typeof(TEntity);
        var keyProperty = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<IndexedDbKeyPathAttribute>() != null);
        
        if (keyProperty == null)
        {
            throw new InvalidOperationException($"No key property found on type {type.Name}");
        }
        
        return keyProperty.GetValue(entity);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        // Nothing to dispose at the set level
        await ValueTask.CompletedTask;
    }

    #region Query Classes

    private class QueryInfo
    {
        public List<QueryOperation> Operations { get; } = new();
    }

    private abstract class QueryOperation
    {
        public bool IsServerSide { get; set; }
        public abstract IEnumerable<TEntity> Apply(IEnumerable<TEntity> source);
    }

    private class WhereOperation : QueryOperation
    {
        private readonly Expression _predicate;

        public WhereOperation(Expression predicate)
        {
            _predicate = predicate;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            var lambda = (Expression<Func<TEntity, bool>>)StripQuotes(_predicate);
            var compiled = lambda.Compile();
            return source.Where(compiled);
        }
    }

    private class OrderByOperation : QueryOperation
    {
        private readonly Expression _keySelector;
        private readonly bool _descending;

        public OrderByOperation(Expression keySelector, bool descending)
        {
            _keySelector = keySelector;
            _descending = descending;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            var lambda = (LambdaExpression)StripQuotes(_keySelector);
            var keyType = lambda.ReturnType;
            var method = _descending ? "OrderByDescending" : "OrderBy";
            
            var orderByMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == method && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), keyType);
            
            return (IEnumerable<TEntity>)orderByMethod.Invoke(null, new object[] { source, lambda.Compile() })!;
        }
    }

    private class ThenByOperation : QueryOperation
    {
        private readonly Expression _keySelector;
        private readonly bool _descending;

        public ThenByOperation(Expression keySelector, bool descending)
        {
            _keySelector = keySelector;
            _descending = descending;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            if (source is not IOrderedEnumerable<TEntity> ordered)
            {
                throw new InvalidOperationException("ThenBy can only be used after OrderBy");
            }

            var lambda = (LambdaExpression)StripQuotes(_keySelector);
            var keyType = lambda.ReturnType;
            var method = _descending ? "ThenByDescending" : "ThenBy";
            
            var thenByMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == method && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), keyType);
            
            return (IEnumerable<TEntity>)thenByMethod.Invoke(null, new object[] { ordered, lambda.Compile() })!;
        }
    }

    private class SkipOperation : QueryOperation
    {
        private readonly int _count;

        public int Count => _count;

        public SkipOperation(Expression countExpression)
        {
            _count = (int)((ConstantExpression)countExpression).Value!;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            return source.Skip(_count);
        }
    }

    private class TakeOperation : QueryOperation
    {
        private readonly int _count;

        public int Count => _count;

        public TakeOperation(Expression countExpression)
        {
            _count = (int)((ConstantExpression)countExpression).Value!;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            return source.Take(_count);
        }
    }

    private class SelectOperation : QueryOperation
    {
        private readonly Expression _selector;

        public SelectOperation(Expression selector)
        {
            _selector = selector;
        }

        public override IEnumerable<TEntity> Apply(IEnumerable<TEntity> source)
        {
            // Note: This is a simplified implementation
            // Real Select would change the type, but we're keeping it simple for now
            throw new NotSupportedException("Select projection is not yet implemented");
        }
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }
        return expression;
    }

    #endregion
}