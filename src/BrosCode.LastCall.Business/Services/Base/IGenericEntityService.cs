using System.Linq.Expressions;
using BrosCode.LastCall.Contracts;
using BrosCode.LastCall.Entity;

namespace BrosCode.LastCall.Business.Services.Base;

/// <summary>
/// Provides generic CRUD and query operations for DTO-based resources.
/// Lives in the Business layer and is consumed by higher layers like the API.
/// </summary>
public interface IGenericEntityService<TDto, TEntity>
    where TEntity : BaseEntity
    where TDto : BaseDto
{
    /// <summary>
    /// Retrieves all resources as DTOs.
    /// </summary>
    Task<IReadOnlyList<TDto>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves resources matching the predicate as DTOs.
    /// </summary>
    Task<IReadOnlyList<TDto>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a resource by its unique identifier.
    /// </summary>
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new resource from the provided DTO.
    /// </summary>
    Task<TDto> AddAsync(TDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing resource with values from the provided DTO.
    /// </summary>
    Task UpdateAsync(Guid id, TDto dto, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a resource by its unique identifier.
    /// </summary>
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a resource by its unique identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Persists any pending changes for the current unit of work.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
