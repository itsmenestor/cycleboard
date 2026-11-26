using Hora.Domain.Entities;

namespace Hora.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
