# System Architecture & Technical Specification

This document provides a detailed breakdown of the architectural patterns, security controls, and AI-agent integration points built into the **Heizentech SaaS Platform**.

---

## 1. Domain-Driven & Clean Architecture Model

The backend is built upon Clean Architecture principles, ensuring that domain logic remains decoupled from persistence, third-party SDKs, and transport protocols.

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Tier                        │
│         (ASP.NET Core Controllers, Swagger, Middlewares)    │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│                    Application Tier                         │
│           (DTOs, CQRS Interfaces, Services, Validations)    │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│                      Domain Tier                            │
│     (BaseEntity, Catalog, Orders, Marketing, Notifications) │
└──────────────────────────────▲──────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────┐
│                   Infrastructure Tier                       │
│    (ApplicationDbContext, Identity, Redis Cache, FCM)      │
└─────────────────────────────────────────────────────────────┘
```

### Domain Core Abstraction: `BaseEntity`
Every tenant-scoped entity inherits from `BaseEntity`, ensuring standard auditability and tenant ownership:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

---

## 2. Multi-Tenancy Data Isolation Pattern

Multi-tenancy in Heizentech is implemented using a **Shared Database, Isolated Schema / Discriminator** approach with automated query rewriting.

### The Resolution Pipeline:
1. **Host Header Analysis:** The incoming request URL (e.g. `md.heizentech.com`) is intercepted by `CurrentTenantProvider`.
2. **Subdomain Lookup:** The subdomain `md` is looked up in an in-memory thread-safe `ConcurrentDictionary` with TTL.
3. **Database Fallback:** If not cached, the Tenant record is queried and cached.
4. **Context Injection:** `ITenantProvider` exposes `GetTenantId()` to the scoped DbContext.
5. **Global Query Filter:**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);

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
   ```

---

## 3. AI Agents & Tool-Calling Layer

The platform is designed to serve as an execution backend for **Autonomous AI Agents** (built with LangGraph, AutoGen, CrewAI, or Semantic Kernel).

### Agent Execution Loop:
```
[User Request / Autonomous Goal]
           │
           ▼
[LLM Agent Core (e.g. Claude / GPT-4 / Gemini)]
           │
           ├── Decide next action (Reasoning Trace)
           │
           ▼
[Tool Calling (Function Calling)]
   ├── Tool 1: check_inventory(productId)
   ├── Tool 2: adjust_product_price(productId, newPrice)
   ├── Tool 3: create_flash_sale_package(title, discountPercentage)
   └── Tool 4: send_broadcast_push(title, body)
           │
           ▼
[Heizentech .NET 9 REST Engine] ──(Executes against DB/Redis/FCM)
           │
           ▼
[Observed State / Feedback returned to Agent]
```

### Key Tool Definitions Ready for Agents:
- **`GET /api/products`**: Query store catalog with semantic filters, stock levels, and price ranges.
- **`POST /api/marketing/promos`**: Programmatically create custom promo codes for targeted customer cohorts.
- **`POST /api/notifications/broadcast`**: Dispatch instant Web Push Notifications to all active subscribers.
- **`GET /api/customers`**: Retrieve real-time registered customer metrics and engagement stats.

---

## 4. Redis High-Speed Caching & Agent Working Memory

To achieve sub-millisecond response times for agent operations and high-traffic customer visits, Redis 7 is integrated as a distributed cache:
- **Tenant Configuration Cache:** Store branding, theme colors, payment credentials.
- **Agent Scratchpad / Session Memory:** Temporary conversation states and reasoning trees.
- **Cache Invalidation:** Smart invalidation triggers whenever an admin or agent updates catalog entities.

---

## 5. Production Topology & Deployment Pipeline

The production architecture is fully containerized and runs on a multi-stage Docker setup managed through **Coolify** and **Traefik**:

```
                       [ Internet Traffic / Users / Agents ]
                                         │
                                         ▼
                             [ Cloudflare Edge CDN & SSL ]
                                         │
                                         ▼
                            [ Traefik Reverse Proxy ]
                                         │
             ┌───────────────────────────┴───────────────────────────┐
             │ Port: 3000                                            │ Port: 5100
             ▼                                                       ▼
   [ Next.js 16 Web SSR ]                                  [ .NET 9 API Engine ]
   - App Router (Turbopack)                                - Asynchronous Web API
   - React 19 Client UI                                    - EF Core & Identity
             │                                                       │
             └───────────────────────────┬───────────────────────────┘
                                         │
             ┌───────────────────────────┴───────────────────────────┐
             │ Internal Bridge                                       │
             ▼                                                       ▼
   [ Redis 7 In-Memory ]                                   [ SQL Server 2022 ]
   - Session & Agent Cache                                 - Persistent Storage
```
