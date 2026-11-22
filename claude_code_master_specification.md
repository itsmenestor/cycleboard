# Master Build Specification: CycleBoard (Shape Up / Cycle Pitch Tracker)

You are a Principal Full-Stack .NET & Angular Engineer. Your task is to build **CycleBoard**, a full-stack web application adhering strictly to **Clean Architecture**, **Test-Driven Development (TDD)**, and all technical constraints specified below.

---

## 1. Critical Hard Constraints (Non-Negotiable)

1. **NO ORMs or Mediator Libraries:**
   - **FORBIDDEN:** Entity Framework Core (`Microsoft.EntityFrameworkCore`), Dapper, MediatR, AutoMapper (prefer explicit mapping extensions for transparency and zero magic).
   - **REQUIRED:** Raw **Npgsql** (`NpgsqlDataSource`, `NpgsqlCommand`, `NpgsqlDataReader`) using parameterized SQL queries and async ADO.NET patterns against **Neon (PostgreSQL)**.
2. **Monorepo Structure (Single Repository):**
   - Everything must reside in a single repository with clear separation between `/backend`, `/frontend`, and root documentation (`README.md`, `GENAI.md`).
   - **Security Rule:** Do **NOT** create any symlinks anywhere in the repository.
3. **Architecture & Separation of Concerns:**
   - Strict **Clean Architecture** dependency rule: `Domain` (0 external dependencies) $\leftarrow$ `Application` (depends only on `Domain`) $\leftarrow$ `Infrastructure` (depends on `Application` & `Domain`) & `WebApi` (depends on `Application`, registers `Infrastructure` via DI).
   - The **Business Logic Layer (`Application` + `Domain`)** must be 100% independent of `Npgsql`, HTTP contexts, and ASP.NET controllers.
4. **Testing Coverage:**
   - Write comprehensive unit tests using **xUnit**, **FluentAssertions**, and **NSubstitute** (or Moq) covering **all four layers**: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, and `WebApi.Tests`.
5. **Production-Ready Deployment Targets:**
   - **Database:** Neon PostgreSQL (`SSL Mode=Require`).
   - **Backend:** ASP.NET Core (.NET 8 or .NET 9) Web API + MVC (for a minimal health/landing status view) ready for **Azure App Service**.
   - **Frontend:** Angular (latest standalone components architecture, strict TypeScript, zero browser console warnings/errors) ready for **Netlify**.

---

## 2. Informal User Story & Domain Rules

### User Story
> *"As a product engineering lead, I want to pitch project ideas with a defined appetite (2-week or 6-week cycles), track their lifecycle status from 'Draft' through 'Shaped' and 'Betted' to 'Shipped', and view public shipped wins so my team can maintain focused, time-boxed product delivery without backlog bloat."*

### Domain Entities & Business Rules

#### A. `User` Entity (`Domain/Entities/User.cs`)
- `Id` (`Guid`, Primary Key)
- `Email` (`string`, unique, normalized lowercase, valid email format)
- `FullName` (`string`, required, 2–100 chars)
- `PasswordHash` (`string`, BCrypt or PBKDF2-SHA256 hash—never plain text)
- `Role` (`string`, default `"Lead"`)
- `CreatedAt` (`DateTime`, UTC)

#### B. `Pitch` Entity (`Domain/Entities/Pitch.cs`)
- `Id` (`Guid`, Primary Key)
- `UserId` (`Guid`, Foreign Key to `users.id`)
- `AuthorName` (`string`, joined/hydrated for display)
- `Title` (`string`, required, 5–120 chars)
- `ProblemStatement` (`string`, required, 10–2000 chars)
- `AppetiteWeeks` (`int`, **strictly `2` or `6`** — Small Batch vs. Big Batch)
- `Status` (`PitchStatus` enum: `Draft = 0`, `Shaped = 1`, `Betted = 2`, `Shipped = 3`)
- `IsPublic` (`bool`, default `false`)
- `CreatedAt` (`DateTime`, UTC)
- `UpdatedAt` (`DateTime`, UTC)

#### C. Domain Invariants & Business Logic Rules (Enforced in `Domain` & `Application`)
1. **Appetite Validation:** `AppetiteWeeks` must be either `2` or `6`. Any other integer throws a `DomainValidationException`.
2. **State Machine Transition Rule:**
   - Statuses follow a controlled Shape Up workflow:
     - `Draft` $\rightarrow$ `Shaped` (or stay `Draft`)
     - `Shaped` $\rightarrow$ `Betted` or back to `Draft`
     - `Betted` $\rightarrow$ `Shipped` or back to `Shaped`
     - `Shipped` $\rightarrow$ Terminal state (cannot revert to `Draft`; title/appetite locked once `Shipped`).
   - Attempting an invalid jump (e.g., `Draft` directly to `Shipped`) must throw a `DomainValidationException` with a clear explanation.
3. **Public Visibility Rule:**
   - Only pitches in `Shaped`, `Betted`, or `Shipped` status can have `IsPublic = true` (or allow `IsPublic` toggle on any non-Draft pitch; a `Draft` pitch cannot be published publicly).
4. **Ownership Authorization Rule:**
   - Only the `UserId` who created a pitch can update or delete it. Attempting to modify another user's pitch throws `ForbiddenAccessException` (mapped to HTTP `403 Forbidden`).

---

## 3. Repository Directory Structure

Generate the exact folder layout below:

```text
cycleboard/
├── README.md                        # Complete setup guide, architecture overview, user story, demo credentials
├── GENAI.md                         # Root-level GenAI assessment deliverable (Task Management API prompt + analysis)
├── .gitignore
├── backend/
│   ├── CycleBoard.sln
│   ├── database/
│   │   └── init.sql                 # Idempotent PostgreSQL DDL + seed script for Neon
│   ├── src/
│   │   ├── CycleBoard.Domain/
│   │   │   ├── Entities/
│   │   │   ├── Enums/
│   │   │   └── Exceptions/
│   │   ├── CycleBoard.Application/
│   │   │   ├── Common/              # Result<T> / Validation helpers
│   │   │   ├── DTOs/
│   │   │   ├── Interfaces/          # IUserRepository, IPitchRepository, IJwtTokenGenerator, IPasswordHasher, IDbConnectionFactory
│   │   │   └── Services/            # AuthService, PitchService (pure business logic)
│   │   ├── CycleBoard.Infrastructure/
│   │   │   ├── Persistence/         # NpgsqlConnectionFactory, SqlSchemaInitializer, UserRepository, PitchRepository
│   │   │   └── Security/            # JwtTokenGenerator, PasswordHasher
│   │   └── CycleBoard.WebApi/
│   │       ├── Controllers/
│   │       │   ├── AuthController.cs
│   │       │   ├── PitchesController.cs
│   │       │   └── HomeController.cs # Minimal ASP.NET MVC controller returning a status/docs view at "/"
│   │       ├── Middleware/          # GlobalExceptionHandlerMiddleware (ProblemDetails RFC 7807)
│   │       ├── Views/Home/Index.cshtml
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       └── Program.cs
│   └── tests/
│       ├── CycleBoard.Domain.Tests/
│       ├── CycleBoard.Application.Tests/
│       ├── CycleBoard.Infrastructure.Tests/
│       └── CycleBoard.WebApi.Tests/
└── frontend/
    ├── netlify.toml                 # SPA redirect rules (`/* -> /index.html 200`) & build config
    ├── angular.json
    ├── package.json
    └── src/
        ├── environments/
        │   ├── environment.ts
        │   └── environment.prod.ts
        └── app/
            ├── core/
            │   ├── guards/          # auth.guard.ts
            │   ├── interceptors/    # jwt.interceptor.ts, error.interceptor.ts
            │   ├── models/          # auth.model.ts, pitch.model.ts
            │   └── services/        # auth.service.ts, pitch.service.ts
            ├── features/
            │   ├── auth/            # login.component.ts, register.component.ts
            │   ├── board/           # pitch-board.component.ts (Kanban/List by Shape Up stage + CRUD modal/drawer)
            │   └── public-showcase/ # public-pitches.component.ts (consumes non-authorized endpoint)
            └── shared/
                └── components/      # navbar.component.ts, toast/alert banner
```

---

## 4. Database Schema (Neon PostgreSQL)

Implement `backend/database/init.sql` and execute it idempotently on application startup via `SqlSchemaInitializer` in `Infrastructure` (so deploying to Azure + Neon works out-of-the-box without manual migrations):

```sql
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    full_name VARCHAR(100) NOT NULL,
    password_hash TEXT NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'Lead',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS pitches (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(120) NOT NULL,
    problem_statement TEXT NOT NULL,
    appetite_weeks INT NOT NULL CHECK (appetite_weeks IN (2, 6)),
    status INT NOT NULL CHECK (status BETWEEN 0 AND 3),
    is_public BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pitches_user_id ON pitches(user_id);
CREATE INDEX IF NOT EXISTS idx_pitches_is_public ON pitches(is_public) WHERE is_public = TRUE;
```

### Seeded Demo Data (`DbSeeder`)
On startup, if `users` has no rows, seed:
1. **Demo User 1:** `demo@cycleboard.dev` / Password: `DemoPass123!` / Name: `Alex Rivera`
2. **Demo User 2:** `sam@cycleboard.dev` / Password: `DemoPass123!` / Name: `Samira Patel`
3. **6 Realistic Pitches** across all 4 stages (`Draft`, `Shaped`, `Betted`, `Shipped`) and both appetites (`2` and `6` weeks), with at least 3 marked `IsPublic = true` so the public endpoint and dashboard look rich immediately upon login.

---

## 5. Backend API Specification

### A. Auth API (`/api/auth`)
| Method | Endpoint | Auth | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | `[AllowAnonymous]` | `{ email, fullName, password }` | `201 Created` `{ token, user: { id, email, fullName, role } }` |
| `POST` | `/api/auth/login` | `[AllowAnonymous]` | `{ email, password }` | `200 OK` `{ token, user: { id, email, fullName, role } }` |
| `GET` | `/api/auth/me` | `[Authorize]` | — | `200 OK` `{ id, email, fullName, role, createdAt }` |

### B. Pitches API (`/api/pitches`)
| Method | Endpoint | Auth | Parameters / Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/pitches/public` | `[AllowAnonymous]` | Query: `?appetite=2\|6` (optional) | `200 OK` `List<PitchDto>` (Non-authorized public showcase) |
| `GET` | `/api/pitches` | `[Authorize]` | Query: `?status=0..3` (optional) | `200 OK` `List<PitchDto>` (Authenticated user's pitches) |
| `GET` | `/api/pitches/{id:guid}` | `[Authorize]` | Route: `id` | `200 OK` `PitchDto` or `404 Not Found` |
| `POST` | `/api/pitches` | `[Authorize]` | `{ title, problemStatement, appetiteWeeks, isPublic }` | `201 Created` (`Location` header + `PitchDto`) |
| `PUT` | `/api/pitches/{id:guid}` | `[Authorize]` | `{ title, problemStatement, appetiteWeeks, status, isPublic }` | `200 OK` `PitchDto` or `400`/`403`/`404` |
| `DELETE` | `/api/pitches/{id:guid}` | `[Authorize]` | Route: `id` | `204 No Content` or `403`/`404` |

### C. MVC Landing View (`/`)
- `HomeController.Index()` serves a clean Razor view (`Views/Home/Index.cshtml`) showing API health status, PostgreSQL connection status, and a link to `/swagger` (enabled in all environments for easy interviewer inspection).

---

## 6. Data Access Layer Details (Raw Npgsql)

In `CycleBoard.Infrastructure`:
- Create `IDbConnectionFactory` returning an open `NpgsqlConnection` (`Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken ct)`).
- Every repository method (`UserRepository`, `PitchRepository`) must:
  - Open/dispose connections cleanly via `await using`.
  - Use explicit `NpgsqlParameter` bindings (`cmd.Parameters.AddWithValue(...)`)—zero string interpolation in SQL.
  - Map `NpgsqlDataReader` columns cleanly using strongly typed ordinal/named helper methods.
  - Keep SQL queries clean, readable, and well-indexed.

---

## 7. Unit Testing Requirements (TDD Mindset)

Create four dedicated test projects and ensure `dotnet test` passes 100%:
1. **`CycleBoard.Domain.Tests`**:
   - Test `Pitch` creation with valid (`2`, `6`) and invalid (`1`, `3`, `10`) appetites.
   - Test every valid and invalid status transition (`Draft -> Shaped`, `Draft -> Shipped` fails, `Shipped -> Draft` fails, publishing a `Draft` fails).
   - Test `User` email normalization and invariant checks.
2. **`CycleBoard.Application.Tests`**:
   - Mock `IPitchRepository`, `IUserRepository`, `IPasswordHasher`, and `IJwtTokenGenerator` using **NSubstitute**.
   - Test `AuthService` (duplicate email registration throws conflict exception, invalid login credentials throw unauthorized exception, valid login returns JWT).
   - Test `PitchService` (CRUD happy paths, enforcing `UserId` ownership on update/delete so non-owners receive `ForbiddenAccessException`).
3. **`CycleBoard.Infrastructure.Tests`**:
   - Test `PasswordHasher` (verifies hash uniqueness with salt, verifies true/false password matching).
   - Test `JwtTokenGenerator` (decodes generated JWT using `JwtSecurityTokenHandler` and asserts `sub`, `email`, `role`, issuer, audience, and expiration claims).
   - Test SQL parameter construction / `DbDataReader` mapping logic using mocked/stubbed `DbDataReader` so data-layer mapping is verified without requiring a live DB during CI unit test runs.
4. **`CycleBoard.WebApi.Tests`**:
   - Instantiate `PitchesController` and `AuthController` with mocked `IPitchService` / `IAuthService` and simulated `ClaimsPrincipal` (`HttpContext.User`).
   - Assert exact HTTP `ActionResult` types and status codes (`200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`).

---

## 8. Frontend Specification (Angular + Netlify)

Build a crisp, minimalist, responsive Angular application in `/frontend`:
1. **Design & UX:**
   - Clean, modern SaaS aesthetic (Linear / Basecamp Shape Up inspired) using clean CSS variables or Tailwind CSS—no bloated UI kits that emit console warnings.
   - **Zero Browser Console Errors or Warnings:** Ensure strict TypeScript (`strict: true`), proper RxJS subscription cleanup (`takeUntilDestroyed()`), valid HTML semantics, and clean handling of HTTP errors.
2. **Views & Features:**
   - **Public Showcase (`/explore`):** Accessible without logging in; fetches `GET /api/pitches/public` with filter pills (`All`, `2-Week Small Batch`, `6-Week Big Batch`).
   - **Login & Register (`/login`, `/register`):** Includes a **"1-Click Fill Demo Credentials"** button on the Login screen (`demo@cycleboard.dev` / `DemoPass123!`) so the interview panel can test immediately.
   - **Cycle Board Workspace (`/board`, protected by `AuthGuard`):**
     - Displays the user's pitches grouped into 4 Shape Up columns (`Draft`, `Shaped`, `Betted`, `Shipped`) with responsive stacking on mobile.
     - Displays appetite badges (`2w Batch` vs `6w Batch`) and `Public` badge.
     - **Create / Edit Pitch Modal:** Full validation matching backend rules (Title length, Problem statement, Appetite `2` or `6`, Status transitions, Public toggle).
     - **Quick Stage Advance Button:** Lets the user advance a pitch to the next valid stage (`Draft -> Shaped -> Betted -> Shipped`) with clear error toast feedback if an invalid rule is triggered.
     - **Delete Pitch** with confirmation prompt.
3. **Netlify Configuration (`frontend/netlify.toml`):**
   ```toml
   [build]
     base = "frontend"
     command = "npm run build"
     publish = "dist/cycleboard-frontend/browser"

   [[redirects]]
     from = "/*"
     to = "/index.html"
     status = 200
   ```

---

## 9. Required Root Documentation Files

### A. `README.md` (Root Level)
Must clearly include:
1. **Project Overview & Informal User Story**
2. **Demo Credentials** (prominently at the top) + Live URL placeholders (Netlify & Azure)
3. **Architecture & Design Decisions** (Clean Architecture diagram, why raw `Npgsql` was used instead of EF/Dapper, Shape Up state machine rules, JWT security design)
4. **Local Setup Instructions** (Neon PostgreSQL connection string setup, `dotnet run`, `dotnet test`, `ng serve`)
5. **Cloud Deployment Guide** (Neon $\rightarrow$ Azure App Service $\rightarrow$ Netlify CORS & environment wiring)

### B. `GENAI.md` (Root Level — Mandatory Assessment Deliverable)
Create `GENAI.md` answering the exact prompt from the technical test:
- **Scenario:** Generating a RESTful API for a simple Task Management System (`title`, `description`, `status`, `due_date`, associated with a `User`) in C# / ASP.NET Core without EF/Dapper/MediatR.
- **Section 1: The Prompt Used** — Show a structured, constraint-driven engineering prompt tailored for Claude Code / Cursor specifying Clean Architecture, parameterized Npgsql, DTO separation, and validation rules.
- **Section 2: Representative Generated Output** — Provide clean, realistic C# code snippets (`TaskItem` domain entity, `TaskService` business logic, `TasksController` endpoint).
- **Section 3: Validation & Verification Process** — Explain how the code was validated (compiling, running `dotnet test`, inspecting SQL execution plans/parameter types, testing Swagger endpoints).
- **Section 4: Corrections & Improvements Made to AI Output** — Detail concrete flaws caught in typical LLM output and fixed (e.g., fixing an N+1 query or missing `user_id` ownership check on `PUT`/`DELETE`, replacing string-interpolated `ORDER BY` with a strict allowlist to prevent SQL injection, and adding UTC `DateTime` enforcement for `due_date`).
- **Section 5: Edge Cases, Authentication & Validations** — Detail handling of expired/missing JWT claims, past `due_date` validation rules, invalid enum status transitions, and RFC 7807 `ProblemDetails` error responses.

---

## 10. Execution Order for Claude Code

Execute the build step-by-step and verify at each checkpoint:
1. Initialize the solution structure (`CycleBoard.sln`) and four `src` projects + four `tests` projects with exact Clean Architecture project references.
2. Implement `CycleBoard.Domain` and run `dotnet test` on `Domain.Tests` (TDD Red-Green-Refactor).
3. Implement `CycleBoard.Application` and run `dotnet test` on `Application.Tests`.
4. Implement `CycleBoard.Infrastructure` (raw `Npgsql`, `SqlSchemaInitializer`, `DbSeeder`, `JwtTokenGenerator`, `PasswordHasher`) and run `dotnet test` on `Infrastructure.Tests`.
5. Implement `CycleBoard.WebApi` (Controllers, Swagger, CORS, Global Exception Middleware, MVC Home view) and run `dotnet test` across the entire solution.
6. Scaffold and build the Angular frontend in `/frontend`, ensuring `ng build` succeeds with zero warnings or errors.
7. Write `README.md` and `GENAI.md` at the repository root, and verify there are **no symlinks** (`find . -type l`) anywhere in the project.