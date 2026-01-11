using System.Linq.Expressions;

namespace BrosCode.LastCall.Entity;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be zero or greater.");
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }

        return query.Skip(skip).Take(take);
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sortBy,
        string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        Expression propertyAccess;

        try
        {
            propertyAccess = Expression.PropertyOrField(parameter, sortBy);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"SortBy '{sortBy}' does not match a public property on {typeof(T).Name}.", nameof(sortBy), ex);
        }

        var orderByLambda = Expression.Lambda(propertyAccess, parameter);
        var methodName = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), propertyAccess.Type },
            query.Expression,
            Expression.Quote(orderByLambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}
