using System.Linq.Expressions;
using BrosCode.LastCall.Contracts;
using BrosCode.LastCall.Entity;

namespace BrosCode.LastCall.Business.Services.Base;

public interface IGenericEntityService<TDto, TEntity>
    where TEntity : BaseEntity
    where TDto : BaseDto
{
    Task<IReadOnlyList<TDto>> ListAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TDto>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TDto> AddAsync(TDto dto, CancellationToken ct = default);

    Task UpdateAsync(Guid id, TDto dto, CancellationToken ct = default);

    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
