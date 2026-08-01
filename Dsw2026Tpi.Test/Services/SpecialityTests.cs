using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Dsw2026Tpi.Test.Services;

public class SpecialityServiceTests
{
    private readonly IPersistence _persistence;
    private readonly SpecialityService _service;

    public SpecialityServiceTests()
    {
        _persistence = Substitute.For<IPersistence>();
        _service = new SpecialityService(_persistence);
    }

    [Fact]
    public async Task Crear_CuandoElNombreEstaVacio_DebeLanzarExcepcionDeValidacion()
    {
        var request = new SpecialityModel.Request(
            "",
            "Descripcion valida");

        var exception =
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.Create(request));

        var detail = Assert.Single(exception.Error.Details);

        Assert.Equal("name", detail.Field);
        Assert.Equal("required", detail.Issue);

        await _persistence.DidNotReceive()
            .Add(Arg.Any<Speciality>());
    }
}
