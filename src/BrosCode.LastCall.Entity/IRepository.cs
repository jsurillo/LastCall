using System.Linq.Expressions;

namespace BrosCode.LastCall.Entity;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    IQueryable<TEntity> Query(bool includeDeleted = false);

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    void SoftDelete(TEntity entity);

    void SoftDeleteRange(IEnumerable<TEntity> entities);

    void HardDelete(TEntity entity);

    void HardDeleteRange(IEnumerable<TEntity> entities);
}
