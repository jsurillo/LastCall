using System.Linq.Expressions;
using BrosCode.LastCall.Entity.Entities.App;
using BrosCode.LastCall.Entity.Identity;
using BrosCode.LastCall.Entity.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrosCode.LastCall.Entity.DbContext;

public sealed class LastCallDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private const string SystemUser = "system";
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public LastCallDbContext(DbContextOptions<LastCallDbContext> options)
        : base(options)
    {
        _currentUserAccessor = new NullCurrentUserAccessor();
    }

    public LastCallDbContext(
        DbContextOptions<LastCallDbContext> options,
        ICurrentUserAccessor currentUserAccessor)
        : base(options)
    {
        _currentUserAccessor = currentUserAccessor;
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
        var currentUser = GetCurrentUser();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = utcNow;
                entry.Entity.ModifiedDate = utcNow;
                entry.Entity.CreatedBy = currentUser;
                entry.Entity.ModifiedBy = currentUser;
                entry.Entity.DeletedDate = entry.Entity.IsDeleted ? utcNow : null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(BaseEntity.CreatedDate)).IsModified = false;
                entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                entry.Property(nameof(BaseEntity.RowVersion)).IsModified = false;
                entry.Property(nameof(BaseEntity.ModifiedDate)).IsModified = false;
                entry.Property(nameof(BaseEntity.ModifiedBy)).IsModified = false;

                entry.Entity.ModifiedDate = utcNow;
                entry.Entity.ModifiedBy = currentUser;

                if (entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedDate ??= utcNow;
                    entry.Property(nameof(BaseEntity.DeletedDate)).IsModified = true;
                }
                else
                {
                    entry.Property(nameof(BaseEntity.DeletedDate)).IsModified = false;
                }
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

    private string GetCurrentUser()
    {
        var user = _currentUserAccessor.UserNameOrId;
        return string.IsNullOrWhiteSpace(user) ? SystemUser : user;
    }

    private sealed class NullCurrentUserAccessor : ICurrentUserAccessor
    {
        public string? UserNameOrId => null;
    }
}
