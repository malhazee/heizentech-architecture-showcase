using System;

namespace Heizentech.Showcase.Domain.Common
{
    /// <summary>
    /// Base entity providing tenant-isolation, universal unique identification,
    /// and audit timestamps across all multi-tenant domain models.
    /// </summary>
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The Tenant ID used by EF Core Global Query Filters to guarantee zero data leakage.
        /// </summary>
        public Guid TenantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
