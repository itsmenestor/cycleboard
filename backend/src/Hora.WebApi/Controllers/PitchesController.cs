using System.Security.Claims;
using Hora.Application.DTOs.Pitches;
using Hora.Application.Services;
using Hora.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hora.WebApi.Controllers;

[ApiController]
[Route("api/pitches")]
[Authorize]
public sealed class PitchesController : ControllerBase
{
    private readonly IPitchService _pitchService;

    public PitchesController(IPitchService pitchService) => _pitchService = pitchService;

    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PitchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PitchDto>>> GetPublic([FromQuery] int? appetite, CancellationToken ct)
    {
        var result = await _pitchService.GetPublicPitchesAsync(appetite, ct);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PitchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PitchDto>>> GetMyPitches([FromQuery] PitchStatus? status, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _pitchService.GetUserPitchesAsync(userId, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PitchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PitchDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _pitchService.GetPitchByIdAsync(id, userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PitchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PitchDto>> Create([FromBody] CreatePitchRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var authorName = User.FindFirstValue("fullName") ?? "Unknown";
        var result = await _pitchService.CreatePitchAsync(userId, authorName, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PitchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PitchDto>> Update(Guid id, [FromBody] UpdatePitchRequest request, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        var result = await _pitchService.UpdatePitchAsync(id, userId, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetAuthenticatedUserId();
        await _pitchService.DeletePitchAsync(id, userId, ct);
        return NoContent();
    }

    private Guid GetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id
            : throw new UnauthorizedAccessException("Invalid or missing user identity claim.");
    }
}
