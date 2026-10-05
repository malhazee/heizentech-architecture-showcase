using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Heizentech.Showcase.Infrastructure.Services
{
    public interface ITenantProvider
    {
        Guid GetTenantId();
    }

    /// <summary>
    /// Thread-safe, high-performance Tenant Resolution Service.
    /// Resolves tenants hierarchically from:
    /// 1. JWT Claims (Identity-authenticated dashboard/admin users)
    /// 2. Explicit HTTP Headers (API clients, Microservices, AI Agents)
    /// 3. Subdomain extraction from HTTP Host (Public storefronts)
    /// </summary>
    public class CurrentTenantProvider : ITenantProvider
    {
        // Thread-safe high-speed cache mapping Subdomains -> (TenantId, Expiration)
        private static readonly ConcurrentDictionary<string, (Guid TenantId, DateTime ExpiresAt)> _subdomainCache = 
            new(StringComparer.OrdinalIgnoreCase);

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private Guid? _cachedTenantId;

        public CurrentTenantProvider(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public static void InvalidateSubdomain(string subdomain)
        {
            if (!string.IsNullOrWhiteSpace(subdomain))
            {
                var clean = subdomain.Trim().ToLower().Replace(" ", "-");
                _subdomainCache.TryRemove(clean, out _);
            }
        }

        public Guid GetTenantId()
        {
            // Return cached result within the same request lifecycle (Scoped performance)
            if (_cachedTenantId.HasValue) return _cachedTenantId.Value;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return Guid.Empty;

            // 1. Try resolving from JWT Claims (Admin/Dashboard Session)
            var tenantClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "TenantId");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var claimTenantId) && claimTenantId != Guid.Empty)
            {
                _cachedTenantId = claimTenantId;
                return claimTenantId;
            }

            // 2. Try Request Header X-Tenant-ID
            var tenantHeader = httpContext.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            if (Guid.TryParse(tenantHeader, out var headerTenantId) && headerTenantId != Guid.Empty)
            {
                _cachedTenantId = headerTenantId;
                return headerTenantId;
            }

            // 3. Try Request Header X-Tenant-Subdomain (e.g. X-Tenant-Subdomain: md)
            var subdomainHeader = httpContext.Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(subdomainHeader))
            {
                var resolvedId = ResolveBySubdomain(subdomainHeader.Trim().ToLower());
                if (resolvedId != Guid.Empty)
                {
                    _cachedTenantId = resolvedId;
                    return resolvedId;
                }
            }

            // 4. Fallback: Parse Host Header (e.g., md.heizentech.com -> 'md')
            var host = httpContext.Request.Host.Host;
            var parts = host.Split('.');
            if (parts.Length >= 3 && !parts[0].Equals("api", StringComparison.OrdinalIgnoreCase) && !parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                var resolvedId = ResolveBySubdomain(parts[0].ToLower());
                if (resolvedId != Guid.Empty)
                {
                    _cachedTenantId = resolvedId;
                    return resolvedId;
                }
            }

            return Guid.Empty;
        }

        private Guid ResolveBySubdomain(string subdomain)
        {
            var now = DateTime.UtcNow;
            if (_subdomainCache.TryGetValue(subdomain, out var entry) && entry.ExpiresAt > now)
            {
                return entry.TenantId;
            }

            // Query Tenant repository using isolated service scope to prevent circular dependencies
            // Cache resolution for 10 minutes to minimize DB roundtrips
            // ... [Database Resolution Snippet] ...
            
            return Guid.Empty;
        }
    }
}
