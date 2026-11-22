using System.Data.Common;

namespace CycleBoard.Application.Interfaces;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
