using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace Dsw2026Tpi.Api.Controllers;

[Route("api/appointments")]
public class AppointmentController : AppController
{
    private readonly IAppointmentService _service;

    public AppointmentController(IAppointmentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = Policies.PatientPolicy)]
    [EnableRateLimiting(RateLimitPolicies.AppointmentBooking)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(
    [FromBody] AppointmentModel.Request request)
    {
        var patientEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(patientEmail))
            return Unauthorized();

        var appointment = await _service.Create(
            request,
            patientEmail);

        return StatusCode(
            StatusCodes.Status201Created,
            appointment);
    }

    [HttpGet("patient")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPatient(
    [FromQuery] long dni)
    {
        var patientEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(patientEmail))
            return Unauthorized();

        var appointments = await _service.GetByPatient(
            dni,
            patientEmail);

        return Ok(appointments);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var patientEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(patientEmail))
            return Unauthorized();

        await _service.Cancel(id, patientEmail);

        return Ok();
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByDate(
    [FromQuery] DateOnly date)
    {
        var appointments = await _service.GetByDate(date);

        return Ok(appointments);
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
    [FromQuery] int pageSize = 10,
    [FromQuery] int pageIndex = 0,
    [FromQuery(Name = "specialtyId")] Guid? specialityId = null,
    [FromQuery] Guid? doctorId = null,
    [FromQuery] long? dni = null,
    [FromQuery] DateOnly? date = null)
    {
        var appointments = await _service.Search(
            pageSize,
            pageIndex,
            specialityId,
            doctorId,
            dni,
            date);

        return Ok(appointments);
    }
}