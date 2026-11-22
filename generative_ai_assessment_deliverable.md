# Generative AI Tools Assessment — RESTful Task Management API

This document outlines my workflow, prompt engineering strategy, code verification process, and architectural refinements when using **Claude Code** to generate a RESTful API for a **Task Management System** in C# / ASP.NET Core.

---

## 1. Scenario & Engineering Prompt Used

To prevent generic, tightly coupled code (such as controllers directly calling an ORM), I used a **constraint-driven architectural prompt** specifying Clean Architecture boundaries, raw ADO.NET (`Npgsql`), explicit DTOs, and domain validation rules.

### Prompt Given to Claude Code:
> *"Act as a Principal .NET Backend Engineer. Generate a RESTful API in C# (ASP.NET Core Web API) for a Task Management System following strict Clean Architecture principles.*
>
> **Requirements & Constraints:**
> - **Entities:** `TaskItem` (`Id` Guid, `UserId` Guid, `Title` string, `Description` string, `Status` enum: `Pending`, `InProgress`, `Completed`, `DueDate` DateTime UTC, `CreatedAt` DateTime UTC) associated with an existing `User` (`Id`, `Email`).
> - **Endpoints:** Full CRUD (`GET /api/tasks`, `GET /api/tasks/{id}`, `POST /api/tasks`, `PUT /api/tasks/{id}`, `DELETE /api/tasks/{id}`) protected with `[Authorize]` JWT authentication.
> - **Hard Technical Constraint:** Do **NOT** use Entity Framework Core, Dapper, or MediatR. Implement the repository using raw `Npgsql` (`NpgsqlConnection`, `NpgsqlCommand`, `NpgsqlDataReader`) with parameterized SQL queries.
> - **Separation of Concerns:** Keep `Domain` and `Application` layers completely independent of `Npgsql` and `Microsoft.AspNetCore.Http`.
> - **Validation & Security:** Validate required fields, enforce valid status transitions, ensure `DueDate` is stored in UTC, and guarantee users can only read, update, or delete their own tasks."*

---

## 2. Representative Generated Output

Below is a representative sample of the Clean Architecture layers produced by the GenAI tool.

### A. Domain Entity (`Domain/Entities/TaskItem.cs`)
```csharp
namespace TaskManager.Domain.Entities;

public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2
}

public class TaskItem
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public TaskItem(Guid id, Guid userId, string title, string description, TaskStatus status, DateTime dueDate, DateTime createdAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Task must be associated with a valid user.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
            throw new ArgumentException("Title is required and must not exceed 150 characters.", nameof(title));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        UserId = userId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = status;
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
        CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
    }

    public void Update(string title, string description, TaskStatus newStatus, DateTime dueDate)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
            throw new ArgumentException("Title is required and must not exceed 150 characters.", nameof(title));

        if (Status == TaskStatus.Completed && newStatus == TaskStatus.Pending)
            throw new InvalidOperationException("A completed task cannot be reverted directly back to Pending.");

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = newStatus;
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);
    }
}
```

### B. Raw Npgsql Repository (`Infrastructure/Persistence/TaskRepository.cs`)
```csharp
using Npgsql;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskStatus = TaskManager.Domain.Entities.TaskStatus;

namespace TaskManager.Infrastructure.Persistence;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TaskRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TaskItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, user_id, title, description, status, due_date, created_at
            FROM tasks
            WHERE user_id = @user_id
            ORDER BY due_date ASC;
            """;

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId);

        var results = new List<TaskItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapTask(reader));
        }

        return results;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO tasks (id, user_id, title, description, status, due_date, created_at)
            VALUES (@id, @user_id, @title, @description, @status, @due_date, @created_at);
            """;

        await using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", task.Id);
        cmd.Parameters.AddWithValue("user_id", task.UserId);
        cmd.Parameters.AddWithValue("title", task.Title);
        cmd.Parameters.AddWithValue("description", task.Description);
        cmd.Parameters.AddWithValue("status", (int)task.Status);
        cmd.Parameters.AddWithValue("due_date", task.DueDate);
        cmd.Parameters.AddWithValue("created_at", task.CreatedAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static TaskItem MapTask(NpgsqlDataReader reader) =>
        new(
            id: reader.GetGuid(0),
            userId: reader.GetGuid(1),
            title: reader.GetString(2),
            description: reader.GetString(3),
            status: (TaskStatus)reader.GetInt32(4),
            dueDate: reader.GetDateTime(5),
            createdAt: reader.GetDateTime(6)
        );
}
```

### C. Web API Controller (`WebApi/Controllers/TasksController.cs`)
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetMyTasks(CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var tasks = await _taskService.GetUserTasksAsync(userId, ct);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var created = await _taskService.CreateTaskAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetTaskById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetTaskById(Guid id, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var task = await _taskService.GetTaskByIdAsync(userId, id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    private Guid GetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid or missing user identity claim.");
    }
}
```

---

## 3. How I Validated the AI's Suggestions

I never treat LLM-generated code as production-ready out of the box. I validated the output across four stages:

1. **Architectural Dependency Audit:**
   Inspected `.csproj` references to ensure `Domain` had zero NuGet dependencies and `Application` contained no `using Npgsql` or `using Microsoft.AspNetCore` directives.
2. **Compiler & Analyzer Verification:**
   Enabled `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to catch subtle null-reference assumptions in `NpgsqlDataReader` mapping and DTO deserialization.
3. **Automated Unit Testing (`xUnit` + `NSubstitute`):**
   Wrote unit tests targeting domain invariants (invalid titles, invalid status jumps) and mocked repository interactions in `TaskService` to verify business logic without touching a database.
4. **End-to-End Integration Testing against PostgreSQL:**
   Executed parameterized queries against a live Neon PostgreSQL instance via Swagger/Postman to verify `TIMESTAMPTZ` round-tripping, foreign-key constraints, and HTTP status codes (`201`, `400`, `401`, `403`, `404`).

---

## 4. Corrections & Improvements Made to the AI Output

During code review of the initial AI generation, I identified and corrected four concrete issues:

1. **Prevented Broken Object Level Authorization (BOLA / IDOR):**
   * *AI Flaw:* The initial `UpdateAsync` and `DeleteAsync` repository SQL queries filtered only by `WHERE id = @id`, relying solely on an application-layer check.
   * *Fix:* Added defense-in-depth at both layers—`TaskService` explicitly verifies `existingTask.UserId == currentUserId` (throwing a `ForbiddenAccessException` $\rightarrow$ `403 Forbidden`), and the SQL `DELETE`/`UPDATE` statements include `AND user_id = @user_id`.
2. **Fixed PostgreSQL `TIMESTAMPTZ` Kind Mismatch (`Npgsql` 6+ Strictness):**
   * *AI Flaw:* The AI passed `request.DueDate` directly to `cmd.Parameters.AddWithValue("due_date", task.DueDate)`. In modern `Npgsql`, passing a `DateTime` with `DateTimeKind.Unspecified` to a `TIMESTAMPTZ` column throws an `InvalidCastException`.
   * *Fix:* Explicitly normalized all incoming dates to UTC (`DateTime.SpecifyKind(dueDate.ToUniversalTime(), DateTimeKind.Utc)`) inside the Domain constructor and update methods.
3. **Eliminated Dynamic `ORDER BY` SQL Injection Risk:**
   * *AI Flaw:* When asked to add optional sorting, the AI initially interpolated `ORDER BY {sortBy}` directly into the SQL string.
   * *Fix:* Replaced string interpolation with a strict C# switch-expression allowlist (`"due_date" => "due_date ASC"`, `"status" => "status ASC"`, `_ => "created_at DESC"`).
4. **Replaced Generic Exceptions with RFC 7807 `ProblemDetails`:**
   * *AI Flaw:* The AI let `ArgumentException` and `InvalidOperationException` bubble up as unhandled `500 Internal Server Error` responses.
   * *Fix:* Added a centralized exception-handling middleware mapping domain validation errors to `400 Bad Request`, ownership violations to `403 Forbidden`, and missing records to `404 Not Found` using standard `ProblemDetails`.

---

## 5. Edge Cases, Authentication & Validations Handled

* **JWT Claim Resilience:** ASP.NET Core's default inbound claim mapping can rename `"sub"` to `ClaimTypes.NameIdentifier`. The controller helper checks both `ClaimTypes.NameIdentifier` and `"sub"` cleanly before parsing the `Guid`.
* **Past `DueDate` Edge Case:** Creation rejects tasks with a `DueDate` in the past (`DueDate < DateTime.UtcNow.AddMinutes(-5)` to allow minor client-server clock skew), while updates allow modifying the status of an already-overdue task without forcing the user to change its original due date.
* **Empty / Whitespace Strings & Enum Bounds:** `[ApiController]` combined with explicit domain guards rejects whitespace-only titles (`"   "`) and out-of-range integer enum values (e.g., `status: 99`) with clear `400 Bad Request` validation messages.
* **CancellationToken Propagation:** Threaded `CancellationToken` from controller actions down through `TaskService` into `NpgsqlCommand.ExecuteReaderAsync(ct)` so aborted HTTP requests immediately cancel in-flight database queries.