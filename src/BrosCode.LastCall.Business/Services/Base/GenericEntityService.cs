using System.Linq.Expressions;
using AutoMapper;
using BrosCode.LastCall.Contracts;
using BrosCode.LastCall.Entity;
using BrosCode.LastCall.Entity.Repository;
using BrosCode.LastCall.Entity.UnitOfWork;
using MassTransit;

namespace BrosCode.LastCall.Business.Services.Base;

public class GenericEntityService<TDto, TEntity> : IGenericEntityService<TDto, TEntity>
    where TEntity : BaseEntity
    where TDto : BaseDto
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<TEntity> _repository;
    private readonly IMapper _mapper;

    public GenericEntityService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.Repository<TEntity>();
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TDto>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _repository.ListAsync(ct);
        return _mapper.Map<IReadOnlyList<TDto>>(entities);
    }

    public async Task<IReadOnlyList<TDto>> ListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var entities = await _repository.ListAsync(predicate, ct);
        return _mapper.Map<IReadOnlyList<TDto>>(entities);
    }

    public async Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : _mapper.Map<TDto>(entity);
    }

    public async Task<TDto> AddAsync(TDto dto, CancellationToken ct = default)
    {
        dto.Id = dto.Id == Guid.Empty ? NewId.NextGuid() : dto.Id;
        var entity = _mapper.Map<TEntity>(dto);
        await _repository.AddAsync(entity, ct);
        return _mapper.Map<TDto>(entity);
    }

    public async Task UpdateAsync(Guid id, TDto dto, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");
        _mapper.Map(dto, entity);
        entity.Id = id;
        _repository.Update(entity);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        _repository.SoftDelete(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");
        _repository.HardDelete(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _unitOfWork.SaveChangesAsync(ct);
}
