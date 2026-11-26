using Hora.Application.DTOs.Pitches;
using Hora.Domain.Enums;

namespace Hora.Application.Services;

public interface IPitchService
{
    Task<IReadOnlyList<PitchDto>> GetUserPitchesAsync(Guid userId, PitchStatus? statusFilter = null, CancellationToken ct = default);
    Task<IReadOnlyList<PitchDto>> GetPublicPitchesAsync(int? appetiteFilter = null, CancellationToken ct = default);
    Task<PitchDto?> GetPitchByIdAsync(Guid pitchId, Guid requestingUserId, CancellationToken ct = default);
    Task<PitchDto> CreatePitchAsync(Guid userId, string authorName, CreatePitchRequest request, CancellationToken ct = default);
    Task<PitchDto> UpdatePitchAsync(Guid pitchId, Guid requestingUserId, UpdatePitchRequest request, CancellationToken ct = default);
    Task DeletePitchAsync(Guid pitchId, Guid requestingUserId, CancellationToken ct = default);
}
