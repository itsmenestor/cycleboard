using CycleBoard.Application.Interfaces;
using Npgsql;

namespace CycleBoard.Infrastructure.Persistence;

public sealed class SqlSchemaInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlSchemaInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        const string sql = """
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
            """;

        await using var conn = (NpgsqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
