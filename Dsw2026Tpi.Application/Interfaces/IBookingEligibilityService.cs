using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IBookingEligibilityService
{
	Task EnsureCanBook(Doctor doctor, Turn turn);
	Task<bool> IsCoveredByCurrentSchedule(Turn turn);
}