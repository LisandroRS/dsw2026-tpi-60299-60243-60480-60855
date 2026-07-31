using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Dsw2026Tpi.Api.Controllers
{
    [Route("api/specialties")]
    [Authorize(Policy = Policies.AdminPolicy)]
    public class SpecialityController : AppController
    {
        private readonly ISpecialityService _service;

        public SpecialityController(ISpecialityService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null)
        {
            var specialities = await _service.GetAll(pageSize, pageIndex, name);
            return Ok(specialities);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var speciality = await _service.GetById(id);
            return speciality == null ? NotFound() : Ok(speciality);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] SpecialityModel.Request request)
        {
            var speciality = await _service.Create(request);
            return CreatedAtAction(nameof(GetById), new { id = speciality.id }, speciality);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SpecialityModel.Request request)
        {
            var speciality = await _service.Update(id, request);
            return speciality == null ? NotFound() : Ok(speciality);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.Delete(id);
            return deleted ? Ok() : NotFound();
        }
    }
}
