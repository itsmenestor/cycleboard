# Hora — Shape Up Pitch Tracker

**Hora** is a full-stack product delivery and pitch-tracking application built on Basecamp's *Shape Up* methodology. Teams pitch ideas with a defined appetite (2-week Small Batch or 6-week Big Batch), advance pitches through a strict lifecycle (`Draft → Shaped → Betted → Shipped`), and showcase public wins on a community board.

Built with **.NET 11 Web API + MVC**, **Angular 19**, and **PostgreSQL (Neon)**, the system enforces **Clean Architecture**, **TDD** (56 passing tests across 4 layers), and **raw Npgsql** data access — no ORMs, no MediatR, no AutoMapper.

---

## Live Demo

| Resource | URL |
|:---|:---|
| **Frontend (Netlify)** | `https://meek-bublanina-7bc461.netlify.app` |
| **Backend API + Status Page (Azure)** | `https://cycleboard-api.azurewebsites.net` |
| **Interactive Swagger Docs** | `https://cycleboard-api.azurewebsites.net/swagger` |

### Demo Credentials

| Account | Email | Password |
|:---|:---|:---|
| Product Engineering Lead | `demo@hora.dev` | `DemoPass123!` |
| Engineering Lead | `sam@hora.dev` | `DemoPass123!` |

> Click **⚡ Fill Demo Credentials** on the login screen to auto-fill.

---

## Architecture

Strict inward-pointing dependency rule — outer rings know nothing about inner ones:

```
┌─────────────────────────────────────────────────────────────┐
│                      Hora.WebApi                       │
│  ASP.NET Controllers · MVC Status Page · JWT Auth · Swagger  │
└──────────────────────────────┬──────────────────────────────┘
                               │ depends on
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Hora.Infrastructure                   │
│  Raw Npgsql Repos · SQL Schema Init · DbSeeder · BCrypt JWT   │
└──────────────────────────────┬──────────────────────────────┘
                               │ implements interfaces from
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Hora.Application                     │
│  PitchService · AuthService · DTOs                            │
│  Zero dependency on Npgsql or ASP.NET                         │
└──────────────────────────────┬──────────────────────────────┘
                               │ depends on
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      Hora.Domain                        │
│  User · Pitch entities · PitchStatus state machine            │
│  Domain invariants & exceptions · Zero external NuGet deps    │
└─────────────────────────────────────────────────────────────┘
```

### Shape Up State Machine

```
Draft ──► Shaped ──► Betted ──► Shipped (terminal)
         ◄──────── (rollback allowed for Shaped → Draft, Betted → Shaped)
```

Illegal jumps (`Draft → Shipped`, `Draft → Betted`, re-opening `Shipped`) throw `DomainValidationException → 400 Bad Request`.

---

## Brand Palette

| Token | Hex | Use |
|:---|:---|:---|
| `--primary` | `#2563EB` Horizon Blue | CTAs, active states, navigation |
| `--secondary` | `#10B981` Emerald | Shipped badges, success indicators |
| `--accent` | `#F59E0B` Amber | Betted stage, appetite callouts |
| `--neutral` | `#1F2937` | High-contrast body text |
| `--bg` | `#F9FAFB` | Page canvas |
| `--border` | `#E5E7EB` | Subtle dividers |

---

## API Endpoints

### Auth (`/api/auth`)

| Method | Path | Auth | Description |
|:---|:---|:---|:---|
| `POST` | `/api/auth/register` | Public | Create account → returns JWT |
| `POST` | `/api/auth/login` | Public | Sign in → returns JWT |
| `GET` | `/api/auth/me` | Bearer | Current user profile |

### Pitches (`/api/pitches`)

| Method | Path | Auth | Description |
|:---|:---|:---|:---|
| `GET` | `/api/pitches/public` | Public | Community showcase (`?appetite=2\|6`) |
| `GET` | `/api/pitches` | Bearer | My pitches (`?status=0..3`) |
| `GET` | `/api/pitches/{id}` | Bearer | Single pitch (owner only) |
| `POST` | `/api/pitches` | Bearer | Create → `201 Created` |
| `PUT` | `/api/pitches/{id}` | Bearer | Update or advance stage |
| `DELETE` | `/api/pitches/{id}` | Bearer | Delete → `204 No Content` |

All errors return RFC 7807 `ProblemDetails` JSON.

---

## Local Setup

### Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20 LTS+](https://nodejs.org/) and Angular CLI (`npm install -g @angular/cli`)
- PostgreSQL 15+ (or a free [Neon](https://neon.tech/) serverless instance)

---

### Backend

```bash
cd backend

# 1. Configure connection string + JWT secret
# Edit src/Hora.WebApi/appsettings.Development.json:
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hora;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "SuperSecretKeyForHoraDevelopmentMustBe32CharsLong!",
    "Issuer": "HoraApi",
    "Audience": "HoraClient",
    "ExpirationMinutes": 120
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}

# 2. Run all 56 tests across Domain / Application / Infrastructure / WebApi
dotnet test

# 3. Start API (auto-runs SQL schema init + demo data seeder)
dotnet run --project src/Hora.WebApi
# → http://localhost:5007  |  Swagger: http://localhost:5007/swagger
```

---

### Frontend

```bash
cd frontend

# 1. Install dependencies
npm install

# 2. Start dev server (proxies to localhost:5000)
ng serve
# → http://localhost:4200

# 3. Production build (output: dist/hora/browser)
npm run build
```

---

## Test Coverage

| Layer | Tests | Covers |
|:---|:---|:---|
| `Hora.Domain.Tests` | 28 | Entity invariants, state machine, User validation |
| `Hora.Application.Tests` | 13 | Service logic, ownership enforcement, NSubstitute mocks |
| `Hora.Infrastructure.Tests` | 6 | BCrypt hashing, JWT generation + claim validation |
| `Hora.WebApi.Tests` | 9 | Controller HTTP status codes, ClaimsPrincipal integration |
| **Total** | **56** | **All passing** |

Run all tests:

```bash
cd backend && dotnet test
```

---

## Deployment

### Database (Neon)

Create a serverless PostgreSQL project at [neon.tech](https://neon.tech/). The backend's `SqlSchemaInitializer` runs `CREATE TABLE IF NOT EXISTS` on startup — no manual migrations needed.

### Backend (Azure App Service)

1. Create an Azure App Service (Linux, .NET 11).
2. Set Application Settings:
   - `ConnectionStrings__DefaultConnection` — your Neon connection string
   - `Jwt__Secret` — 32+ char random secret
   - `Cors__AllowedOrigins` — your Netlify URL (`https://meek-bublanina-7bc461.netlify.app`)
3. Deploy via GitHub Actions or `az webapp deploy`.

### Frontend (Netlify)

The repo includes `frontend/netlify.toml` with SPA redirect rules:

```toml
[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200

[build]
  command = "npm run build"
  publish = "dist/hora/browser"
```

Set environment variable `VITE_API_URL` or update `environment.prod.ts` to point at your Azure App Service URL before deploying.

---

## Project Structure

```
hora/
├── backend/
│   ├── src/
│   │   ├── Hora.Domain/          # Entities, state machine, exceptions
│   │   ├── Hora.Application/     # Services, DTOs, interfaces
│   │   ├── Hora.Infrastructure/  # Npgsql repos, JWT, BCrypt, seeder
│   │   └── Hora.WebApi/          # Controllers, MVC status page, Program.cs
│   └── tests/
│       ├── Hora.Domain.Tests/
│       ├── Hora.Application.Tests/
│       ├── Hora.Infrastructure.Tests/
│       └── Hora.WebApi.Tests/
└── frontend/
    └── src/app/
        ├── core/                       # Services, interceptors, guards, models
        ├── shared/components/          # Navbar, Toast
        └── features/
            ├── auth/                   # Login, Register
            ├── board/                  # Kanban pitch board (protected)
            └── public-showcase/        # Public explore page
```
