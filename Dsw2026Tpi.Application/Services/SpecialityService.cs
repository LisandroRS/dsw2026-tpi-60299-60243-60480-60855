
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService
    {
        private readonly IPersistence _persistence;

        public SpecialityService(IPersistence persistence)
        {
            _persistence = persistence;
        }

        private static SpecialityModel.Response ToResponse(Speciality s)
        => new(s.Id, s.Name, s.Description);


        private static void ValidateRequest(SpecialityModel.Request request)
        {
            if (string.IsNullOrWhiteSpace(request.name))
                throw new ValidationException().WithDetail("name", "required");

            if (request.name.Trim().Length < 3 || request.name.Trim().Length > 100)
                throw new ValidationException().WithDetail("name", "length_between_3_and_100");

            if (string.IsNullOrWhiteSpace(request.description))
                throw new ValidationException().WithDetail("description", "required");

            if (request.description.Trim().Length < 10 || request.description.Trim().Length > 100)
                throw new ValidationException().WithDetail("description", "length_between_10_and_100");
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

        public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
        {
            ValidatePagination(pageSize, pageIndex, name);

            var specialities = await _persistence.Paginate<Speciality, string>(
                pageSize, pageIndex,
                s => !s.Deleted && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)),
                s => s.Name);

            return specialities.Map(ToResponse);
        }

        public async Task<SpecialityModel.Response?> GetById(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id);
            return speciality == null || speciality.Deleted ? null : ToResponse(speciality);
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            ValidateRequest(request);

            var name = request.name.Trim();

            var existingSpeciality = await _persistence.First<Speciality>(
                s => !s.Deleted && s.Name == name);

            if (existingSpeciality != null)
                throw new ConflictException(
                    "Speciality already exists",
                    "SPECIALITY_ALREADY_EXISTS")
                    .WithDetail("name", "already_exists");

            var speciality = new Speciality(
                name,
                request.description.Trim());

            await _persistence.Add(speciality);

            return ToResponse(speciality);
        }

        public async Task<SpecialityModel.Response?> Update(Guid id, SpecialityModel.Request request)
        {
            ValidateRequest(request);

            var speciality = await _persistence.GetById<Speciality>(id);
            if (speciality == null || speciality.Deleted) return null;

            var name = request.name.Trim();

            var existingSpeciality = await _persistence.First<Speciality>(
                s => !s.Deleted &&
                     s.Name == name &&
                     s.Id != id);

            if (existingSpeciality != null)
                throw new ConflictException(
                    "Speciality already exists",
                    "SPECIALITY_ALREADY_EXISTS")
                    .WithDetail("name", "already_exists");

            speciality.Update(name, request.description.Trim());
            await _persistence.Update(speciality);

            return ToResponse(speciality);
        }

        public async Task<bool> Delete(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id);
            if (speciality == null || speciality.Deleted) return false;

            speciality.Delete();  //aca usamos el metodo de la entidad para q no haga el borrado fisico
            await _persistence.Update(speciality);

            return true;
        }


    }
}
