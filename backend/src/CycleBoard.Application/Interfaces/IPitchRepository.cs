using CycleBoard.Domain.Entities;
using CycleBoard.Domain.Enums;

namespace CycleBoard.Application.Interfaces;

public interface IPitchRepository
{
    Task<IReadOnlyList<Pitch>> GetByUserIdAsync(Guid userId, PitchStatus? statusFilter = null, CancellationToken ct = default);
    Task<IReadOnlyList<Pitch>> GetPublicAsync(int? appetiteFilter = null, CancellationToken ct = default);
    Task<Pitch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Pitch pitch, CancellationToken ct = default);
    Task UpdateAsync(Pitch pitch, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
