using BrosCode.LastCall.Entity.Repository;

namespace BrosCode.LastCall.Entity.UnitOfWork;

/// <summary>
/// Coordinates repositories and transactional work for persistence operations.
/// Lives in the Entity layer and is consumed by the Business layer.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Provides a repository for the specified entity type.
    /// </summary>
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes an operation within a transaction and commits on success.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
