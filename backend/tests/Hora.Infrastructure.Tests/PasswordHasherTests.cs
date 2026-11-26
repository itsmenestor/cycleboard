using Hora.Infrastructure.Security;
using FluentAssertions;

namespace Hora.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = _sut.Hash("Password1!");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_SamePlaintext_ShouldProduceDifferentHashes()
    {
        var hash1 = _sut.Hash("Password1!");
        var hash2 = _sut.Hash("Password1!");
        hash1.Should().NotBe(hash2, "BCrypt uses a unique salt per hash");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple");
        _sut.Verify("CorrectHorseBatteryStaple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple");
        _sut.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
