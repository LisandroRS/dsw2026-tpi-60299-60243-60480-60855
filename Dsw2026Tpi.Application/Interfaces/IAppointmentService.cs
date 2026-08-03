using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request, string patientEmail);
    Task<IEnumerable<AppointmentModel.Response>> GetByPatient(long dni, string patientEmail);
    Task Cancel(Guid id, string patientEmail);
    Task<IEnumerable<AppointmentModel.Response>> GetByDate(DateOnly date);
    Task<Pagination<AppointmentModel.SearchResponse>> Search(int pageSize, int pageIndex, Guid? specialityId, Guid? doctorId, long? dni, DateOnly? date);
    //Task<Pagination<AppointmentModel.Response>> Search(int pageSize, int pageIndex, Guid? specialityId, Guid? doctorId, long? dni, DateOnly? date);
}