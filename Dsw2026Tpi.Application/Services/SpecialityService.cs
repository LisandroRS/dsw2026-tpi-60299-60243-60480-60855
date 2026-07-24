
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

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

        public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
        {
            var specialities = await _persistence.Paginate<Speciality, string>(
                pageSize, pageIndex,
                s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name),
                s => s.Name);

            return specialities.Map(ToResponse);
        }

        public async Task<SpecialityModel.Response?> GetById(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id);
            return speciality == null ? null : ToResponse(speciality);
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            var speciality = new Speciality(request.name, request.description);
            await _persistence.Add(speciality);
            return ToResponse(speciality);
        }

        public async Task<SpecialityModel.Response?> Update(Guid id, SpecialityModel.Request request)
        {
            var speciality = await _persistence.GetById<Speciality>(id);
            if (speciality == null) return null;

            speciality.Update(request.name, request.description);
            await _persistence.Update(speciality);
            return ToResponse(speciality);
        }

        public async Task<bool> Delete(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id);
            if (speciality == null) return false;

            await _persistence.Delete(speciality);
            return true;
        }

        
    }
}
