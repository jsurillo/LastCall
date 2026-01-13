using BrosCode.LastCall.Entity.Entities.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrosCode.LastCall.Entity.Configurations.App;

public sealed class DrinkConfiguration : IEntityTypeConfiguration<Drink>
{
    public void Configure(EntityTypeBuilder<Drink> builder)
    {
        builder.Property(drink => drink.Name)
            .IsRequired()
            .HasMaxLength(MaxLengths.Name);
    }
}
