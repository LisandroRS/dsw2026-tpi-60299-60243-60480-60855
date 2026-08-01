namespace Dsw2026Tpi.Api.Options
{
    public class RateLimitSettings
    {
        public RateLimitRule AdminLogin { get; set; } = new();
        public RateLimitRule PatientLogin { get; set; } = new();
        public RateLimitRule AppointmentBooking { get; set; } = new();
        public RateLimitRule General { get; set; } = new();
    }

    public class RateLimitRule
    {
        public int PermitLimit { get; set; } = 100;
        public int WindowSeconds { get; set; } = 60;
        public int QueueLimit { get; set; } = 0;
    }
}
