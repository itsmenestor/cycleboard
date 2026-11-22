using CycleBoard.Application.DTOs.Auth;
using CycleBoard.Application.Interfaces;
using CycleBoard.Domain.Entities;
using CycleBoard.Domain.Exceptions;

namespace CycleBoard.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
            throw new DomainValidationException($"An account with email '{request.Email}' already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(Guid.NewGuid(), request.Email, request.FullName, passwordHash);

        await _userRepository.AddAsync(user, ct);

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.FullName, user.Role));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.FullName, user.Role));
    }

    public async Task<MeResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new KeyNotFoundException($"User with ID '{userId}' not found.");

        return new MeResponse(user.Id, user.Email, user.FullName, user.Role, user.CreatedAt);
    }
}
