# 📞 PhoneBook HMO — Direktori Kenalan Rasmi

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&style=for-the-badge" />
  <img alt="Blazor" src="https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor&style=for-the-badge" />
  <img alt="SQLite" src="https://img.shields.io/badge/SQLite-EF_Core-003B57?logo=sqlite&style=for-the-badge" />
  <img alt="DeepSeek" src="https://img.shields.io/badge/AI-DeepSeek-6366f1?style=for-the-badge" />
</p>

**Enterprise contact directory** for His Majesty's Office, Istana Nurul Iman. Built with .NET 10 Blazor WebAssembly, clean architecture, and DeepSeek AI.

🌐 **https://phone.lakastahsolat.com**

---

## 🏆 What Makes PhoneBook HMO Unique

| # | Differentiator | Why It Matters |
|---|---|---|
| 1 | 🤖 **DeepSeek AI Integration** | Semantic search ("cari pegawai protokol di istana") and auto-parse contacts from email signatures — no other phonebook has this |
| 2 | 💚 **WhatsApp Export Suite** | Export single, selected, or ALL contacts to WhatsApp with clean formatted text — includes user attribution + date/time footer |
| 3 | 👑 **HMO Premium Design** | Navy + gold branding inspired by Pesambah. Material Design 3 with dark/light mode, gradient avatars, responsive across all devices |
| 4 | 🏛️ **Government Protocol Fields** | Honorific (YB, YM, Dato, Dk, Hjh, Dr, Pg), Jawatan, Kementerian, Department, Bahagian, Building/Floor/Room, PA/Secretary |
| 5 | 📱 **PWA + Offline** | Installable on iOS/Android/Desktop. Service worker with automatic cache clearing |
| 6 | ☑️ **Bulk Operations** | Select multiple contacts across searches, export/delete in bulk. Select All + Clear with persistent state |
| 7 | 📇 **vCard + CSV** | Download individual vCard (.vcf) or export full directory as CSV |
| 8 | 🔐 **Enterprise Auth** | ASP.NET Core Identity with cookie auth. All API endpoints protected with `[Authorize]` |
| 9 | 📜 **Audit Trail** | Every create, update, delete, photo upload, and favorite toggle logged with username + timestamp |
| 10 | 🏗 **Clean Architecture** | Domain → Application → Infrastructure → API. Repository + Unit of Work. Zero magic strings |

### 🎯 Core Differentiator

> **PhoneBook HMO is the only government contact directory combining:**
> - DeepSeek AI smart search + auto-parse
> - WhatsApp bulk export with attribution
> - HMO premium design (navy + gold)
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

## 📄 License

MIT © [hartunnoo](https://github.com/hartunnoo)

---

<p align="center">
  <sub>Designed & Built by <strong>SSCU</strong>, ITD, Istana Nurul Iman</sub>
</p>
