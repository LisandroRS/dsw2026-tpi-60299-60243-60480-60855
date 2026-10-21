using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class BookingEligibilityService : IBookingEligibilityService
{
    private readonly IPersistence _persistence;

    public BookingEligibilityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task EnsureCanBook(Doctor doctor, Turn turn)
    {
        if (!doctor.AcceptsAppointments())
            throw new EntityNotFoundException(nameof(Doctor));

        if (turn.Deleted)
            throw new EntityNotFoundException(nameof(Turn));

        if (turn.DoctorId != doctor.Id)
            throw new ValidationException()
                .WithDetail("availabilitySlotId", "does_not_belong_to_doctor");

        if (turn.StartDateTime <= DateTime.Now)
            throw new ValidationException()
                .WithDetail("availabilitySlotId", "slot_in_the_past");

        if (!turn.IsAvailable)
            throw new ConflictException("Slot already booked", "APPOINTMENT_CONFLICT")
                .WithDetail("availabilitySlotId", "slot_unavailable");

        if (!await IsCoveredByCurrentSchedule(turn))
            throw new ConflictException("Slot is no longer offered", "APPOINTMENT_CONFLICT")
                .WithDetail("availabilitySlotId", "slot_not_in_schedule");
    }

    public async Task<bool> IsCoveredByCurrentSchedule(Turn turn)
    {
        var doctorId = turn.DoctorId;
        var year = turn.Date.Year;
        var month = turn.Date.Month;

        var rules = await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == doctorId
              && !a.Deleted
              && a.Year == year
              && a.Month == month);

        return (rules ?? []).Any(rule => rule.Covers(turn));
    }
}