using System.Security.Claims;
using CycleBoard.Application.DTOs.Pitches;
using CycleBoard.Application.Services;
using CycleBoard.Domain.Enums;
using CycleBoard.WebApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CycleBoard.WebApi.Tests;

public class PitchesControllerTests
{
    private readonly IPitchService _pitchService = Substitute.For<IPitchService>();
    private readonly PitchesController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public PitchesControllerTests()
    {
        _sut = new PitchesController(_pitchService);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("fullName", "Alex Rivera")
        };
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    private static PitchDto MakePitchDto(Guid? userId = null) => new(
        Guid.NewGuid(), userId ?? Guid.NewGuid(), "Alex Rivera",
        "Revamp Onboarding", "Users drop off because setup is too complex.",
        2, PitchStatus.Draft, "Draft", false, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetPublic_Returns200OK()
    {
        IReadOnlyList<PitchDto> pitches = new[] { MakePitchDto() }.ToList().AsReadOnly();
        _pitchService.GetPublicPitchesAsync(null, Arg.Any<CancellationToken>()).Returns(pitches);

        var result = await _sut.GetPublic(null, default);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyPitches_Returns200OK()
    {
        IReadOnlyList<PitchDto> pitches = new[] { MakePitchDto(_userId) }.ToList().AsReadOnly();
        _pitchService.GetUserPitchesAsync(_userId, null, Arg.Any<CancellationToken>()).Returns(pitches);

        var result = await _sut.GetMyPitches(null, default);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _pitchService.GetPitchByIdAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>()).Returns((PitchDto?)null);

        var result = await _sut.GetById(Guid.NewGuid(), default);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201Created()
    {
        var dto = MakePitchDto(_userId);
        _pitchService.CreatePitchAsync(_userId, Arg.Any<string>(), Arg.Any<CreatePitchRequest>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Create(
            new CreatePitchRequest("Revamp Onboarding", "Users drop off because setup is too complex.", 2),
            default);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Delete_ValidId_Returns204NoContent()
    {
        _pitchService.DeletePitchAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.Delete(Guid.NewGuid(), default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_ValidRequest_Returns200OK()
    {
        var dto = MakePitchDto(_userId);
        _pitchService.UpdatePitchAsync(Arg.Any<Guid>(), _userId, Arg.Any<UpdatePitchRequest>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Update(Guid.NewGuid(),
            new UpdatePitchRequest("Revamp Onboarding", "Users drop off because setup is too complex.", 2, PitchStatus.Draft, false),
            default);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
