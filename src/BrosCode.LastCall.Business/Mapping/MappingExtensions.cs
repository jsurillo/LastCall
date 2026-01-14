using AutoMapper;
using BrosCode.LastCall.Contracts;
using BrosCode.LastCall.Entity;

namespace BrosCode.LastCall.Business.Mapping;

public static class MappingExtensions
{
    public static IMappingExpression<TDto, TEntity> IgnoreAuditMembers<TDto, TEntity>(
        this IMappingExpression<TDto, TEntity> mapping)
        where TEntity : BaseEntity
        where TDto : BaseDto
    {
        return mapping
            .ForMember(dest => dest.CreatedDate, options => options.Ignore())
            .ForMember(dest => dest.CreatedBy, options => options.Ignore())
            .ForMember(dest => dest.ModifiedDate, options => options.Ignore())
            .ForMember(dest => dest.ModifiedBy, options => options.Ignore())
            .ForMember(dest => dest.IsDeleted, options => options.Ignore())
            .ForMember(dest => dest.DeletedDate, options => options.Ignore())
            .ForMember(dest => dest.RowVersion, options => options.Ignore());
    }
}
