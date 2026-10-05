# Heizentech Cloud Platform — Enterprise Multi-Tenant E-Commerce & Agentic Architecture

[![Next.js 16](https://img.shields.io/badge/Frontend-Next.js%2016%20%7C%20React%2019-black?logo=next.js)](https://nextjs.org/)
[![.NET 9 Web API](https://img.shields.io/badge/Backend-.NET%209%20Web%20API-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Multi-Tenant Architecture](https://img.shields.io/badge/Architecture-Database--Isolated%20Multi--Tenant-blue)](./ARCHITECTURE.md)
[![Docker & Traefik](https://img.shields.io/badge/DevOps-Docker%20%7C%20Traefik%20%7C%20Redis-2496ED?logo=docker)](./src/DevOps/)
[![AI-Agent Ready](https://img.shields.io/badge/AI--Agents-Tool%20Calling%20%26%20Function%20Calling-FF6F00?logo=openai)](./ARCHITECTURE.md#ai-agents--tool-calling-layer)
[![Live Demo](https://img.shields.io/badge/Production-Live%20Platform-success)](https://heizentech.com)

> **Architectural Blueprint:** This repository contains the public architecture specification, domain contracts, and core design patterns of the **Heizentech Cloud Platform**. The core enterprise business implementation is maintained within a proprietary repository, while this repository serves as a reference architecture for multi-tenant isolation, high-throughput caching, and autonomous AI-agent runtime integration.

---

## 🌐 Live Production References
- **Main Platform & SaaS Portal:** [https://heizentech.com](https://heizentech.com)
- **Live Tenant Storefront & PWA (Sample):** [https://md.heizentech.com](https://md.heizentech.com)
- **API Engine:** [https://api.heizentech.com](https://api.heizentech.com)

---

## 🎯 Platform Overview
**Heizentech** is an enterprise-grade, cloud-native **Multi-Tenant SaaS E-Commerce Operating System** designed with **.NET 9 Web API** and **Next.js 16 (App Router)**. 

The architecture provides dynamic store provisioning, subdomain routing, and automated data partitioning, while exposing an enterprise **Tool-Calling Interface for Autonomous AI Agents**.

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

## 🤖 Autonomous AI Agents & Tool-Calling Runtime

The platform was architected from inception to serve as an execution environment for AI agents (integrating with frameworks such as LangGraph, AutoGen, CrewAI, and Semantic Kernel):

1. **Deterministic Tool Calling (25+ Actionable APIs):**
   - Autonomous agents can programmatically inspect live inventory, adjust prices, configure promotional campaigns, and trigger notifications via OpenAPI schemas.
2. **Strict Multi-Tenant Isolation:**
   - EF Core Global Query Filters guarantee zero cross-tenant data leakage. Agents operating for Tenant A can never query or mutate Tenant B's data.
3. **Low-Latency Working Memory (Redis 7):**
   - High-throughput Redis caching serves as the sub-millisecond scratchpad for agent reasoning traces, conversation context, and session rate limiting.
4. **Proactive Event Triggers (Firebase Cloud Messaging):**
   - Supports proactive agent behaviors (e.g., an autonomous marketing agent detecting an abandoned cart or inventory restock and dispatching real-time Web Push alerts).

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
├── README.md                              <-- System Overview & Technical Highlights
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

---

## 📊 Core Architectural Capabilities & Specifications

| Capability | Architecture Implementation |
| :--- | :--- |
| **Agent Tool Calling** | Full RESTful OpenAPI surface ready for LLM Function Calling |
| **System Architecture** | Enterprise Multi-Tenant SaaS with Clean Architecture principles |
| **Low-Latency Backend** | .NET 9 asynchronous processing capable of thousands of RPS |
| **Cloud & DevOps** | Containerized micro-services with Traefik routing and health probes |
| **Full-Stack Integration** | Seamless communication between .NET 9 Web API and modern React 19/Next.js 16 |

---

## 📜 Intellectual Property & Contact
- **Architecture & System Design:** Mosab Alhazeem
- **Contact:** [mosabalhazeem30@gmail.com](mailto:mosabalhazeem30@gmail.com)
- **Live Platform:** [https://heizentech.com](https://heizentech.com)
