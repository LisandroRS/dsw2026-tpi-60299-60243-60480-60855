using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System.Globalization;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IPersistence _persistence;

    private record ParsedDay(DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);

    public AvailabilityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task Create(AvailabilityModel.Request request)
    {
        var parsedDays = ValidateAndParseRequest(request);
        var doctor = await GetActiveDoctor(request.DoctorId);
        var slots = GenerateSlots(doctor, parsedDays);
        var monthStart = GetMonthStart();
        var nextMonth = monthStart.AddMonths(1);

        var existing = (await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == request.DoctorId &&
                !a.Deleted &&
                 a.StartDateTime < nextMonth &&
                 a.EndDateTime > monthStart))?.ToList() ?? [];

        var hasOverlap = slots.Any(slot =>
            existing.Any(current =>
                slot.StartDateTime < current.EndDateTime &&
                current.StartDateTime < slot.EndDateTime));

        if (hasOverlap)
            throw new ValidationException().WithDetail("days", "overlapping_existing_availability");

        await _persistence.AddRange(slots);
    }

    public async Task Update(AvailabilityModel.Request request)
    {
        var parsedDays = ValidateAndParseRequest(request);
        var doctor = await GetActiveDoctor(request.DoctorId);
        var slots = GenerateSlots(doctor, parsedDays);
        var monthStart = GetMonthStart();
        var nextMonth = monthStart.AddMonths(1);

        var existing = (await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == request.DoctorId &&
                 !a.Deleted &&
                 a.StartDateTime < nextMonth &&
                 a.EndDateTime > monthStart))?.ToList() ?? [];

        if (existing.Any(a => a.Status == SlotStatus.Booked))
            throw new ConflictException(
                "No se puede modificar la disponibilidad: hay turnos reservados en el mes",
                "AVAILABILITY_HAS_BOOKED_SLOTS");

        foreach (var slot in existing)
            slot.Delete();                               

        await _persistence.AddRange(slots);
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> GetByDoctor(Guid doctorId)
    {
        _ = await GetActiveDoctor(doctorId);

        var monthStart = GetMonthStart();
        var nextMonth = monthStart.AddMonths(1);

        var availabilities = (await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == doctorId &&
                !a.Deleted &&
                 a.StartDateTime < nextMonth &&
                 a.EndDateTime > monthStart))?.ToList() ?? [];

        return ToResponse(availabilities);
    }

    private async Task<Doctor> GetActiveDoctor(Guid doctorId)
    {
        if (doctorId == Guid.Empty)
            throw new ValidationException().WithDetail("doctorId", "required");

        var doctor = await _persistence.GetById<Doctor>(doctorId);
        if (doctor == null || !doctor.IsActive)
            throw new EntityNotFoundException(nameof(Doctor));

        return doctor;
    }

    private static List<ParsedDay> ValidateAndParseRequest(AvailabilityModel.Request request)
    {
        if (request.DoctorId == Guid.Empty)
            throw new ValidationException().WithDetail("doctorId", "required");

        var days = request.Days?.ToList();
        if (days == null || days.Count == 0)
            throw new ValidationException().WithDetail("days", "required");

        var parsedDays = new List<ParsedDay>();

        foreach (var day in days)
        {
            if (string.IsNullOrWhiteSpace(day.Day))
                throw new ValidationException().WithDetail("day", "required");

            var dayOfWeek = ParseDay(day.Day);

            if (!TimeOnly.TryParseExact(day.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
                throw new ValidationException().WithDetail("startTime", "invalid_HH:mm");

            if (!TimeOnly.TryParseExact(day.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
                throw new ValidationException().WithDetail("endTime", "invalid_HH:mm");

            if (startTime >= endTime)
                throw new ValidationException().WithDetail("startTime", "must_be_before_endTime");

            if (startTime.Minute % 30 != 0 || endTime.Minute % 30 != 0)
                throw new ValidationException().WithDetail("days", "times_must_align_to_30_minutes");

            var duration = endTime.ToTimeSpan() - startTime.ToTimeSpan();
            if (duration.TotalMinutes < 30 || duration.TotalMinutes % 30 != 0)
                throw new ValidationException().WithDetail("days", "duration_must_be_multiple_of_30_minutes");

            parsedDays.Add(new ParsedDay(dayOfWeek, startTime, endTime));
        }

        ValidateOverlaps(parsedDays);

        return parsedDays;
    }

    private static void ValidateOverlaps(List<ParsedDay> parsedDays)
    {
        foreach (var group in parsedDays.GroupBy(d => d.Day))
        {
            var ranges = group.ToList();

            for (var i = 0; i < ranges.Count; i++)
            {
                for (var j = i + 1; j < ranges.Count; j++)
                {
                    var overlap = ranges[i].StartTime < ranges[j].EndTime &&
                                  ranges[j].StartTime < ranges[i].EndTime;

                    if (overlap)
                        throw new ValidationException().WithDetail("days", "overlapping_ranges");
                }
            }
        }
    }

    private static List<Availability> GenerateSlots(Doctor doctor, List<ParsedDay> parsedDays)
    {
        var now = DateTime.Now;
        var lastDay = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        var slots = new List<Availability>();

        for (var date = now.Date; date <= lastDay; date = date.AddDays(1))
        {
            foreach (var day in parsedDays.Where(d => d.Day == date.DayOfWeek))
            {
                var slotStart = date.Add(day.StartTime.ToTimeSpan());
                var rangeEnd = date.Add(day.EndTime.ToTimeSpan());

                while (slotStart < rangeEnd)
                {
                    var slotEnd = slotStart.AddMinutes(30);

                    if (slotStart >= now)
                        slots.Add(new Availability(doctor, slotStart, slotEnd));

                    slotStart = slotEnd;
                }
            }
        }

        if (slots.Count == 0)
            throw new ValidationException().WithDetail("days", "no_future_slots_for_current_month");

        return slots;
    }

    private static IEnumerable<AvailabilityModel.Response> ToResponse(IEnumerable<Availability> availabilities)
    {
        var slotsByDay = availabilities
            .Select(a => new ParsedDay(
                a.StartDateTime.DayOfWeek,
                TimeOnly.FromDateTime(a.StartDateTime),
                TimeOnly.FromDateTime(a.EndDateTime)))
            .Distinct()
            .GroupBy(a => a.Day)
            .OrderBy(g => DayOrder(g.Key));

        var response = new List<AvailabilityModel.Response>();

        foreach (var group in slotsByDay)
        {
            var ranges = group.OrderBy(r => r.StartTime).ToList();
            var currentStart = ranges[0].StartTime;
            var currentEnd = ranges[0].EndTime;

            foreach (var range in ranges.Skip(1))
            {
                if (range.StartTime <= currentEnd)
                {
                    if (range.EndTime > currentEnd)
                        currentEnd = range.EndTime;
                }
                else
                {
                    response.Add(new AvailabilityModel.Response(
                        DayToText(group.Key),
                        currentStart.ToString("HH:mm"),
                        currentEnd.ToString("HH:mm")));

                    currentStart = range.StartTime;
                    currentEnd = range.EndTime;
                }
            }

            response.Add(new AvailabilityModel.Response(
                DayToText(group.Key),
                currentStart.ToString("HH:mm"),
                currentEnd.ToString("HH:mm")));
        }

        return response;
    }

    private static DayOfWeek ParseDay(string day)
    {
        return day.Trim().ToUpperInvariant() switch
        {
            "LUNES" => DayOfWeek.Monday,
            "MARTES" => DayOfWeek.Tuesday,
            "MIÉRCOLES" or "MIERCOLES" => DayOfWeek.Wednesday,
            "JUEVES" => DayOfWeek.Thursday,
            "VIERNES" => DayOfWeek.Friday,
            "SÁBADO" or "SABADO" => DayOfWeek.Saturday,
            "DOMINGO" => DayOfWeek.Sunday,
            _ => throw new ValidationException().WithDetail("day", "invalid")
        };
    }

    private static string DayToText(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "LUNES",
            DayOfWeek.Tuesday => "MARTES",
            DayOfWeek.Wednesday => "MIÉRCOLES",
            DayOfWeek.Thursday => "JUEVES",
            DayOfWeek.Friday => "VIERNES",
            DayOfWeek.Saturday => "SÁBADO",
            DayOfWeek.Sunday => "DOMINGO",
            _ => string.Empty
        };
    }

    private static int DayOrder(DayOfWeek day)
    {
        return day == DayOfWeek.Sunday ? 7 : (int)day;
    }

    private static DateTime GetMonthStart()
    {
        var today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1);
    }
}