using Hora.Application.Interfaces;
using Hora.Domain.Entities;
using Npgsql;

namespace Hora.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT id, email, full_name, password_hash, role, created_at FROM users WHERE id = @id;";
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT id, email, full_name, password_hash, role, created_at FROM users WHERE email = @email;";
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", email.ToLowerInvariant());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapUser(reader) : null;
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO users (id, email, full_name, password_hash, role, created_at)
            VALUES (@id, @email, @full_name, @password_hash, @role, @created_at);
            """;
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("email", user.Email);
        cmd.Parameters.AddWithValue("full_name", user.FullName);
        cmd.Parameters.AddWithValue("password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("role", user.Role);
        cmd.Parameters.AddWithValue("created_at", user.CreatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users LIMIT 1);";
        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        return (bool)(await cmd.ExecuteScalarAsync(ct))!;
    }

    private static User MapUser(NpgsqlDataReader r) =>
        User.Reconstitute(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetDateTime(5));
}
