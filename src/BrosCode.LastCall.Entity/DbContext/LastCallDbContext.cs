using System.Linq.Expressions;
using BrosCode.LastCall.Entity.Entities.App;
using BrosCode.LastCall.Entity.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrosCode.LastCall.Entity.DbContext;

public sealed class LastCallDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public LastCallDbContext(DbContextOptions<LastCallDbContext> options)
        : base(options)
    {
    }

    public DbSet<Drink> Drinks => Set<Drink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DbSchema.Default);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LastCallDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property(nameof(BaseEntity.CreatedBy))
                .HasMaxLength(200);

            builder.Property(nameof(BaseEntity.ModifiedBy))
                .HasMaxLength(200);

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsRowVersion();

            modelBuilder
                .Entity(entityType.ClrType)
                .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = utcNow;
                entry.Entity.ModifiedDate = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is { IsDeleted: true, DeletedDate: null })
                {
                    entry.Entity.DeletedDate = utcNow;
                }

                entry.Entity.ModifiedDate = utcNow;
            }
        }
    }

    private static LambdaExpression BuildSoftDeleteFilter(Type entityClrType)
    {
        var parameter = Expression.Parameter(entityClrType, "entity");
        var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
        return Expression.Lambda(compareExpression, parameter);
    }
}
