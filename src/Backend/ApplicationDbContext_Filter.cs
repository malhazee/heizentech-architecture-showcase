using System;
using System.Linq.Expressions;
using System.Reflection;
using Heizentech.Showcase.Domain.Common;
using Heizentech.Showcase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Heizentech.Showcase.Infrastructure.Persistence
{
    /// <summary>
    /// Demonstrates the automated EF Core Global Query Filter integration.
    /// Intercepts all queries across all BaseEntity derivatives to automatically append
    /// WHERE TenantId = @currentTenantId, making data leaks architecturally impossible.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatically scan and apply tenant isolation filters to all domain entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(ApplicationDbContext)
                        .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)?
                        .MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }
        }

        private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
        {
            // The query filter delegates dynamically to _tenantProvider at query execution time
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantProvider.GetTenantId());
        }

        public override int SaveChanges()
        {
            ApplyAuditAndTenantInfo();
            return base.SaveChanges();
        }

        private void ApplyAuditAndTenantInfo()
        {
            var tenantId = _tenantProvider.GetTenantId();
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = tenantId;
                    }
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
