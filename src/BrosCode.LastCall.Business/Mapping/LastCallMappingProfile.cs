using AutoMapper;
using BrosCode.LastCall.Contracts;
using BrosCode.LastCall.Contracts.Dtos.App;
using BrosCode.LastCall.Entity;
using BrosCode.LastCall.Entity.Entities.App;

namespace BrosCode.LastCall.Business.Mapping;

public sealed class LastCallMappingProfile : Profile
{
    public LastCallMappingProfile()
    {
        RegisterEntityDtoMap<Drink, DrinkDto>();
    }

    private void RegisterEntityDtoMap<TEntity, TDto>()
        where TEntity : BaseEntity
        where TDto : BaseDto
    {
        CreateMap<TEntity, TDto>().ReverseMap();
    }
}
