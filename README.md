# Heizentech Cloud Platform — Enterprise Multi-Tenant E-Commerce & Agentic Architecture

[![Next.js 16](https://img.shields.io/badge/Frontend-Next.js%2016%20%7C%20React%2019-black?logo=next.js)](https://nextjs.org/)
[![.NET 9 Web API](https://img.shields.io/badge/Backend-.NET%209%20Web%20API-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Multi-Tenant Architecture](https://img.shields.io/badge/Architecture-Database--Isolated%20Multi--Tenant-blue)](./ARCHITECTURE.md)
[![Docker & Traefik](https://img.shields.io/badge/DevOps-Docker%20%7C%20Traefik%20%7C%20Redis-2496ED?logo=docker)](./src/DevOps/)
[![AI-Agent Ready](https://img.shields.io/badge/AI--Agents-Tool%20Calling%20%26%20Function%20Calling-FF6F00?logo=openai)](./ARCHITECTURE.md#ai-agents--tool-calling-layer)
[![Live Demo](https://img.shields.io/badge/Production-Live%20Platform-success)](https://heizentech.com)

> **Notice:** This repository is an **Architectural Showcase & Engineering Blueprint** representing the production design of the **Heizentech SaaS Platform**. The underlying business repository is maintained as private intellectual property. This showcase is compiled specifically for technical evaluation and the **AI Agents Hackathon Committee**.

---

## 🌐 Live Production References
- **Main Platform & SaaS Portal:** [https://heizentech.com](https://heizentech.com)
- **Live Tenant Storefront & PWA (Sample):** [https://md.heizentech.com](https://md.heizentech.com)
- **API Engine:** [https://api.heizentech.com](https://api.heizentech.com)

---

## 🎯 Executive Overview
**Heizentech** is a high-performance, cloud-native **Multi-Tenant SaaS E-Commerce Platform** built from the ground up with **.NET 9 Web API** and **Next.js 16 (App Router)**. 

The platform is purpose-built to provide isolated store environments (with custom subdomains, catalogs, order management, and PWA push notifications) while acting as an **execution environment for Autonomous AI Agents**.

```
                           ┌──────────────────────────────────────────────┐
                           │            Autonomous AI Agents              │
                           │  (Sales Agent, Inventory Agent, Marketer)    │
                           └──────────────────────┬───────────────────────┘
                                                  │ (Tool / Function Calling)
                                                  ▼
┌─────────────────────────┐          ┌──────────────────────────┐          ┌─────────────────────────┐
│       Storefront        │          │   Heizentech .NET 9 API  │          │   Super-Admin Console   │
│  (Next.js 16 / PWA)     │ ◄──────► │ (Multi-Tenant Engine)    │ ◄──────► │ (Tenant & Subscriptions)│
└─────────────────────────┘          └────────────┬─────────────┘          └─────────────────────────┘
                                                  │
                 ┌────────────────────────────────┼───────────────────────────────┐
                 ▼                                ▼                               ▼
       ┌──────────────────┐             ┌──────────────────┐            ┌──────────────────┐
       │ SQL Server 2022  │             │   Redis Cache    │            │   Firebase FCM   │
       │ (Isolated Data)  │             │  (Agent Memory)  │            │ (Real-time Push) │
       └──────────────────┘             └──────────────────┘            └──────────────────┘
```

---

## 🤖 Why this Architecture Wins in an AI Agents Hackathon

In AI agent competitions, **models cannot deliver value in a vacuum**. An autonomous agent requires an enterprise-grade environment equipped with tools, state persistence, and real-time execution capabilities:

1. **Rich Tool-Calling Interface (25+ Actionable APIs):**
   - Agents can programmatically inspect inventory, update pricing, generate promo codes, process orders, and dispatch push notifications via standard OpenAPI specs.
2. **Deterministic Data Isolation (Multi-Tenancy):**
   - EF Core Global Query Filters ensure that an AI Agent operating on behalf of Tenant A can *never* leak or access data from Tenant B.
3. **Agent Short-Term & Working Memory (Redis 7):**
   - High-throughput Redis caching serves as the sub-millisecond scratchpad for agent reasoning traces, conversation context, and rate limiting.
4. **Proactive Event Triggers (Firebase Cloud Messaging):**
   - Enables proactive agent behavior (e.g., an autonomous marketing agent detecting an abandoned cart or price drop and dispatching real-time Web Push alerts).

---

## 🏗️ High-Level Technical Stack

| Tier | Technologies & Protocols | Role in System |
| :--- | :--- | :--- |
| **Backend Engine** | **.NET 9, C#, ASP.NET Core Web API** | High-throughput, low-latency multi-tenant core services |
| **Data Layer** | **SQL Server 2022, EF Core 9, LINQ** | Relational integrity with Global Query Filter isolation |
| **Caching & Memory** | **Redis 7 (Alpine)** | Distributed caching, session store & agent working memory |
| **Frontend Web** | **Next.js 16.3, React 19, TypeScript** | Turbopack-optimized SSR/SSG/ISR with App Router |
| **Mobile & PWA** | **Dynamic Web Manifest, Web Push (FCM)** | Installable web application with foreground/background push |
| **Security & Auth** | **JWT Bearer, ASP.NET Core Identity, RBAC** | Tenant-scoped claims authentication & granular permissions |
| **Infrastructure** | **Docker, Docker Compose, Traefik, Coolify** | Automated continuous deployment and container orchestration |
| **Edge & CDN** | **Cloudflare SSL / Enterprise Edge** | Edge caching, DDoS mitigation, and SSL termination |

---

## 📂 Repository Architecture & Key Code Samples

This repository contains sanitized architectural blueprints in [`/src`](./src):

```text
showcase-repo/
├── README.md                              <-- Project Overview & Hackathon Positioning
├── ARCHITECTURE.md                        <-- In-depth Architectural Design Specification
└── src/
    ├── Backend/
    │   ├── CurrentTenantProvider.cs       <-- Subdomain/Header/JWT Tenant Resolution Engine
    │   ├── ApplicationDbContext_Filter.cs <-- EF Core Multi-Tenant Global Query Filters
    │   └── BaseEntity.cs                  <-- Clean Architecture Core Domain Model
    ├── Frontend/
    │   ├── StoreSettingsContext.tsx       <-- Dynamic Tenant Hydration & SSR-safe State
    │   └── api-client.ts                  <-- Resilient API Client with Multi-tenant Headers
    └── DevOps/
        └── docker-compose.architecture.yml <-- Multi-container Production Topology
```

---

## 🔑 Core Architectural Highlights

### 1. Zero-Leak Multi-Tenant Resolution ([`CurrentTenantProvider.cs`](./src/Backend/CurrentTenantProvider.cs))
Tenants are dynamically resolved at request time from three hierarchical sources:
- **JWT Claims** (for authenticated admin / dashboard sessions).
- **Custom HTTP Headers** (`X-Tenant-Subdomain` / `X-Tenant-ID`).
- **HTTP Host / Subdomain Parsing** (e.g., `md.heizentech.com` → resolves tenant `md` with in-memory `ConcurrentDictionary` caching).

### 2. Automatic Tenant Data Isolation ([`ApplicationDbContext_Filter.cs`](./src/Backend/ApplicationDbContext_Filter.cs))
```csharp
// All entities deriving from BaseEntity are automatically filtered at the EF Core level
modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantProvider.GetTenantId());
```
Developers cannot accidentally query another store's data—isolation is enforced transparently by the ORM engine.

### 3. Production PWA & Dynamic Multi-Tenant Manifests
The frontend dynamically serves tailored Web Manifests (`/manifest.webmanifest`) based on the active tenant's branding, color palette, and icons, enabling custom app installation for every single merchant.

## 📜 Intellectual Property & Contact
- **Architecture & System Design:** Mosabalhazeem
- **Contact:** [taqialhazeem@gmail.com](mailto:mosabalhazeem30@gmail.com)
- **Live Platform:** [https://heizentech.com](https://heizentech.com)
