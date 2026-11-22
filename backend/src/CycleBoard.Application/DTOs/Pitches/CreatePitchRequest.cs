namespace CycleBoard.Application.DTOs.Pitches;

public sealed record CreatePitchRequest(
    string Title,
    string ProblemStatement,
    int AppetiteWeeks,
    bool IsPublic = false
);
