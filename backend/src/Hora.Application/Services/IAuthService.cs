using Hora.Application.DTOs.Auth;

namespace Hora.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<MeResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
