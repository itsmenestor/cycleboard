using CycleBoard.Domain.Enums;

namespace CycleBoard.Application.DTOs.Pitches;

public sealed record UpdatePitchRequest(
    string Title,
    string ProblemStatement,
    int AppetiteWeeks,
    PitchStatus Status,
    bool IsPublic
);
