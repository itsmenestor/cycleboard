# Cycleboard — Minimalist Shape Up / Cycle Pitch Tracker

**Cycleboard** is a full-stack product delivery and pitch-tracking web application inspired by Basecamp's *Shape Up* methodology. Built with **.NET Web API / MVC**, **Angular**, and **Neon (PostgreSQL)**, the application strictly adheres to **Clean Architecture**, **Test-Driven Development (TDD)**, and explicit **raw ADO.NET (`Npgsql`)** data access without ORMs or mediator abstractions.

---

## Live Demo & Credentials

* **Frontend (Netlify):** `https://cycleboard-app.netlify.app` *(update with your deployed Netlify URL)*
* **Backend API & Status Page (Azure App Service):** `https://cycleboard-api.azurewebsites.net` *(update with your deployed Azure URL)*
* **Interactive API Documentation (Swagger):** `https://cycleboard-api.azurewebsites.net/swagger`

### Seeded Demo Credentials
The backend automatically seeds the PostgreSQL database on startup if no users exist:

| Role | Email | Password |
| :--- | :--- | :--- |
| **Product Engineering Lead (Primary)** | `demo@cycleboard.dev` | `DemoPass123!` |
| **Engineering Lead (Secondary)** | `sam@cycleboard.dev` | `DemoPass123!` |

*(You can also click the **"Fill Demo Credentials"** button on the login screen or register a new account.)*

---

## Informal User Story & Thought Process

### User Story
> *"As a product engineering lead, I want to pitch project ideas with a defined appetite (2-week Small Batch or 6-week Big Batch), track their lifecycle status from 'Draft' through 'Shaped' and 'Betted' to 'Shipped', and showcase public shipped wins so my team can maintain focused, time-boxed product delivery without endless backlog bloat."*

### Engineering Thought Process & Design Decisions
1. **Why Shape Up ("Cycleboard")?**
   Traditional to-do apps often devolve into flat CRUD forms with no meaningful domain rules. Modeling a Shape Up pitch lifecycle introduces real **state-machine invariants** (e.g., a pitch cannot jump directly from `Draft` to `Shipped`, appetites are strictly constrained to `2` or `6` weeks, and `Draft` pitches cannot be published publicly) that naturally justify a dedicated, framework-independent Business Logic Layer.
2. **Why Raw `Npgsql` Instead of EF Core or Dapper?**
   Per the assessment constraints, no ORMs or mediator libraries were used. Writing repositories directly against `NpgsqlConnection`, `NpgsqlCommand`, and `NpgsqlDataReader` provides complete control over SQL execution, connection pooling, and parameterized queries while keeping the `Domain` and `Application` layers completely decoupled from database mechanics.
3. **Why Explicit Services Over MediatR?**
   Instead of hiding dependencies behind a generic `IMediator` pipeline, use cases are modeled as explicit, cohesive application services (`IPitchService`, `IAuthService`) with clear constructor injection. This makes dependencies immediately visible in unit tests and code reviews.
4. **Security & Isolation:**
   * Passwords are hashed using salted cryptographic hashing (`PBKDF2-SHA256` / `BCrypt`)—never stored in plaintext.
   * All mutation endpoints (`POST`, `PUT`, `DELETE`) enforce resource ownership (`pitch.UserId == currentUserId`), returning `403 Forbidden` if a user attempts to alter another lead's pitch.
   * Global exception middleware translates domain exceptions into standard RFC 7807 `ProblemDetails` JSON responses without leaking stack traces.

---

## Architecture Overview

Cycleboard follows **Clean Architecture** with strict inward-pointing dependencies:

```text
+-------------------------------------------------------------------+
|                        CycleBoard.WebApi                          |
|   ASP.NET Controllers, MVC Status View, JWT Auth, CORS, Swagger   |
+---------------------------------+---------------------------------+
                                  | depends on
                                  v
+-------------------------------------------------------------------+
|                     CycleBoard.Infrastructure                     |
|   Raw Npgsql Repositories, SQL Schema Initializer, DbSeeder,      |
|   JWT Token Generator, Cryptographic Password Hasher              |
+---------------------------------+---------------------------------+
                                  | implements interfaces from
                                  v
+-------------------------------------------------------------------+
|                      CycleBoard.Application                       |
|   PitchService, AuthService, DTOs, Validators, Repository &       |
|   Security Interfaces (Zero dependency on Npgsql or ASP.NET)      |
+---------------------------------+---------------------------------+
                                  | depends on
                                  v
+-------------------------------------------------------------------+
|                        CycleBoard.Domain                          |
|   User & Pitch Entities, PitchStatus State Machine, Domain        |
|   Invariants & Exceptions (Zero external NuGet dependencies)      |
+-------------------------------------------------------------------+
```

### Brand Palette & UI Design
* **Primary Brand (`#2563EB` Horizon Blue):** Primary CTAs, active stage indicators, navigation highlights.
* **Secondary Brand (`#10B981` Emerald):** `Shipped` status badges, completed cycle indicators.
* **Accent (`#F59E0B` Amber):** `Betted` / attention indicators and appetite callouts.
* **Neutral Text (`#1F2937`):** High-contrast typography on `#F9FAFB` canvas with `#E5E7EB` subtle borders.

---

## API Endpoints Summary

### Authentication API (`/api/auth`)
* `POST /api/auth/register` (`[AllowAnonymous]`) — Creates a new user account and returns a signed JWT.
* `POST /api/auth/login` (`[AllowAnonymous]`) — Validates credentials and returns a signed JWT.
* `GET /api/auth/me` (`[Authorize]`) — Returns the currently authenticated user's profile.

### Pitches API (`/api/pitches`)
* `GET /api/pitches/public` (`[AllowAnonymous]`) — Public showcase of shared pitches (supports `?appetite=2|6`).
* `GET /api/pitches` (`[Authorize]`) — Lists all pitches belonging to the authenticated user.
* `GET /api/pitches/{id}` (`[Authorize]`) — Retrieves a single pitch owned by the authenticated user.
* `POST /api/pitches` (`[Authorize]`) — Creates a new pitch (`201 Created`).
* `PUT /api/pitches/{id}` (`[Authorize]`) — Updates pitch details or advances Shape Up lifecycle stage.
* `DELETE /api/pitches/{id}` (`[Authorize]`) — Deletes a pitch owned by the authenticated user (`204 No Content`).

---

## Local Setup Instructions

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download) (or .NET 9 SDK)
* [Node.js 20 LTS+](https://nodejs.org/) & Angular CLI (`npm install -g @angular/cli`)
* A [Neon PostgreSQL](https://neon.tech/) database connection string (or local PostgreSQL 15+)

### 1. Backend Setup (`/backend`)
1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Configure your Neon PostgreSQL connection string and JWT secret in `src/CycleBoard.WebApi/appsettings.Development.json` (or via environment variables):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=your-neon-host.neon.tech;Database=cycleboard;Username=your_user;Password=your_password;SSL Mode=Require;Trust Server Certificate=true"
     },
     "Jwt": {
       "Secret": "SuperSecretKeyForCycleboardDevelopmentMustBe32CharsLong!",
       "Issuer": "CycleboardApi",
       "Audience": "CycleboardClient",
       "ExpirationMinutes": 120
     }
   }
   ```
3. Run the unit test suite across all four architectural layers:
   ```bash
   dotnet test
   ```
4. Start the Web API (this automatically runs `SqlSchemaInitializer` and `DbSeeder` on startup):
   ```bash
   dotnet run --project src/CycleBoard.WebApi
   ```
   The API will listen on `https://localhost:7001` (or `http://localhost:5000`).

### 2. Frontend Setup (`/frontend`)
1. Open a new terminal and navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Verify `src/environments/environment.ts` points to your local backend API URL (`http://localhost:5000/api`).
4. Start the Angular development server:
   ```bash
   ng serve
   ```
5. Open `http://localhost:4200` in your browser.

---

## Cloud Deployment Overview

* **Database (Neon):** Serverless PostgreSQL instance with connection pooling enabled and `SSL Mode=Require`.
* **Backend (Azure App Service):** Deployed as a .NET Web App with `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, and `Cors__AllowedOrigins` configured in Azure Application Settings.
* **Frontend (Netlify):** Built via `npm run build` publishing `dist/cycleboard-frontend/browser` with SPA fallback routing (`/* -> /index.html 200`) configured in `frontend/netlify.toml`.