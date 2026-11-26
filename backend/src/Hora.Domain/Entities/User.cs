using Hora.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace Hora.Domain.Entities;

public sealed class User
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(Guid id, string email, string fullName, string passwordHash, string role = "Lead")
    {
        if (id == Guid.Empty)
            throw new DomainValidationException("User ID cannot be empty.");

        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!EmailRegex.IsMatch(normalizedEmail))
            throw new DomainValidationException($"'{email}' is not a valid email address.");

        var trimmedName = fullName?.Trim() ?? string.Empty;
        if (trimmedName.Length < 2 || trimmedName.Length > 100)
            throw new DomainValidationException("Full name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainValidationException("Password hash cannot be empty.");

        return new User
        {
            Id = id,
            Email = normalizedEmail,
            FullName = trimmedName,
            PasswordHash = passwordHash,
            Role = string.IsNullOrWhiteSpace(role) ? "Lead" : role.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static User Reconstitute(Guid id, string email, string fullName, string passwordHash, string role, DateTime createdAt)
    {
        return new User
        {
            Id = id,
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc)
        };
    }
}
