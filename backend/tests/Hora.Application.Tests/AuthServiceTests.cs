using Hora.Application.DTOs.Auth;
using Hora.Application.Interfaces;
using Hora.Application.Services;
using Hora.Domain.Entities;
using Hora.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Hora.Application.Tests;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtGen = Substitute.For<IJwtTokenGenerator>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepo, _passwordHasher, _jwtGen);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowDomainValidationException()
    {
        var existingUser = User.Reconstitute(Guid.NewGuid(), "alex@test.com", "Alex", "hash", "Lead", DateTime.UtcNow);
        _userRepo.GetByEmailAsync("alex@test.com", Arg.Any<CancellationToken>()).Returns(existingUser);

        var act = () => _sut.RegisterAsync(new RegisterRequest("alex@test.com", "Alex Rivera", "Password1!"));
        await act.Should().ThrowAsync<DomainValidationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Register_WithUniqueEmail_ShouldReturnAuthResponse()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Hash("Password1!").Returns("hashed_pw");
        _jwtGen.GenerateToken(Arg.Any<User>()).Returns("jwt_token");

        var result = await _sut.RegisterAsync(new RegisterRequest("new@test.com", "New User", "Password1!"));

        result.Token.Should().Be("jwt_token");
        result.User.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldThrowUnauthorized()
    {
        var user = User.Reconstitute(Guid.NewGuid(), "alex@test.com", "Alex", "hash", "Lead", DateTime.UtcNow);
        _userRepo.GetByEmailAsync("alex@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrongpass", "hash").Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("alex@test.com", "wrongpass"));
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid*");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldThrowUnauthorized()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nobody@test.com", "pass"));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var user = User.Reconstitute(Guid.NewGuid(), "alex@test.com", "Alex Rivera", "hash", "Lead", DateTime.UtcNow);
        _userRepo.GetByEmailAsync("alex@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password1!", "hash").Returns(true);
        _jwtGen.GenerateToken(user).Returns("valid_jwt");

        var result = await _sut.LoginAsync(new LoginRequest("alex@test.com", "Password1!"));

        result.Token.Should().Be("valid_jwt");
        result.User.FullName.Should().Be("Alex Rivera");
    }

    [Fact]
    public async Task GetCurrentUser_WithValidId_ShouldReturnMeResponse()
    {
        var userId = Guid.NewGuid();
        var user = User.Reconstitute(userId, "alex@test.com", "Alex Rivera", "hash", "Lead", DateTime.UtcNow);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetCurrentUserAsync(userId);

        result.Id.Should().Be(userId);
        result.Email.Should().Be("alex@test.com");
    }
}
