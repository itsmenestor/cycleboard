using CycleBoard.Domain.Enums;
using CycleBoard.Domain.Exceptions;

namespace CycleBoard.Domain.Entities;

public sealed class Pitch
{
    private static readonly HashSet<int> ValidAppetites = [2, 6];

    private static readonly Dictionary<PitchStatus, HashSet<PitchStatus>> ValidTransitions = new()
    {
        [PitchStatus.Draft]   = [PitchStatus.Draft,   PitchStatus.Shaped],
        [PitchStatus.Shaped]  = [PitchStatus.Shaped,  PitchStatus.Betted,  PitchStatus.Draft],
        [PitchStatus.Betted]  = [PitchStatus.Betted,  PitchStatus.Shipped, PitchStatus.Shaped],
        [PitchStatus.Shipped] = [PitchStatus.Shipped]
    };

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string ProblemStatement { get; private set; } = string.Empty;
    public int AppetiteWeeks { get; private set; }
    public PitchStatus Status { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Pitch() { }

    public static Pitch Create(Guid userId, string authorName, string title, string problemStatement, int appetiteWeeks)
    {
        if (userId == Guid.Empty)
            throw new DomainValidationException("UserId cannot be empty.");

        ValidateAppetite(appetiteWeeks);
        ValidateTitle(title);
        ValidateProblemStatement(problemStatement);

        var now = DateTime.UtcNow;
        return new Pitch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AuthorName = authorName?.Trim() ?? string.Empty,
            Title = title.Trim(),
            ProblemStatement = problemStatement.Trim(),
            AppetiteWeeks = appetiteWeeks,
            Status = PitchStatus.Draft,
            IsPublic = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string problemStatement, int appetiteWeeks, PitchStatus newStatus, bool isPublic)
    {
        if (Status == PitchStatus.Shipped)
        {
            if (title.Trim() != Title || appetiteWeeks != AppetiteWeeks)
                throw new DomainValidationException("Title and appetite cannot be changed once a pitch is Shipped.");
        }
        else
        {
            ValidateTitle(title);
            ValidateProblemStatement(problemStatement);
            ValidateAppetite(appetiteWeeks);
        }

        TransitionTo(newStatus);

        if (isPublic && Status == PitchStatus.Draft)
            throw new DomainValidationException("A Draft pitch cannot be made public.");

        Title = title.Trim();
        ProblemStatement = problemStatement.Trim();
        AppetiteWeeks = appetiteWeeks;
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransitionTo(PitchStatus newStatus)
    {
        if (!ValidTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            var allowedList = string.Join(", ", ValidTransitions[Status]);
            throw new DomainValidationException(
                $"Invalid status transition from '{Status}' to '{newStatus}'. Allowed transitions from '{Status}': [{allowedList}].");
        }

        Status = newStatus;
    }

    public void SetPublic(bool isPublic)
    {
        if (isPublic && Status == PitchStatus.Draft)
            throw new DomainValidationException("A Draft pitch cannot be made public.");
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Pitch Reconstitute(Guid id, Guid userId, string authorName, string title, string problemStatement,
        int appetiteWeeks, PitchStatus status, bool isPublic, DateTime createdAt, DateTime updatedAt)
    {
        return new Pitch
        {
            Id = id,
            UserId = userId,
            AuthorName = authorName,
            Title = title,
            ProblemStatement = problemStatement,
            AppetiteWeeks = appetiteWeeks,
            Status = status,
            IsPublic = isPublic,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc)
        };
    }

    private static void ValidateTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length < 5 || trimmed.Length > 120)
            throw new DomainValidationException("Title must be between 5 and 120 characters.");
    }

    private static void ValidateProblemStatement(string ps)
    {
        var trimmed = ps?.Trim() ?? string.Empty;
        if (trimmed.Length > 0 && trimmed.Length < 10)
            throw new DomainValidationException("Problem statement must be at least 10 characters, or leave it empty.");
        if (trimmed.Length > 2000)
            throw new DomainValidationException("Problem statement must not exceed 2000 characters.");
    }

    private static void ValidateAppetite(int appetiteWeeks)
    {
        if (!ValidAppetites.Contains(appetiteWeeks))
            throw new DomainValidationException(
                $"AppetiteWeeks must be 2 (Small Batch) or 6 (Big Batch). Got: {appetiteWeeks}.");
    }
}
