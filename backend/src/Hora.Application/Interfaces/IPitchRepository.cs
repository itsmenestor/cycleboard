using Hora.Domain.Entities;
using Hora.Domain.Enums;

namespace Hora.Application.Interfaces;

public interface IPitchRepository
{
    Task<IReadOnlyList<Pitch>> GetByUserIdAsync(Guid userId, PitchStatus? statusFilter = null, CancellationToken ct = default);
    Task<IReadOnlyList<Pitch>> GetPublicAsync(int? appetiteFilter = null, CancellationToken ct = default);
    Task<Pitch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Pitch pitch, CancellationToken ct = default);
    Task UpdateAsync(Pitch pitch, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
