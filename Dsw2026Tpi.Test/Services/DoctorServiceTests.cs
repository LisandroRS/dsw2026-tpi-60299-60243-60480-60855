using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;
using Xunit;

namespace Dsw2026Tpi.Test.Services;

public class DoctorServiceTests
{
    private readonly IPersistence _mockPersistence;
    private readonly DoctorService _service;

    public DoctorServiceTests()
    {
        _mockPersistence = Substitute.For<IPersistence>();
        _service = new DoctorService(_mockPersistence);
    }

    [Fact]
    public async Task Create_CuandoLaMatriculaYaExiste_EntoncesSeLanzaUnaExcepcionDeConflicto()
    {
        var speciality = new Speciality(
            "Cardiologia",
            "Atencion cardiologica");

        var existingDoctor = new Doctor(
            "Doctor existente",
            "MP1234",
            speciality);

        var request = new DoctorModel.Request(
            "Doctor nuevo",
            "MP1234",
            speciality.Id);

        _mockPersistence
            .First<Doctor>(
                Arg.Any<Expression<Func<Doctor, bool>>>(),
                Arg.Any<string[]>())
            .Returns(existingDoctor);

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () => _service.Create(request));

        var detail = Assert.Single(exception.Error.Details);

        Assert.Equal("licenseNumber", detail.Field);
        Assert.Equal("already_exists", detail.Issue);

        await _mockPersistence.DidNotReceive()
            .Add(Arg.Any<Doctor>());
    }
}