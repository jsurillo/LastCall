using System.Linq.Expressions;
using BrosCode.LastCall.Entity.DbContext;
using Microsoft.EntityFrameworkCore;

namespace BrosCode.LastCall.Entity.Repository;

public sealed class EfRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly LastCallDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public EfRepository(LastCallDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(entity => entity.Id == id, ct);

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(predicate, ct);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        => _dbSet.AnyAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? _dbSet.CountAsync(ct) : _dbSet.CountAsync(predicate, ct);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public IQueryable<TEntity> Query(bool includeDeleted = false)
        => includeDeleted ? _dbSet.IgnoreQueryFilters().AsQueryable() : _dbSet.AsQueryable();

    public Task AddAsync(TEntity entity, CancellationToken ct = default)
        => _dbSet.AddAsync(entity, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => _dbSet.AddRangeAsync(entities, ct);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities)
        => _dbSet.UpdateRange(entities);

    public void SoftDelete(TEntity entity)
    {
        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        entity.ModifiedDate = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void SoftDeleteRange(IEnumerable<TEntity> entities)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedDate = utcNow;
            entity.ModifiedDate = utcNow;
        }

        _dbSet.UpdateRange(entities);
    }

    public void HardDelete(TEntity entity)
        => _dbSet.Remove(entity);

    public void HardDeleteRange(IEnumerable<TEntity> entities)
        => _dbSet.RemoveRange(entities);
}
