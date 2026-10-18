using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;

using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Api.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Serilog;

namespace Dsw2026Tpi.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Inicializar con un logger simple antes de construir el host
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Iniciando aplicación Dsw2026Tpi.Api");

            var builder = WebApplication.CreateBuilder(args);

            //Configuraciones personalizadas
            builder.AddSerilogConfiguration();
            builder.Services.AddAppIdentity();
            builder.Services.AddAppAuthentication(builder.Configuration);
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddApplicationPersistence(builder.Configuration);
            builder.Services.AddAppCors(builder.Configuration);
            builder.Services.AddAppDependencies();
            builder.Services.AddAppRateLimiting(builder.Configuration);
            builder.Services.AddControllers();
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            //seeder del admin 
            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var adminEmail = builder.Configuration["AdminSeed:Email"]!;
                var adminPassword = builder.Configuration["AdminSeed:Password"]!;

                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (!result.Succeeded)
                        throw new InvalidOperationException("No se pudo crear el administrador inicial: " +
                            string.Join("; ", result.Errors.Select(e => e.Description)));

                    result = await userManager.AddToRoleAsync(admin, Roles.Administrator);
                    if (!result.Succeeded)
                    {
                        await userManager.DeleteAsync(admin);
                        throw new InvalidOperationException("No se pudo asignar el rol al administrador inicial");
                    }

                    Log.Information("Administrador inicial creado: {Email}", adminEmail);
                }
            }

            app.UseSerilogRequestLogging();

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health-check");

            Log.Information("Aplicación iniciada correctamente");

            await app.RunAsync();
        }
        catch (HostAbortedException)
        {
            Log.Information("El host fue abortado (normal durante migraciones de EF Core)");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación falló al iniciar");
            throw;
        }
        finally
        {
            Log.Information("Cerrando aplicación");
            await Log.CloseAndFlushAsync();
        }
    }
}

