using CycleBoard.Application.Interfaces;
using CycleBoard.Domain.Entities;
using CycleBoard.Domain.Enums;
using Npgsql;

namespace CycleBoard.Infrastructure.Persistence;

public sealed class DbSeeder
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DbSeeder(IDbConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);

        await using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users;", conn);
        var count = (long)(await checkCmd.ExecuteScalarAsync(ct))!;
        if (count > 0) return;

        var alexId = Guid.NewGuid();
        var samId = Guid.NewGuid();
        var hash = _passwordHasher.Hash("DemoPass123!");

        var alex = User.Create(alexId, "demo@cycleboard.dev", "Alex Rivera", hash);
        var sam = User.Create(samId, "sam@cycleboard.dev", "Samira Patel", hash);

        await InsertUserAsync(conn, alex, ct);
        await InsertUserAsync(conn, sam, ct);

        var now = DateTime.UtcNow;
        var pitches = new[]
        {
            Pitch.Reconstitute(Guid.NewGuid(), alexId, "Alex Rivera",
                "Revamp Onboarding Flow",
                "Users drop off during the first-time setup because it has too many steps and no contextual help.",
                2, PitchStatus.Shipped, true, now.AddDays(-30), now.AddDays(-5)),

            Pitch.Reconstitute(Guid.NewGuid(), alexId, "Alex Rivera",
                "AI-Powered Sprint Retrospective Assistant",
                "Teams waste time structuring retros; an AI assistant could auto-summarise themes and action items from meeting notes.",
                6, PitchStatus.Betted, true, now.AddDays(-20), now.AddDays(-10)),

            Pitch.Reconstitute(Guid.NewGuid(), alexId, "Alex Rivera",
                "Public Roadmap Board",
                "Customers repeatedly ask about upcoming features with no self-service way to track planned work.",
                2, PitchStatus.Shaped, true, now.AddDays(-15), now.AddDays(-8)),

            Pitch.Reconstitute(Guid.NewGuid(), alexId, "Alex Rivera",
                "Dark Mode for Dashboard",
                "Power users working late request dark mode; it is a quick win with high satisfaction impact.",
                2, PitchStatus.Draft, false, now.AddDays(-3), now.AddDays(-3)),

            Pitch.Reconstitute(Guid.NewGuid(), samId, "Samira Patel",
                "Mobile-First Notification Centre",
                "Push notifications for key cycle events are absent on mobile; users miss critical stage transitions.",
                6, PitchStatus.Shipped, true, now.AddDays(-45), now.AddDays(-2)),

            Pitch.Reconstitute(Guid.NewGuid(), samId, "Samira Patel",
                "Slack Integration for Pitch Updates",
                "Engineering leads want real-time Slack pings when a pitch moves from Shaped to Betted.",
                2, PitchStatus.Draft, false, now.AddDays(-1), now.AddDays(-1)),
        };

        foreach (var pitch in pitches)
            await InsertPitchAsync(conn, pitch, ct);
    }

    private static async Task InsertUserAsync(NpgsqlConnection conn, User user, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO users (id, email, full_name, password_hash, role, created_at)
            VALUES (@id, @email, @full_name, @password_hash, @role, @created_at)
            ON CONFLICT (email) DO NOTHING;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("email", user.Email);
        cmd.Parameters.AddWithValue("full_name", user.FullName);
        cmd.Parameters.AddWithValue("password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("role", user.Role);
        cmd.Parameters.AddWithValue("created_at", user.CreatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertPitchAsync(NpgsqlConnection conn, Pitch pitch, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO pitches (id, user_id, title, problem_statement, appetite_weeks, status, is_public, created_at, updated_at)
            VALUES (@id, @user_id, @title, @problem_statement, @appetite_weeks, @status, @is_public, @created_at, @updated_at);
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", pitch.Id);
        cmd.Parameters.AddWithValue("user_id", pitch.UserId);
        cmd.Parameters.AddWithValue("title", pitch.Title);
        cmd.Parameters.AddWithValue("problem_statement", pitch.ProblemStatement);
        cmd.Parameters.AddWithValue("appetite_weeks", pitch.AppetiteWeeks);
        cmd.Parameters.AddWithValue("status", (int)pitch.Status);
        cmd.Parameters.AddWithValue("is_public", pitch.IsPublic);
        cmd.Parameters.AddWithValue("created_at", pitch.CreatedAt);
        cmd.Parameters.AddWithValue("updated_at", pitch.UpdatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
