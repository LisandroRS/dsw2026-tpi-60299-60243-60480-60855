using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;

    public AppointmentService(IPersistence persistence)
    {
        _persistence = persistence;
    }
    private static AppointmentModel.Response ToResponse(Appointment appointment)
    {
        var turn = appointment.Turn!;
        var doctor = turn.Doctor!;

        return new AppointmentModel.Response(
            appointment.Id,
            doctor.Speciality!.Name,
            doctor.Name,
            new AppointmentModel.AvailableTimeDto(
                turn.Date,
                turn.StartTime.ToString("HH:mm"),
                turn.EndTime.ToString("HH:mm")),
            appointment.Patient!.Dni,
            appointment.Reason,
            StatusToText(appointment.Status));
    }

    private static string StatusToText(AppointmentStatus status)
    {
        return status switch
        {
            AppointmentStatus.Booked => "BOOKED",
            AppointmentStatus.Cancelled => "CANCELLED",
            AppointmentStatus.Attended => "ATTENDED",
            AppointmentStatus.NoShow => "NO_SHOW", _ => status.ToString().ToUpperInvariant()
        };
    }
    private static void ValidateCreateRequest(AppointmentModel.Request request)
    {
        if (request.DoctorId == Guid.Empty)
            throw new ValidationException()
                .WithDetail("doctorId", "required");

        if (request.AvailabilityId == Guid.Empty)
            throw new ValidationException()
                .WithDetail("availabilityId", "required");

        if (request.Patient == null)
            throw new ValidationException()
                .WithDetail("patient", "required");

        ValidateDni(request.Patient.Dni, "patient.dni");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException()
                .WithDetail("reason", "required");

        if (request.Reason.Trim().Length < 5)
            throw new ValidationException()
                .WithDetail("reason", "minimum_length_5");
    }
    private static void ValidateDni(long dni, string field)
    {
        if (dni < 1_000_000 || dni > 9_999_999_999)
            throw new ValidationException()
                .WithDetail(field, "must_have_between_7_and_10_digits");
    }

    public async Task<AppointmentModel.Response> Create(
     AppointmentModel.Request request,
     string patientEmail)
    {
        ValidateCreateRequest(request);

        if (string.IsNullOrWhiteSpace(patientEmail))
            throw new AuthorizationException();

        var email = patientEmail.Trim().ToLowerInvariant();

        var doctor = await _persistence.GetById<Doctor>(
            request.DoctorId,
            nameof(Doctor.Speciality));

        if (doctor == null || !doctor.IsActive)
            throw new EntityNotFoundException(nameof(Doctor));

        var patient = await _persistence.First<Patient>(
            p => p.Dni == request.Patient.Dni);

        if (patient == null)
            throw new EntityNotFoundException(nameof(Patient));

        if (patient.Email != email)
            throw new AuthorizationException();

        var turn = await _persistence.GetById<Turn>(
            request.AvailabilityId,
            "Doctor.Speciality");

        if (turn == null || turn.Deleted)
            throw new EntityNotFoundException(nameof(Turn));

        if (turn.DoctorId != doctor.Id)
            throw new ValidationException()
                .WithDetail("availabilityId", "does_not_belong_to_doctor");

        if (turn.StartDateTime <= DateTime.Now)
            throw new ValidationException()
                .WithDetail("availabilityId", "slot_in_the_past");

        if (!turn.IsAvailable)
            throw new ConflictException(
                "Slot already booked",
                "APPOINTMENT_CONFLICT")
                .WithDetail("availabilityId", "slot_unavailable");

        turn.Book();

        var appointment = new Appointment(
            turn,
            patient,
            request.Reason.Trim());

        await _persistence.Add(appointment);

        return ToResponse(appointment);
    }

    public  async Task<IEnumerable<AppointmentModel.Response>> GetByPatient(
        long dni,
        string patientEmail)
    {
        ValidateDni(dni, "dni");

        if (string.IsNullOrWhiteSpace(patientEmail))
            throw new AuthorizationException();

        var email = patientEmail.Trim().ToLowerInvariant();

        var patient = await _persistence.First<Patient>(
            p => p.Dni == dni);

        if (patient == null)
            throw new EntityNotFoundException(nameof(Patient));

        if (patient.Email != email)
            throw new AuthorizationException();

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        var appointments = await _persistence.GetFiltered<Appointment>(
            a => a.PatientId == patient.Id &&
                 a.Status == AppointmentStatus.Booked &&
                 !a.Turn!.Deleted &&
                 (a.Turn.Date > today ||
                  (a.Turn.Date == today &&
                   a.Turn.StartTime > currentTime)),
            nameof(Appointment.Patient),
            "Turn.Doctor.Speciality");

        return (appointments ?? [])
            .OrderBy(a => a.Turn!.Date)
            .ThenBy(a => a.Turn!.StartTime)
            .Select(ToResponse)
            .ToList();
    }

    public async Task Cancel(
        Guid id,
        string patientEmail)
    {
        if (id == Guid.Empty)
            throw new ValidationException()
                .WithDetail("id", "required");

        if (string.IsNullOrWhiteSpace(patientEmail))
            throw new AuthorizationException();

        var email = patientEmail.Trim().ToLowerInvariant();

        var appointment = await _persistence.GetById<Appointment>(
            id,
            nameof(Appointment.Patient),
            nameof(Appointment.Turn));

        if (appointment == null)
            throw new EntityNotFoundException(nameof(Appointment));

        if (appointment.Patient == null || appointment.Patient.Email != email)
            throw new AuthorizationException();

        if (appointment.Status != AppointmentStatus.Booked)
            throw new ConflictException(
                "Appointment is not booked",
                "APPOINTMENT_CONFLICT")
                .WithDetail("id", "appointment_not_booked");

        var turn = appointment.Turn;

        if (turn == null || turn.Deleted)
            throw new EntityNotFoundException(nameof(Turn));

        if (turn.Status != TurnStatus.Booked)
            throw new ConflictException(
                "Slot is not booked",
                "APPOINTMENT_CONFLICT")
                .WithDetail("id", "slot_not_booked");

        appointment.Cancel();
        turn.Release();

        await _persistence.SaveChanges();
    }

    public async Task<IEnumerable<AppointmentModel.Response>> GetByDate(
        DateOnly date)
    {
        if (date == default)
            throw new ValidationException()
                .WithDetail("date", "required");

        var appointments = await _persistence.GetFiltered<Appointment>(
            a => a.Turn!.Date == date,
            nameof(Appointment.Patient),
            "Turn.Doctor.Speciality");

        return (appointments ?? [])
            .OrderBy(a => a.Turn!.StartTime)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<Pagination<AppointmentModel.Response>> Search(
        int pageSize,
        int pageIndex,
        Guid? specialityId,
        Guid? doctorId,
        long? dni,
        DateOnly? date)
    {
        if (pageSize <= 0)
            throw new ValidationException()
                .WithDetail("pageSize", "greater_than_zero");

        if (pageIndex < 0)
            throw new ValidationException()
                .WithDetail("pageIndex", "greater_than_or_equal_to_zero");

        if (specialityId.HasValue &&
            specialityId.Value == Guid.Empty)
            throw new ValidationException()
                .WithDetail("specialtyId", "invalid");

        if (doctorId.HasValue &&
            doctorId.Value == Guid.Empty)
            throw new ValidationException()
                .WithDetail("doctorId", "invalid");

        if (dni.HasValue)
            ValidateDni(dni.Value, "dni");

        if (date.HasValue &&
            date.Value == default)
            throw new ValidationException()
                .WithDetail("date", "invalid");

        var appointments =
            await _persistence.Paginate<Appointment, DateOnly>(
                pageSize,
                pageIndex,
                a =>
                    (!specialityId.HasValue ||
                 a.Turn!.Doctor!.SpecialityId == specialityId.Value) &&
                    (!doctorId.HasValue ||

                 a.Turn!.DoctorId == doctorId.Value) &&
                    (!dni.HasValue ||
                 a.Patient!.Dni == dni.Value) &&
                    (!date.HasValue ||

                 a.Turn!.Date == date.Value),
                a => a.Turn!.Date,
                nameof(Appointment.Patient),
                "Turn.Doctor.Speciality");

        return appointments.Map(ToResponse);
    }
}
