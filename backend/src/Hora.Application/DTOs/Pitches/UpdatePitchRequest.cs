using Hora.Domain.Enums;

namespace Hora.Application.DTOs.Pitches;

public sealed record UpdatePitchRequest(
    string Title,
    string ProblemStatement,
    int AppetiteWeeks,
    PitchStatus Status,
    bool IsPublic
);
