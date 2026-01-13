using BrosCode.LastCall.Entity.Repository;

namespace BrosCode.LastCall.Entity.UnitOfWork;

public interface IUnitOfWork
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
