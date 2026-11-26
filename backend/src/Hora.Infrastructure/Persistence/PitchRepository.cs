using Hora.Application.Interfaces;
using Hora.Domain.Entities;
using Hora.Domain.Enums;
using Npgsql;

namespace Hora.Infrastructure.Persistence;

public sealed class PitchRepository : IPitchRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PitchRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Pitch>> GetByUserIdAsync(Guid userId, PitchStatus? statusFilter = null, CancellationToken ct = default)
    {
        var sql = """
            SELECT p.id, p.user_id, u.full_name, p.title, p.problem_statement,
                   p.appetite_weeks, p.status, p.is_public, p.created_at, p.updated_at
            FROM pitches p
            JOIN users u ON u.id = p.user_id
            WHERE p.user_id = @user_id
            """;

        if (statusFilter.HasValue) sql += " AND p.status = @status";
        sql += " ORDER BY p.created_at DESC;";

        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", userId);
        if (statusFilter.HasValue)
            cmd.Parameters.AddWithValue("status", (int)statusFilter.Value);

        var results = new List<Pitch>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add(MapPitch(reader));

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Pitch>> GetPublicAsync(int? appetiteFilter = null, CancellationToken ct = default)
    {
        var sql = """
            SELECT p.id, p.user_id, u.full_name, p.title, p.problem_statement,
                   p.appetite_weeks, p.status, p.is_public, p.created_at, p.updated_at
            FROM pitches p
            JOIN users u ON u.id = p.user_id
            WHERE p.is_public = TRUE
            """;

        if (appetiteFilter.HasValue) sql += " AND p.appetite_weeks = @appetite_weeks";
        sql += " ORDER BY p.updated_at DESC;";

        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (appetiteFilter.HasValue)
            cmd.Parameters.AddWithValue("appetite_weeks", appetiteFilter.Value);

        var results = new List<Pitch>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add(MapPitch(reader));

        return results.AsReadOnly();
    }

    public async Task<Pitch?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT p.id, p.user_id, u.full_name, p.title, p.problem_statement,
                   p.appetite_weeks, p.status, p.is_public, p.created_at, p.updated_at
            FROM pitches p
            JOIN users u ON u.id = p.user_id
            WHERE p.id = @id;
            """;

        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapPitch(reader) : null;
    }

    public async Task AddAsync(Pitch pitch, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO pitches (id, user_id, title, problem_statement, appetite_weeks, status, is_public, created_at, updated_at)
            VALUES (@id, @user_id, @title, @problem_statement, @appetite_weeks, @status, @is_public, @created_at, @updated_at);
            """;
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        BindPitchParams(cmd, pitch);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(Pitch pitch, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE pitches
            SET title            = @title,
                problem_statement = @problem_statement,
                appetite_weeks   = @appetite_weeks,
                status           = @status,
                is_public        = @is_public,
                updated_at       = @updated_at
            WHERE id = @id AND user_id = @user_id;
            """;
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        BindPitchParams(cmd, pitch);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM pitches WHERE id = @id AND user_id = @user_id;";
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("user_id", userId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void BindPitchParams(NpgsqlCommand cmd, Pitch pitch)
    {
        cmd.Parameters.AddWithValue("id", pitch.Id);
        cmd.Parameters.AddWithValue("user_id", pitch.UserId);
        cmd.Parameters.AddWithValue("title", pitch.Title);
        cmd.Parameters.AddWithValue("problem_statement", pitch.ProblemStatement);
        cmd.Parameters.AddWithValue("appetite_weeks", pitch.AppetiteWeeks);
        cmd.Parameters.AddWithValue("status", (int)pitch.Status);
        cmd.Parameters.AddWithValue("is_public", pitch.IsPublic);
        cmd.Parameters.AddWithValue("created_at", pitch.CreatedAt);
        cmd.Parameters.AddWithValue("updated_at", pitch.UpdatedAt);
    }

    private static Pitch MapPitch(NpgsqlDataReader r) =>
        Pitch.Reconstitute(
            r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.GetInt32(5), (PitchStatus)r.GetInt32(6), r.GetBoolean(7),
            r.GetDateTime(8), r.GetDateTime(9)
        );
}
