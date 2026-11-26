using Hora.Domain.Entities;
using Hora.Domain.Enums;
using Hora.Domain.Exceptions;
using FluentAssertions;

namespace Hora.Domain.Tests;

public class PitchTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidTitle = "Revamp onboarding flow";
    private const string ValidProblem = "Users drop off during the first-time setup because it has too many steps.";

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Create_WithValidAppetite_ShouldSucceed(int appetite)
    {
        var pitch = Pitch.Create(ValidUserId, "Alex Rivera", ValidTitle, ValidProblem, appetite);
        pitch.AppetiteWeeks.Should().Be(appetite);
        pitch.Status.Should().Be(PitchStatus.Draft);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidAppetite_ShouldThrowDomainValidationException(int appetite)
    {
        var act = () => Pitch.Create(ValidUserId, "Alex Rivera", ValidTitle, ValidProblem, appetite);
        act.Should().Throw<DomainValidationException>()
           .WithMessage("*AppetiteWeeks must be 2*");
    }

    [Fact]
    public void Create_WithTitleTooShort_ShouldThrow()
    {
        var act = () => Pitch.Create(ValidUserId, "Alex", "Hi", ValidProblem, 2);
        act.Should().Throw<DomainValidationException>().WithMessage("*Title*");
    }

    [Fact]
    public void Create_WithTitleTooLong_ShouldThrow()
    {
        var longTitle = new string('A', 121);
        var act = () => Pitch.Create(ValidUserId, "Alex", longTitle, ValidProblem, 2);
        act.Should().Throw<DomainValidationException>().WithMessage("*Title*");
    }

    [Fact]
    public void Create_NewPitchIsAlwaysDraftAndNotPublic()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        pitch.Status.Should().Be(PitchStatus.Draft);
        pitch.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void TransitionTo_DraftToShaped_ShouldSucceed()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.Status.Should().Be(PitchStatus.Shaped);
    }

    [Fact]
    public void TransitionTo_DraftToShipped_ShouldThrow()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        var act = () => pitch.TransitionTo(PitchStatus.Shipped);
        act.Should().Throw<DomainValidationException>().WithMessage("*Invalid status transition*");
    }

    [Fact]
    public void TransitionTo_DraftToBetted_ShouldThrow()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        var act = () => pitch.TransitionTo(PitchStatus.Betted);
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void TransitionTo_ShippedToDraft_ShouldThrow()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 6);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.TransitionTo(PitchStatus.Betted);
        pitch.TransitionTo(PitchStatus.Shipped);

        var act = () => pitch.TransitionTo(PitchStatus.Draft);
        act.Should().Throw<DomainValidationException>().WithMessage("*Shipped*");
    }

    [Fact]
    public void TransitionTo_ShapedBackToDraft_ShouldSucceed()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.TransitionTo(PitchStatus.Draft);
        pitch.Status.Should().Be(PitchStatus.Draft);
    }

    [Fact]
    public void TransitionTo_BettedBackToShaped_ShouldSucceed()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.TransitionTo(PitchStatus.Betted);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.Status.Should().Be(PitchStatus.Shaped);
    }

    [Fact]
    public void SetPublic_OnDraftPitch_ShouldThrow()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        var act = () => pitch.SetPublic(true);
        act.Should().Throw<DomainValidationException>().WithMessage("*Draft*");
    }

    [Fact]
    public void SetPublic_OnShapedPitch_ShouldSucceed()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 2);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.SetPublic(true);
        pitch.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void FullLifecycle_DraftToShipped_ShouldSucceed()
    {
        var pitch = Pitch.Create(ValidUserId, "Alex", ValidTitle, ValidProblem, 6);
        pitch.TransitionTo(PitchStatus.Shaped);
        pitch.TransitionTo(PitchStatus.Betted);
        pitch.TransitionTo(PitchStatus.Shipped);
        pitch.Status.Should().Be(PitchStatus.Shipped);
    }
}
