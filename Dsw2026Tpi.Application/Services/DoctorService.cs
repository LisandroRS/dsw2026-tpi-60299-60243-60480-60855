using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    private static DoctorModel.Response ToResponse(Doctor d) => new(d.Id, d.Name, d.LicenseNumber, new DoctorModel.SpecialityDto(d.Speciality!.Id, d.Speciality!.Name));

    private static void ValidateRequest(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException().WithDetail("name", "required");

        if (request.Name.Trim().Length < 3 || request.Name.Trim().Length > 100)
            throw new ValidationException().WithDetail("name", "length_between_3_and_100");

        if (request.SpecialityId == Guid.Empty)
            throw new ValidationException().WithDetail("specialityId", "required");
    }

    private static void ValidatePagination(int pageSize, int pageIndex, string? name)
    {
        if (pageSize <= 0)
            throw new ValidationException().WithDetail("pageSize", "greater_than_zero");

        if (pageIndex < 0)
            throw new ValidationException().WithDetail("pageIndex", "greater_than_or_equal_to_zero");

        if (!string.IsNullOrWhiteSpace(name) && (name.Trim().Length < 3 || name.Trim().Length > 100))
            throw new ValidationException().WithDetail("name", "length_between_3_and_100");
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {

        ValidatePagination(pageSize, pageIndex, name);

        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, pageIndex,
            d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)),
            d => d.Name,
            nameof(Doctor.Speciality));

        return doctors.Map(ToResponse);
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        ValidateRequest(request);

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);
        if (speciality == null || !speciality.IsActive) throw new EntityNotFoundException(nameof(Speciality));


        var doctor = new Doctor(request.Name.Trim(), request.LicenseNumber, speciality);
        await _persistence.Add(doctor);

        return ToResponse(doctor);
    }

    public async Task<DoctorModel.Response?> Update(Guid id, DoctorModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null || !doctor.IsActive) return null;

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);
        if (speciality == null || !speciality.IsActive) throw new EntityNotFoundException(nameof(Speciality));

        doctor.Update(request.Name.Trim(), request.LicenseNumber, speciality);
        await _persistence.Update(doctor);

        return ToResponse(doctor);
    }

    
    public async Task<bool> Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null || !doctor.IsActive) return false;

        doctor.Deactivate();
        await _persistence.Update(doctor);

        return true;
    }

}
