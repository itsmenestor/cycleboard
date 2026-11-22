namespace CycleBoard.Application.DTOs.Auth;

public sealed record AuthResponse(string Token, UserDto User);

public sealed record UserDto(Guid Id, string Email, string FullName, string Role);

public sealed record MeResponse(Guid Id, string Email, string FullName, string Role, DateTime CreatedAt);
