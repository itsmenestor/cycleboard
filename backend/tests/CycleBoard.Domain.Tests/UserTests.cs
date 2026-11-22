using CycleBoard.Domain.Entities;
using CycleBoard.Domain.Exceptions;
using FluentAssertions;

namespace CycleBoard.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldNormalizeEmailToLowercase()
    {
        var user = User.Create(Guid.NewGuid(), "Alex.Rivera@Example.COM", "Alex Rivera", "hash");
        user.Email.Should().Be("alex.rivera@example.com");
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        var act = () => User.Create(Guid.NewGuid(), "not-an-email", "Alex Rivera", "hash");
        act.Should().Throw<DomainValidationException>().WithMessage("*valid email*");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithShortFullName_ShouldThrow(string name)
    {
        var act = () => User.Create(Guid.NewGuid(), "alex@example.com", name, "hash");
        act.Should().Throw<DomainValidationException>().WithMessage("*Full name*");
    }

    [Fact]
    public void Create_WithFullNameExceeding100Chars_ShouldThrow()
    {
        var longName = new string('A', 101);
        var act = () => User.Create(Guid.NewGuid(), "alex@example.com", longName, "hash");
        act.Should().Throw<DomainValidationException>().WithMessage("*Full name*");
    }

    [Fact]
    public void Create_WithEmptyGuid_ShouldThrow()
    {
        var act = () => User.Create(Guid.Empty, "alex@example.com", "Alex Rivera", "hash");
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithDefaultRole_ShouldBeLead()
    {
        var user = User.Create(Guid.NewGuid(), "alex@example.com", "Alex Rivera", "hash");
        user.Role.Should().Be("Lead");
    }

    [Fact]
    public void Create_ValidUser_ShouldSetCreatedAtUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = User.Create(Guid.NewGuid(), "alex@example.com", "Alex Rivera", "hash");
        user.CreatedAt.Should().BeAfter(before).And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
