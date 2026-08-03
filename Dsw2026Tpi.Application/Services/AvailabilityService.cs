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
    private readonly IHolidayService _holidayService;

    private record ParsedDay(DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);

    public AvailabilityService(IPersistence persistence, IHolidayService holidayService)
    {
        _persistence = persistence;
        _holidayService = holidayService;
    }

    private static AvailabilityModel.Response ToResponse(Availability availability)
    {
        return new AvailabilityModel.Response(
            availability.Id,
            DayToText(availability.DayOfWeek),
            availability.StartTime.ToString("HH:mm"),
            availability.EndTime.ToString("HH:mm"));
    }

    private static AvailabilityModel.SaveResponse ToSaveResponse(Availability availability)
    {
        return new AvailabilityModel.SaveResponse(
            availability.Id,
            availability.DoctorId,
            availability.Year,
            availability.Month,
            DayToText(availability.DayOfWeek),
            availability.StartTime.ToString("HH:mm"),
            availability.EndTime.ToString("HH:mm"));
    }
    public async Task<IEnumerable<AvailabilityModel.SaveResponse>> Create(AvailabilityModel.Request request)
    {
        var parsedDays = ValidateAndParseRequest(request);
        var doctor = await GetActiveDoctor(request.DoctorId);

        var existingRules = await GetMonthRules(doctor.Id);

        var hasOverlap = parsedDays.Any(nueva =>
            existingRules.Any(actual =>
                actual.DayOfWeek == nueva.Day &&
                nueva.StartTime < actual.EndTime &&
                actual.StartTime < nueva.EndTime));

        if (hasOverlap)
            throw new ValidationException().WithDetail("days", "overlapping_existing_availability");

        var now = DateTime.Now;

        var rules = parsedDays
            .Select(p => new Availability(doctor, now.Year, now.Month, p.Day, p.StartTime, p.EndTime))
            .ToList();

        var turns = rules
            .SelectMany(rule => GenerateTurns(rule, now))
            .ToList();

        await _persistence.AddRange(rules, saveChanges: false);
        await _persistence.AddRange(turns);

        return rules
            .OrderBy(r => DayOrder(r.DayOfWeek))
            .ThenBy(r => r.StartTime)
            .Select(ToSaveResponse)
            .ToList();
    }


    public async Task<IEnumerable<AvailabilityModel.SaveResponse>> Update(AvailabilityModel.Request request)
    {
        var parsedDays = ValidateAndParseRequest(request);
        var doctor = await GetActiveDoctor(request.DoctorId);

        var existingRules = await GetMonthRules(doctor.Id);
        var existingTurns = await GetMonthTurns(doctor.Id);

        var now = DateTime.Now;

        var futureTurns = existingTurns
            .Where(t => t.StartDateTime >= now)
            .ToList();

        var bookedTurns = futureTurns
            .Where(t => t.Status == TurnStatus.Booked)
            .ToList();

        foreach (var rule in existingRules)
            rule.Delete();

        foreach (var turn in futureTurns.Where(t => t.Status != TurnStatus.Booked))
            turn.Delete();

        var rules = parsedDays
            .Select(p => new Availability(doctor, now.Year, now.Month, p.Day, p.StartTime, p.EndTime))
            .ToList();

        var ocupados = bookedTurns
            .Select(t => (t.Date, t.StartTime))
            .ToHashSet();

        var turns = rules
            .SelectMany(rule => GenerateTurns(rule, now))
            .Where(t => !ocupados.Contains((t.Date, t.StartTime)))
            .ToList();

        await _persistence.AddRange(rules, saveChanges: false);
        await _persistence.AddRange(turns);

        return rules
            .OrderBy(r => DayOrder(r.DayOfWeek))
            .ThenBy(r => r.StartTime)
            .Select(ToSaveResponse)
            .ToList();
    }


    public async Task<IEnumerable<AvailabilityModel.Response>> GetByDoctor(Guid doctorId)
    {
        _ = await GetActiveDoctor(doctorId);

        var rules = await GetMonthRules(doctorId);

        return rules
            .OrderBy(r => DayOrder(r.DayOfWeek))
            .ThenBy(r => r.StartTime)
            .Select(ToResponse)
            .ToList();
    }


    private async Task<List<Availability>> GetMonthRules(Guid doctorId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        return (await _persistence.GetFiltered<Availability>(
            a => a.DoctorId == doctorId &&
                 !a.Deleted &&
                 a.Year == today.Year &&
                 a.Month == today.Month))?.ToList() ?? [];
    }

    private async Task<List<Turn>> GetMonthTurns(Guid doctorId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = new DateOnly(today.Year, today.Month,
                                    DateTime.DaysInMonth(today.Year, today.Month));

        return (await _persistence.GetFiltered<Turn>(
            t => t.DoctorId == doctorId &&
                 !t.Deleted &&
                 t.Date >= monthStart &&
                 t.Date <= monthEnd))?.ToList() ?? [];
    }

    private async Task<Doctor> GetActiveDoctor(Guid doctorId)
    {
        if (doctorId == Guid.Empty)
            throw new ValidationException().WithDetail("doctorId", "required");

        var doctor = await _persistence.GetById<Doctor>(doctorId);
        if (doctor == null || doctor.Deleted)
            throw new EntityNotFoundException(nameof(Doctor));

        return doctor;
    }

    private List<Turn> GenerateTurns(Availability rule, DateTime now)
    {
        var turns = new List<Turn>();

        var firstDay = new DateOnly(rule.Year, rule.Month, 1);
        var lastDay = new DateOnly(rule.Year, rule.Month,
                                   DateTime.DaysInMonth(rule.Year, rule.Month));

        var today = DateOnly.FromDateTime(now);
        var from = today > firstDay ? today : firstDay;

        var slotCount = (int)(rule.EndTime.ToTimeSpan() - rule.StartTime.ToTimeSpan())
                            .TotalMinutes / 30;

        for (var date = from; date <= lastDay; date = date.AddDays(1))
        {
            if (date.DayOfWeek != rule.DayOfWeek) continue;
            if (_holidayService.IsHoliday(date)) continue;

            for (var i = 0; i < slotCount; i++)
            {
                var start = rule.StartTime.AddMinutes(i * 30);
                var end = rule.StartTime.AddMinutes((i + 1) * 30);

                if (date.ToDateTime(start) >= now)
                    turns.Add(new Turn(rule, date, start, end));
            }
        }

        return turns;
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

            if (!TimeOnly.TryParseExact(day.StartTime, "HH:mm", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var startTime))
                throw new ValidationException().WithDetail("startTime", "invalid_HH:mm");

            if (!TimeOnly.TryParseExact(day.EndTime, "HH:mm", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var endTime))
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
}