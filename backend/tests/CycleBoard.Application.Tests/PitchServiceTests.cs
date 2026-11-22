using CycleBoard.Application.DTOs.Pitches;
using CycleBoard.Application.Interfaces;
using CycleBoard.Application.Services;
using CycleBoard.Domain.Entities;
using CycleBoard.Domain.Enums;
using CycleBoard.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace CycleBoard.Application.Tests;

public class PitchServiceTests
{
    private readonly IPitchRepository _pitchRepo = Substitute.For<IPitchRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly PitchService _sut;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();

    public PitchServiceTests()
    {
        _sut = new PitchService(_pitchRepo, _userRepo);
    }

    private static Pitch MakePitch(Guid ownerId, PitchStatus status = PitchStatus.Draft) =>
        Pitch.Reconstitute(Guid.NewGuid(), ownerId, "Alex", "Title Here Now",
            "Problem statement must be at least ten chars.", 2, status, false,
            DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task CreatePitch_ShouldAddAndReturnDto()
    {
        _pitchRepo.AddAsync(Arg.Any<Pitch>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.CreatePitchAsync(_ownerId, "Alex",
            new CreatePitchRequest("Revamp Onboarding Now", "Users drop off because setup is too complex.", 2));

        result.Title.Should().Be("Revamp Onboarding Now");
        result.Status.Should().Be(PitchStatus.Draft);
        await _pitchRepo.Received(1).AddAsync(Arg.Any<Pitch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePitch_ByNonOwner_ShouldThrowForbiddenAccessException()
    {
        var pitch = MakePitch(_ownerId);
        _pitchRepo.GetByIdAsync(pitch.Id, Arg.Any<CancellationToken>()).Returns(pitch);

        var act = () => _sut.UpdatePitchAsync(pitch.Id, _strangerId,
            new UpdatePitchRequest("Title Here Now", "Problem statement must be at least ten chars.", 2, PitchStatus.Draft, false));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task DeletePitch_ByNonOwner_ShouldThrowForbiddenAccessException()
    {
        var pitch = MakePitch(_ownerId);
        _pitchRepo.GetByIdAsync(pitch.Id, Arg.Any<CancellationToken>()).Returns(pitch);

        var act = () => _sut.DeletePitchAsync(pitch.Id, _strangerId);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task DeletePitch_ByOwner_ShouldCallRepository()
    {
        var pitch = MakePitch(_ownerId);
        _pitchRepo.GetByIdAsync(pitch.Id, Arg.Any<CancellationToken>()).Returns(pitch);

        await _sut.DeletePitchAsync(pitch.Id, _ownerId);

        await _pitchRepo.Received(1).DeleteAsync(pitch.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPitchById_ByNonOwner_ShouldThrowForbidden()
    {
        var pitch = MakePitch(_ownerId);
        _pitchRepo.GetByIdAsync(pitch.Id, Arg.Any<CancellationToken>()).Returns(pitch);

        var act = () => _sut.GetPitchByIdAsync(pitch.Id, _strangerId);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetPitchById_NotFound_ShouldReturnNull()
    {
        _pitchRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Pitch?)null);

        var result = await _sut.GetPitchByIdAsync(Guid.NewGuid(), _ownerId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePitch_ByOwner_ShouldUpdateAndReturnDto()
    {
        var pitch = MakePitch(_ownerId);
        _pitchRepo.GetByIdAsync(pitch.Id, Arg.Any<CancellationToken>()).Returns(pitch);
        _pitchRepo.UpdateAsync(Arg.Any<Pitch>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.UpdatePitchAsync(pitch.Id, _ownerId,
            new UpdatePitchRequest("Title Here Now Updated", "Problem statement must be at least ten chars.", 2, PitchStatus.Shaped, false));

        result.Status.Should().Be(PitchStatus.Shaped);
        await _pitchRepo.Received(1).UpdateAsync(Arg.Any<Pitch>(), Arg.Any<CancellationToken>());
    }
}
