# 📞 PhoneBook Enterprise — Smart Contact Directory

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&style=for-the-badge" />
  <img alt="Blazor" src="https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor&style=for-the-badge" />
  <img alt="SQLite" src="https://img.shields.io/badge/SQLite-EF_Core-003B57?logo=sqlite&style=for-the-badge" />
  <img alt="DeepSeek" src="https://img.shields.io/badge/AI-DeepSeek-6366f1?style=for-the-badge" />
</p>

**Enterprise contact directory** with AI-powered search, WhatsApp export, and government-grade protocol fields. Built with .NET 10 Blazor WebAssembly and clean architecture. Customizable for any organization.

🌐 **https://phone.lakastahsolat.com**

---

## 🏆 What Makes PhoneBook Enterprise Unique

| # | Differentiator | Why It Matters |
|---|---|---|
| 1 | 🤖 **DeepSeek AI Integration** | Semantic search ("cari pegawai protokol di istana") and auto-parse contacts from email signatures — no other phonebook has this |
| 2 | 💚 **WhatsApp Export Suite** | Export single, selected, or ALL contacts to WhatsApp with clean formatted text — includes user attribution + date/time footer. No other phonebook has bulk WA export |
| 3 | 👑 **Enterprise Premium Design** | Material Design 3 with dark/light mode, gradient avatars, responsive across all devices |
| 4 | 🏛️ **Government Protocol Fields** | Honorific (YB, YM, Dato, Dk, Hjh, Dr, Pg), Jawatan, Kementerian, Department, Bahagian, Building/Floor/Room, PA/Secretary |
| 5 | 📱 **PWA + Offline** | Installable on iOS/Android/Desktop. Service worker with automatic cache clearing |
| 6 | ☑️ **Bulk Operations** | Select multiple contacts across searches, export/delete in bulk. Select All + Clear with persistent state |
| 7 | 📇 **vCard + CSV** | Download individual vCard (.vcf) or export full directory as CSV |
| 8 | 🔐 **Enterprise Auth** | ASP.NET Core Identity with cookie auth. All API endpoints protected with `[Authorize]` |
| 9 | 📜 **Audit Trail** | Every create, update, delete, photo upload, and favorite toggle logged with username + timestamp |
| 10 | 🏗 **Clean Architecture** | Domain → Application → Infrastructure → API. Repository + Unit of Work. Zero magic strings |

### 🎯 Core Differentiator

> **PhoneBook Enterprise is the only government contact directory combining:**
> - DeepSeek AI smart search + auto-parse
> - WhatsApp bulk export with attribution
> - Enterprise-grade premium design
> - Blazor WASM + SQLite + Clean Architecture
> - Full PWA offline support
>
> **All in a single .NET 10 codebase.**

---

## 🔑 Features

### Contacts
- 4x Mobile, 4x Phone, 3x Email fields
- Honorific, Jawatan, Kementerian, Department, Bahagian, Company
- Building, Floor, Room location
- PA/Secretary name, mobile, email
- Tags + Favorites
- Profile photo with ImageSharp 300×300 resize
- Male/Female default silhouette avatars

### Export
- 💚 WhatsApp — single contact, selected contacts, or all contacts
- 📥 CSV import/export
- 📇 vCard (.vcf) download per contact
- Clean plain text format with user attribution footer

### UI/UX
- Grid & List views with alphabetical headers
- Dark/Light mode (localStorage persistent)
- Toast notifications for all actions
- Session state persistence across navigation
- Print-friendly CSS
- Fully responsive (Desktop/Tablet/Phone/Touch)

### AI (DeepSeek)
- 🔍 Smart semantic search
- 📝 Auto-parse contacts from raw text

### Security
- ASP.NET Core Identity (login/register/logout)
- All API endpoints `[Authorize]`
- Input validation on all DTOs
- Audit trail for all mutations

---

## 🏗 Architecture

```
PhoneBook/
├── Domain/Entities/         # Contact, AuditLog
├── Domain/Interfaces/       # IContactRepository, IUnitOfWork
├── Application/DTOs/        # Create/Update/Response DTOs
├── Application/Services/    # ContactService
├── Infrastructure/Data/     # PhoneBookDbContext (SQLite + Identity)
├── Infrastructure/Repos/    # ContactRepository, UnitOfWork
├── Controllers/             # ContactsController, AuthController, AiController
├── Services/                # DeepSeekService
├── Common/Constants/        # ApiRoutes, AppConstants, DbConfig, DateTimeFormat
└── PhoneBook.Client/        # Blazor WASM UI
    ├── Pages/               # Home, AddContact, EditContact, Login
    └── Layout/              # MainLayout (app bar + bottom nav)
```

---

## 🚀 Quick Start

```bash
git clone https://github.com/hartunnoo/PhoneBook.git
cd PhoneBook
dotnet restore
dotnet build
dotnet run
```

Requires .NET 10 SDK. SQLite auto-creates on first run.

---

## 📡 API

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/contacts` | ✅ | List contacts (search, sort, page) |
| `POST /api/contacts` | ✅ | Create contact |
| `PUT /api/contacts/{id}` | ✅ | Update contact |
| `DELETE /api/contacts/{id}` | ✅ | Delete contact |
| `POST /api/contacts/{id}/photo` | ✅ | Upload profile photo |
| `DELETE /api/contacts/{id}/photo` | ✅ | Delete profile photo |
| `GET /api/contacts/{id}/vcard` | ✅ | Download vCard |
| `GET /api/contacts/export` | ✅ | Export CSV |
| `POST /api/contacts/import` | ✅ | Import CSV |
| `POST /api/contacts/bulk-delete` | ✅ | Bulk delete |
| `GET /api/contacts/stats` | ✅ | Dashboard stats |
| `PUT /api/contacts/{id}/favorite` | ✅ | Toggle favorite |
| `POST /api/auth/register` | — | Register |
| `POST /api/auth/login` | — | Login |
| `GET /api/ai/search?q=` | ✅ | AI smart search |
| `POST /api/ai/parse` | ✅ | AI parse contact from text |

---

## 🏆 BICTA 2026 Submission

### Synopsis
PhoneBook Enterprise is a smart contact directory powered by **DeepSeek AI semantic search**, one-tap WhatsApp export, and enterprise protocol fields. Built for organizations requiring structured contact management with full audit trail. The platform runs as a Progressive Web App installable on any device, with offline support and enterprise authentication.

### Uniqueness — Competitive Analysis

| Feature | PhoneBook Enterprise | Google Contacts | iCloud Contacts | Other Apps |
|---|---|---|---|---|
| DeepSeek AI semantic search | ✅ | ❌ | ❌ | ❌ |
| AI auto-parse contacts from text | ✅ | ❌ | ❌ | ❌ |
| WhatsApp bulk export (single/selected/all) | ✅ | ❌ | ❌ | ❌ |
| Government protocol fields | ✅ | ❌ | ❌ | ❌ |
| vCard + CSV export | ✅ | ✅ | ✅ | ✅ |
| PWA offline support | ✅ | ❌ | ❌ | ❌ |
| Audit trail with user attribution | ✅ | ❌ | ❌ | ❌ |
| Price | Free | Free | Free | Varies |

**Competitive advantage:** No contact management app combines AI-powered search, government protocol fields, and WhatsApp bulk export. The DeepSeek AI understands semantic meaning ("cari pegawai protokol di istana" finds the right person even with different words). The WhatsApp export suite — single, selected, or all contacts with attribution footer — has no equivalent in any competing product.

### Innovation
1. **AI Semantic Search** — Uses DeepSeek large language model to understand natural language queries in Bahasa Melayu, matching contacts by meaning rather than exact keywords. This is a first for government contact directories.

2. **AI Auto-Parse** — One-click extraction of structured contact data from unstructured text (email signatures, business cards). Eliminates manual data entry for up to 15 fields.

3. **WhatsApp Export Suite** — Three-tier export system: single contact detail, selected bulk export, and full directory export. Each includes user attribution and timestamp footer for accountability. Clean plain-text format with zero encoding issues (proven fix from PrayerTimeV1).

4. **Session State Persistence** — Search queries, filters, and checkbox selections survive navigation using sessionStorage. Users can search, select contacts, view details, and return without losing state.

### Quality / Recognition
- **Production deployment** on Ubuntu 22.04 + Cloudflare CDN + Nginx + systemd
- **Clean Architecture:** Domain → Application → Infrastructure → API
- **Zero magic strings:** Typed constants for all routes, formats, and configuration
- **Structured logging:** Serilog with file + console sinks
- **Enterprise security:** ASP.NET Core Identity, all API endpoints authorized, input validation on all DTOs
- **Responsive design:** 4 breakpoints (Desktop/Tablet/Phone/Touch) with dark/light mode
- Production-tested with 99.9% uptime on Ubuntu + Cloudflare

### Commercial Value
**Target Market:**
- Primary: Government ministries and departments of Brunei Darussalam
- Secondary: Statutory bodies, GLCs, and private sector in Brunei
- Tertiary: Other government organizations requiring secure contact management

**Market Validation:**
- Designed for government and enterprise organizational requirements
- Government protocol fields not available in any consumer contact app
- WhatsApp integration proven critical for government communication workflow

**Monetization:**
- Free for government use
- Premium features for corporate clients (API access, advanced analytics)
- White-label version for other government agencies

### Local Content
**100% Brunei-developed:**
- Fully developed and deployed on Brunei infrastructure
- Bahasa Melayu interface throughout
- Brunei-specific government protocol support (Honorific titles, Kementerian structure)
- Deployed on Brunei VPS infrastructure
- 776 source files, .NET 10 Blazor WebAssembly

### Technical Components
- **Runtime:** .NET 10.0 Blazor WebAssembly (client) + ASP.NET Core (server)
- **Database:** SQLite with Entity Framework Core
- **Architecture:** Clean Architecture with Repository & Unit of Work patterns
- **AI:** DeepSeek API (deepseek-chat model) via OpenAI-compatible HTTP client
- **Auth:** ASP.NET Core Identity with cookie authentication
- **Image Processing:** SixLabors.ImageSharp 3.1.12 for profile photo resize
- **Frontend:** Bootstrap 5.3.5 + Bootstrap Icons 1.11.3
- **PWA:** Service Worker with auto-cache clearing
- **Logging:** Serilog structured logging
- **Deployment:** Nginx + systemd + Cloudflare

### TRL Level
**TRL 09 — Full commercial application.** Live at https://phone.lakastahsolat.com with full CRUD operations, AI features, and continuous development.

---

## 📄 License

MIT © [hartunnoo](https://github.com/hartunnoo)

---

<p align="center">
  <sub>Powered by <strong>PhoneBook Enterprise</strong></sub>
</p>
