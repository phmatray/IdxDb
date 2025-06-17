using System.Collections;
using System.Linq.Expressions;

namespace IdxDb;

/// <summary>
/// Custom LINQ query provider for IndexedDB.
/// </summary>
internal class IndexedDbQueryProvider : IQueryProvider
{
    private readonly object _source;

    public IndexedDbQueryProvider(object source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = GetElementType(expression.Type);
        
        try
        {
            // Check if we're dealing with an OrderBy/OrderByDescending expression
            if (expression is MethodCallExpression methodCall && 
                (methodCall.Method.Name == "OrderBy" || methodCall.Method.Name == "OrderByDescending" ||
                 methodCall.Method.Name == "ThenBy" || methodCall.Method.Name == "ThenByDescending"))
            {
                var queryableType = typeof(IndexedDbSet<>).MakeGenericType(elementType);
                var constructor = queryableType.GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { queryableType, typeof(Expression) },
                    null);
                
                if (constructor == null)
                {
                    throw new InvalidOperationException($"Could not find constructor for {queryableType}");
                }
                
                var newSet = constructor.Invoke(new[] { _source, expression });
                
                // Wrap it in an ordered set
                var orderedSetType = typeof(IndexedDbOrderedSet<>).MakeGenericType(elementType);
                var orderedSetConstructor = orderedSetType.GetConstructor(new[] { queryableType });
                
                if (orderedSetConstructor == null)
                {
                    throw new InvalidOperationException($"Could not find ordered set constructor");
                }

                return (IQueryable)orderedSetConstructor.Invoke(new[] { newSet });
            }
            
            // Default behavior
            var defaultQueryableType = typeof(IndexedDbSet<>).MakeGenericType(elementType);
            var defaultConstructor = defaultQueryableType.GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { defaultQueryableType, typeof(Expression) },
                null);
            
            if (defaultConstructor == null)
            {
                throw new InvalidOperationException($"Could not find constructor for {defaultQueryableType}");
            }
            
            return (IQueryable)defaultConstructor.Invoke(new[] { _source, expression });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating query for type {elementType}", ex);
        }
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var sourceType = _source.GetType();
        
        // Check if we're dealing with an OrderBy/OrderByDescending expression
        if (expression is MethodCallExpression methodCall && 
            (methodCall.Method.Name == "OrderBy" || methodCall.Method.Name == "OrderByDescending" ||
             methodCall.Method.Name == "ThenBy" || methodCall.Method.Name == "ThenByDescending"))
        {
            // For ordering operations, we need to return an IOrderedQueryable
            if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(IndexedDbSet<>))
            {
                var elementType = sourceType.GetGenericArguments()[0];
                if (elementType != typeof(TElement))
                {
                    throw new InvalidOperationException($"Element type mismatch: expected {elementType.Name}, got {typeof(TElement).Name}");
                }

                // Create a new IndexedDbSet with the expression
                var setConstructor = sourceType.GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { sourceType, typeof(Expression) },
                    null);

                if (setConstructor == null)
                {
                    throw new InvalidOperationException($"Could not find constructor");
                }

                var newSet = setConstructor.Invoke(new[] { _source, expression });
                
                // Wrap it in an ordered set
                var orderedSetType = typeof(IndexedDbOrderedSet<>).MakeGenericType(elementType);
                var orderedSetConstructor = orderedSetType.GetConstructor(new[] { sourceType });
                
                if (orderedSetConstructor == null)
                {
                    throw new InvalidOperationException($"Could not find ordered set constructor");
                }

                return (IQueryable<TElement>)orderedSetConstructor.Invoke(new[] { newSet });
            }
        }
        
        // Check if we're dealing with a Select expression that changes the type
        if (expression is MethodCallExpression selectCall && selectCall.Method.Name == "Select")
        {
            // For now, throw a more helpful error message
            throw new NotSupportedException("Select projection that changes the element type is not yet supported. Use ToArrayAsync() first, then apply Select in memory.");
        }
        
        // Default behavior for other operations
        if (!sourceType.IsGenericType || sourceType.GetGenericTypeDefinition() != typeof(IndexedDbSet<>))
        {
            throw new InvalidOperationException($"Source is not IndexedDbSet");
        }

        var sourceElementType = sourceType.GetGenericArguments()[0];
        if (sourceElementType != typeof(TElement))
        {
            throw new InvalidOperationException($"Element type mismatch: expected {sourceElementType.Name}, got {typeof(TElement).Name}");
        }

        var constructor = sourceType.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { sourceType, typeof(Expression) },
            null);

        if (constructor == null)
        {
            throw new InvalidOperationException($"Could not find constructor");
        }

        return (IQueryable<TElement>)constructor.Invoke(new[] { _source, expression });
    }

    public object? Execute(Expression expression)
    {
        throw new NotSupportedException("Synchronous execution is not supported. Use async methods like ToArrayAsync().");
    }

    public TResult Execute<TResult>(Expression expression)
    {
        throw new NotSupportedException("Synchronous execution is not supported. Use async methods like ToArrayAsync().");
    }

    private static Type GetElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            return type.GetGenericArguments()[0];
        }

        var ienum = type.GetInterface("IEnumerable`1");
        if (ienum != null)
        {
            return ienum.GetGenericArguments()[0];
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return typeof(object);
        }

        throw new ArgumentException("Could not determine element type", nameof(type));
    }
}