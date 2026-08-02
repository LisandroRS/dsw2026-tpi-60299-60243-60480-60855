
namespace Dsw2026Tpi.Application.Dtos;

public record AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest Patient, string Reason);
    public record PatientRequest(long Dni);
    public record Response(Guid Id, String Specialty, String Doctor, AvailableTimeDto AvailableTime, long Dni, string Reason, string Status);
    public record SearchResponse(Guid AppointmentsId, string AppointmentsStatus, SearchPatientDto Patient, SearchDoctorDto Doctor);
    public record SearchPatientDto(long Dni, string FullName);
    public record SearchDoctorDto(Guid DoctorId, string Name, SearchSpecialtyDto Specialty);
    public record SearchSpecialtyDto(Guid SpecialtyId, string Name);
    public record AvailableTimeDto(DateOnly Date, string StartTime, string EndTime);
}
