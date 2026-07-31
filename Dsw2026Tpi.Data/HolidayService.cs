using Dsw2026Tpi.Domain.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace Dsw2026Tpi.Data
{
    public class HolidayService : IHolidayService
    {
        private readonly HashSet<DateOnly> _holidays;

        public HolidayService()
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "Sources",
                "Feriados.json");

            var json = File.ReadAllText(path);

            var dates = JsonSerializer.Deserialize<List<string>>(json) ?? [];

            _holidays = dates
                .Select(date => DateOnly.ParseExact(
                    date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture))
                .ToHashSet();
        }

        public bool IsHoliday(DateOnly date)
        {
            return _holidays.Contains(date);
        }

    }
}
