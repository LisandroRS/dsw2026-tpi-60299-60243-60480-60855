using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos;

public record AvailabilityModel
{
    public record Request(Guid DoctorId, IEnumerable<DayRequest> Days);
    public record DayRequest(string Day, string StartTime, string EndTime);
    public record Response(Guid Id, string Day, string StartTime, string EndTime);
}
