using Hora.Application.DTOs.Pitches;
using Hora.Application.Interfaces;
using Hora.Domain.Enums;
using Hora.Domain.Exceptions;

namespace Hora.Application.Services;

public sealed class PitchService : IPitchService
{
    private readonly IPitchRepository _pitchRepository;
    private readonly IUserRepository _userRepository;

    public PitchService(IPitchRepository pitchRepository, IUserRepository userRepository)
    {
        _pitchRepository = pitchRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<PitchDto>> GetUserPitchesAsync(Guid userId, PitchStatus? statusFilter = null, CancellationToken ct = default)
    {
        var pitches = await _pitchRepository.GetByUserIdAsync(userId, statusFilter, ct);
        return pitches.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<PitchDto>> GetPublicPitchesAsync(int? appetiteFilter = null, CancellationToken ct = default)
    {
        var pitches = await _pitchRepository.GetPublicAsync(appetiteFilter, ct);
        return pitches.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<PitchDto?> GetPitchByIdAsync(Guid pitchId, Guid requestingUserId, CancellationToken ct = default)
    {
        var pitch = await _pitchRepository.GetByIdAsync(pitchId, ct);
        if (pitch is null) return null;
        if (pitch.UserId != requestingUserId)
            throw new ForbiddenAccessException();
        return MapToDto(pitch);
    }

    public async Task<PitchDto> CreatePitchAsync(Guid userId, string authorName, CreatePitchRequest request, CancellationToken ct = default)
    {
        var pitch = Domain.Entities.Pitch.Create(userId, authorName, request.Title, request.ProblemStatement, request.AppetiteWeeks);
        await _pitchRepository.AddAsync(pitch, ct);
        return MapToDto(pitch);
    }

    public async Task<PitchDto> UpdatePitchAsync(Guid pitchId, Guid requestingUserId, UpdatePitchRequest request, CancellationToken ct = default)
    {
        var pitch = await _pitchRepository.GetByIdAsync(pitchId, ct)
            ?? throw new KeyNotFoundException($"Pitch '{pitchId}' not found.");

        if (pitch.UserId != requestingUserId)
            throw new ForbiddenAccessException();

        pitch.Update(request.Title, request.ProblemStatement, request.AppetiteWeeks, request.Status, request.IsPublic);
        await _pitchRepository.UpdateAsync(pitch, ct);
        return MapToDto(pitch);
    }

    public async Task DeletePitchAsync(Guid pitchId, Guid requestingUserId, CancellationToken ct = default)
    {
        var pitch = await _pitchRepository.GetByIdAsync(pitchId, ct)
            ?? throw new KeyNotFoundException($"Pitch '{pitchId}' not found.");

        if (pitch.UserId != requestingUserId)
            throw new ForbiddenAccessException();

        await _pitchRepository.DeleteAsync(pitchId, requestingUserId, ct);
    }

    private static PitchDto MapToDto(Domain.Entities.Pitch p) => new(
        p.Id, p.UserId, p.AuthorName, p.Title, p.ProblemStatement,
        p.AppetiteWeeks, p.Status, p.Status.ToString(), p.IsPublic,
        p.CreatedAt, p.UpdatedAt
    );
}
