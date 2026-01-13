using BrosCode.LastCall.Entity.DbContext;
using BrosCode.LastCall.Entity.Repository;
using Microsoft.EntityFrameworkCore;

namespace BrosCode.LastCall.Entity.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LastCallDbContext _dbContext;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(LastCallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        if (_repositories.TryGetValue(type, out var repo))
        {
            return (IRepository<TEntity>)repo;
        }

        var repository = new EfRepository<TEntity>(_dbContext);
        _repositories[type] = repository;
        return repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                await operation(ct);
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(ct);
                }
                catch
                {
                    // Ignore rollback errors to preserve the original exception.
                }

                throw;
            }
        });
    }
}
