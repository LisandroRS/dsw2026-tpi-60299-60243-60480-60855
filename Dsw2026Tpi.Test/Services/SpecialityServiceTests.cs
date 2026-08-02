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
    private readonly IPersistence _mockPersistence;
    private readonly SpecialityService _service;

    public SpecialityServiceTests()
    {
        _mockPersistence = Substitute.For<IPersistence>();
        _service = new SpecialityService(_mockPersistence);
    }

    [Fact]
    public async Task Create_CuandoElNombreEstaVacio_DebeLanzarExcepcionDeValidacion()
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

        await _mockPersistence.DidNotReceive()
            .Add(Arg.Any<Speciality>());
    }

    [Fact]
    public async Task Create_CuandoElNombreEsDemasiadoCorto_DebeLanzarExcepcionDeValidacion()
    {
        var request = new SpecialityModel.Request(
            "AB",
            "Descripcion valida");

        var exception =
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.Create(request));

        var detail = Assert.Single(exception.Error.Details);

        Assert.Equal("name", detail.Field);
        Assert.Equal("length_between_3_and_100", detail.Issue);

        await _mockPersistence.DidNotReceive()
            .Add(Arg.Any<Speciality>());
    }
    [Fact]
    public async Task Create_CuandoLaDescripcionEsNull_EntoncesSeLanzaUnaExcepcionDeValidacion()
    {
        var request = new SpecialityModel.Request(
            "Cardiologia",
            null!);

        var exception =
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.Create(request));

        var detail = Assert.Single(exception.Error.Details);

        Assert.Equal("description", detail.Field);
        Assert.Equal("required", detail.Issue);

        await _mockPersistence.DidNotReceive()
            .Add(Arg.Any<Speciality>());
    }

    [Fact]
    public async Task Create_CuandoLosDatosSonValidos_EntoncesSeGuardaYDevuelveLaEspecialidad()
    {
        var request = new SpecialityModel.Request(
            "Cardiologia",
            "Atencion cardiologica");

        var response = await _service.Create(request);

        Assert.Equal("Cardiologia", response.name);
        Assert.Equal("Atencion cardiologica", response.description);
        Assert.NotEqual(Guid.Empty, response.id);

        await _mockPersistence.Received()
            .Add(Arg.Is<Speciality>(speciality =>
                speciality.Name == "Cardiologia" &&
                speciality.Description == "Atencion cardiologica"));
    }
}
