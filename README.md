# CDN API — ASP.NET Core 8 + Onion Architecture + SQL Server

A production-ready Content Delivery Network API built with **ASP.NET Core 8**, **Onion Architecture**, and **SQL Server**.

---

## 🏛️ Architecture — Onion Architecture

```
CdnApi/
├── src/
│   ├── CdnApi.Domain/              ← Core business entities & enums (no dependencies)
│   ├── CdnApi.Application/         ← Interfaces, DTOs, business logic contracts
│   ├── CdnApi.Infrastructure/      ← DB, email, file storage, JWT, service implementations
│   └── CdnApi.API/                 ← Controllers, middleware, startup
├── tests/
│   └── CdnApi.Tests/
└── CdnApi.sln
```

### Layer Dependencies (strict Onion rule)
```
API  →  Infrastructure  →  Application  →  Domain
                       ↗
              (implements interfaces)
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server (or SQL Server Express / LocalDB)
- An SMTP account (Gmail, Mailtrap, SendGrid, etc.)

### 1. Clone & Configure
```bash
git clone <repo>
cd CdnApi
```

Open `src/CdnApi.API/appsettings.json` and update:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=CdnApiDb;..."
  },
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_MIN_32_CHARS"
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Username": "your@gmail.com",
    "Password": "your-app-password"
  },
  "AppSettings": {
    "BaseUrl": "https://localhost:5001",
    "FrontendUrl": "https://localhost:3000"
  }
}
```

### 2. Apply Database Migrations
```bash
cd src/CdnApi.API
dotnet ef database update --project ../CdnApi.Infrastructure
```

Or the app auto-migrates on first run.

### 3. Run
```bash
cd src/CdnApi.API
dotnet run
```

Swagger UI: `https://localhost:5001` (root)

---

## 📦 NuGet Packages

| Package | Layer | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | Infrastructure | ORM |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | Infrastructure | Migrations CLI |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Infrastructure / API | JWT auth |
| `System.IdentityModel.Tokens.Jwt` | Infrastructure | Token generation |
| `AutoMapper` | Application / Infrastructure | DTO mapping |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | API | DI registration |
| `Swashbuckle.AspNetCore` | API | Swagger/OpenAPI |
| `Microsoft.AspNetCore.RateLimiting` | API | Rate limiting |

---

## 🔑 Authentication

### JWT Bearer Token
```
Authorization: Bearer <your_jwt_token>
```

### API Key (programmatic access)
```
X-Api-Key: cdnapi_<your_key>
```
Create API keys via `POST /api/apikeys`.

---

## 📡 API Endpoints

### Auth (`/api/auth`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Register new user |
| POST | `/login` | — | Login → returns JWT + refresh token |
| POST | `/refresh-token` | — | Refresh expired access token |
| POST | `/logout` | ✅ | Invalidate refresh token |
| GET | `/verify-email?token=` | — | Verify email address |
| POST | `/forgot-password` | — | Send password reset email |
| POST | `/reset-password` | — | Reset password with token |
| POST | `/change-password` | ✅ | Change current password |

### User (`/api/user`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/profile` | ✅ | Get current user profile |
| PUT | `/profile` | ✅ | Update profile |
| POST | `/profile/picture` | ✅ | Upload profile picture |
| GET | `/storage` | ✅ | Get storage usage info |

### Files (`/api/files`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/upload` | ✅ | Upload single file |
| POST | `/upload/multiple` | ✅ | Upload up to 10 files |
| GET | `/` | ✅ | List my files (paginated) |
| GET | `/{fileId}` | ✅/— | Get file metadata |
| PUT | `/{fileId}` | ✅ | Update file metadata |
| DELETE | `/{fileId}` | ✅ | Delete file |

### CDN Delivery (`/cdn`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/{path}` | — | Serve file (public CDN URL) |

### API Keys (`/api/apikeys`)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/` | ✅ | Create API key |
| GET | `/` | ✅ | List my API keys |
| DELETE | `/{keyId}` | ✅ | Revoke API key |

### Admin (`/api/admin`) — Role: Admin / SuperAdmin
| Method | Endpoint | Description |
|---|---|---|
| GET | `/dashboard` | Dashboard stats |
| GET | `/users` | List all users |
| GET | `/users/{id}` | User detail |
| PUT | `/users/{id}` | Update user |
| POST | `/users/{id}/ban` | Ban user |
| POST | `/users/{id}/unban` | Unban user |
| DELETE | `/users/{id}` | Delete user (SuperAdmin) |
| GET | `/files` | All files |
| DELETE | `/files/{id}` | Delete any file |
| GET | `/audit-logs` | Audit logs |
| GET | `/settings` | System settings |
| PUT | `/settings/{key}` | Update setting (SuperAdmin) |

---

## 🗄️ Database Schema

```
Users ──────────────┬── CdnFiles ── FileAccessLogs
                    ├── UserApiKeys
                    └── AuditLogs

EmailLogs           (standalone)
SystemSettings      (standalone)
```

---

## 🛡️ Security Features

- **PBKDF2 + SHA256** password hashing (100,000 iterations)
- **JWT** access tokens (60 min) + **refresh tokens** (7–30 days)
- **Account lockout** after 5 failed login attempts (15 min)
- **API key hashing** with SHA-256 (plain key never stored)
- **Soft delete** for all entities
- **Global exception handler** (no stack traces in production)
- **Rate limiting**: 100 req/min globally, 10 req/5min for auth
- **CORS** policy (configurable allowed origins)

---

## 📧 SMTP Email Templates

All emails use responsive HTML templates:
- **Email Verification** — sent on registration
- **Password Reset** — expires in 1 hour
- **Welcome Email** — sent after email verification
- **Password Changed Notification**
- **Account Banned Notification**

---

## ⚙️ System Settings (Admin Configurable)

| Key | Default | Description |
|---|---|---|
| `MaxFileSizeMB` | `100` | Max upload size |
| `AllowedFileTypes` | JPEG,PNG,GIF,WEBP,MP4,PDF,TXT,ZIP | Allowed MIME types |
| `DefaultStorageQuotaGB` | `1` | New user quota |
| `MaintenanceMode` | `false` | Maintenance flag |

---

## 🗃️ EF Core Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/CdnApi.Infrastructure --startup-project src/CdnApi.API

# Apply migration
dotnet ef database update --project src/CdnApi.Infrastructure --startup-project src/CdnApi.API

# Rollback
dotnet ef database update PreviousMigrationName --project src/CdnApi.Infrastructure --startup-project src/CdnApi.API
```

---

## 🌍 Environment Variables (Production)

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Server=prod-server;..."
JwtSettings__SecretKey="your-production-secret"
SmtpSettings__Username="prod@yourdomain.com"
SmtpSettings__Password="prod-password"
AppSettings__BaseUrl="https://api.yourdomain.com"
```

---

## 🔧 Health Check

```
GET /health
```

Returns: `{ "status": "healthy", "timestamp": "...", "version": "1.0.0" }`
