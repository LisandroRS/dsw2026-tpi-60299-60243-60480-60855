using Dsw2026Tpi.Api.Options;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimitingConfigurationExtensions
{
    private const string LoggerCategory = "Dsw2026Tpi.Api.RateLimiting";

    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => CreatePartition(
                    $"General:{GetUserOrIpKey(context)}",
                    settings.General));

            options.AddPolicy(RateLimitPolicies.AdminLogin, context =>
                CreatePartition(
                    $"{RateLimitPolicies.AdminLogin}:{GetIpKey(context)}",
                    settings.AdminLogin));

            options.AddPolicy(RateLimitPolicies.PatientLogin, context =>
                CreatePartition(
                    $"{RateLimitPolicies.PatientLogin}:{GetIpKey(context)}",
                    settings.PatientLogin));

            options.AddPolicy(RateLimitPolicies.AppointmentBooking, context =>
                CreatePartition(
                    $"{RateLimitPolicies.AppointmentBooking}:{GetUserOrIpKey(context)}",
                    settings.AppointmentBooking));

            options.OnRejected = HandleRejectionAsync;
        });

        return services;
    }

   
    private static RateLimitPartition<string> CreatePartition(
        string partitionKey,
        RateLimitRule rule) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                QueueLimit = rule.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });

    private static string GetIpKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    
    private static string GetUserOrIpKey(HttpContext context)
    {
        var user = context.User.Identity?.Name;

        return string.IsNullOrWhiteSpace(user)
            ? $"ip:{GetIpKey(context)}"
            : $"user:{user}";
    }

    
    private static async ValueTask HandleRejectionAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var user = httpContext.User.Identity?.Name ?? "anonymous";

        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory);

        logger.LogWarning(
            "Se superó el límite de solicitudes. Path: {Path}, IP: {Ip}, Usuario: {User}",
            httpContext.Request.Path.Value,
            ip,
            user);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var error = new ErrorResponse(
            nameof(ErrorCodes.RATE_LIMIT_EXCEEDED),
            ErrorCodes.RATE_LIMIT_EXCEEDED);

        error.AddDetail("request", "rate_limit_exceeded");

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(error),
            cancellationToken);
    }
}



