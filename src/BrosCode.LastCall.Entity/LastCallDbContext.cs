using Microsoft.EntityFrameworkCore;

namespace BrosCode.LastCall.Entity;

public sealed class LastCallDbContext : DbContext
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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsRowVersion();
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
                entry.Entity.ModifiedDate = utcNow;
            }
        }
    }
}
