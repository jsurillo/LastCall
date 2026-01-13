using AutoMapper;
using BrosCode.LastCall.Business.Services.Base;
using BrosCode.LastCall.Contracts.Dtos.App;
using BrosCode.LastCall.Entity.Entities.App;
using BrosCode.LastCall.Entity.UnitOfWork;

namespace BrosCode.LastCall.Business.Services.App;

public sealed class DrinkService : GenericEntityService<DrinkDto, Drink>
{
    public DrinkService(IUnitOfWork unitOfWork, IMapper mapper)
        : base(unitOfWork, mapper)
    {
    }

    // Example: transactional workflow with error handling (Business layer)
    // try
    // {
    //     await _unitOfWork.ExecuteInTransactionAsync(async ct =>
    //     {
    //         // Multiple repository calls here...
    //         // await _unitOfWork.Repository<Drink>().AddAsync(entity, ct);
    //         // await _unitOfWork.Repository<Drink>().UpdateAsync(other, ct);
    //         // await _unitOfWork.Repository<Drink>().SoftDeleteAsync(third, ct);
    //
    //         // Prefer a single SaveChanges at the end (UnitOfWork helper handles it).
    //     }, ct);
    // }
    // catch (Exception ex)
    // {
    //     // You can see the error here (log/inspect/translate).
    //     // Rollback already happened inside UnitOfWork.
    //     // Example: _logger.LogError(ex, "Drink transactional workflow failed");
    //     // Do NOT catch inside UnitOfWork except to rollback; always rethrow.
    //     throw; // preserve stack trace
    // }
    public Task<IReadOnlyList<DrinkDto>> SearchByNameAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return ListAsync(ct);
        }

        var trimmedTerm = term.Trim();
        return ListAsync(drink => drink.Name.Contains(trimmedTerm), ct);
    }
}
