using System.Data.Common;

namespace Hora.Application.Interfaces;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
