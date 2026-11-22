using System.Security.Claims;
using CycleBoard.Application.DTOs.Auth;
using CycleBoard.Application.Services;
using CycleBoard.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CycleBoard.WebApi.Tests;

public class AuthControllerTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService);
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201Created()
    {
        var response = new AuthResponse("token", new UserDto(Guid.NewGuid(), "alex@test.com", "Alex Rivera", "Lead"));
        _authService.RegisterAsync(Arg.Any<RegisterRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Register(new RegisterRequest("alex@test.com", "Alex Rivera", "Password1!"), default);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200OK()
    {
        var response = new AuthResponse("token", new UserDto(Guid.NewGuid(), "alex@test.com", "Alex Rivera", "Lead"));
        _authService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Login(new LoginRequest("alex@test.com", "Password1!"), default);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result.Result!).Value.Should().Be(response);
    }

    [Fact]
    public async Task Me_AuthenticatedUser_Returns200OK()
    {
        var userId = Guid.NewGuid();
        var meResponse = new MeResponse(userId, "alex@test.com", "Alex Rivera", "Lead", DateTime.UtcNow);
        _authService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>()).Returns(meResponse);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
        };

        var result = await _sut.Me(default);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
