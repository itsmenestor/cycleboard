using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hora.Domain.Entities;
using Hora.Infrastructure.Security;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace Hora.Infrastructure.Tests;

public class JwtTokenGeneratorTests
{
    private const string Secret = "SuperSecretKeyForCycleboardTestingMustBe32CharsLong!";
    private const string Issuer = "CycleboardApi";
    private const string Audience = "CycleboardClient";
    private readonly JwtTokenGenerator _sut = new(Secret, Issuer, Audience, expirationMinutes: 60);

    [Fact]
    public void GenerateToken_ShouldReturnValidJwt()
    {
        var user = User.Reconstitute(Guid.NewGuid(), "alex@test.com", "Alex Rivera", "hash", "Lead", DateTime.UtcNow);

        var token = _sut.GenerateToken(user);
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(token, validationParams, out var validatedToken);

        validatedToken.Should().BeOfType<JwtSecurityToken>();
        principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(JwtRegisteredClaimNames.Email)!.Value.Should().Be(user.Email);
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Lead");
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        var user = User.Reconstitute(Guid.NewGuid(), "alex@test.com", "Alex Rivera", "hash", "Lead", DateTime.UtcNow);
        var token = _sut.GenerateToken(user);

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(30));
    }
}
