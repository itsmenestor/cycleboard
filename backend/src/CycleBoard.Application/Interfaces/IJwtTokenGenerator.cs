using CycleBoard.Domain.Entities;

namespace CycleBoard.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
