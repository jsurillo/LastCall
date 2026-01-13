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
