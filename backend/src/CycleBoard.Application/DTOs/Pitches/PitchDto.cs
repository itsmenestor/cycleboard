using CycleBoard.Domain.Enums;

namespace CycleBoard.Application.DTOs.Pitches;

public sealed record PitchDto(
    Guid Id,
    Guid UserId,
    string AuthorName,
    string Title,
    string ProblemStatement,
    int AppetiteWeeks,
    PitchStatus Status,
    string StatusName,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
