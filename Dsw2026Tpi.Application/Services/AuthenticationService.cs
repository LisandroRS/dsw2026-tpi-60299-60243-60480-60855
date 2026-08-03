using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;

    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPersistence persistence)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _persistence = persistence;
    }

    private static void ValidateAdminRequest(LoginAdminModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException().WithDetail("email", "required");

        if (!request.Email.IsEmailValid())
            throw new ValidationException().WithDetail("email", "invalid");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException().WithDetail("password", "required");

        if (request.Password.Length < 8)
            throw new ValidationException().WithDetail(
                "password",
                "minimum_length_8");
    }
    private static void ValidatePatientRequest(LoginPatientModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException().WithDetail("email", "required");

        if (!request.Email.IsEmailValid())
            throw new ValidationException().WithDetail("email", "invalid");

        if (request.Dni == 0)
            throw new ValidationException().WithDetail("dni", "required");

        if (request.Dni < 1_000_000 || request.Dni > 99_999_999)
            throw new ValidationException().WithDetail("dni", "must_have_7_or_8_digits");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private async Task<Patient?> FindPatient(string email, long dni)
    {
        var patientByEmail = await _persistence.First<Patient>(
            p => p.Email == email);

        var patientByDni = await _persistence.First<Patient>(
            p => p.Dni == dni);

        if (patientByEmail == null && patientByDni == null)
            return null;

        if (patientByEmail == null ||
            patientByDni == null ||
            patientByEmail.Id != patientByDni.Id)
            throw new AuthenticationException();

        return patientByEmail;
    }


    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        ValidateAdminRequest(request);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }

        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        var token  = _jwtService.GenerateToken(user.UserName!, role);

        _logger.LogInformation("Login de administrador exitoso para: {Email}", request.Email);

        return new LoginAdminModel.Response(
            token,
            role?.ToUpperInvariant()
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        ValidatePatientRequest(request);

        var email = NormalizeEmail(request.Email);
        var patient = await FindPatient(email, request.Dni);
        ApplicationUser user;

        if (patient == null)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
                throw new AuthenticationException();

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
                throw new ConflictException(
                    nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                    ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(result.Errors.Select(
                        e => (e.Code, e.Description)));

            _ = await _userManager.AddToRoleAsync(user, Roles.Patient);

            patient = new Patient(email, request.Dni, user.Id);
            await _persistence.Add(patient);

            _logger.LogInformation("Paciente registrado: {Email}", email);
        }
        else
        {
            user = await _userManager.FindByIdAsync(patient.IdentityUserId)
                ?? throw new AuthenticationException();

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains(Roles.Patient))
                throw new AuthenticationException();
        }

        var role = Roles.Patient;

        var token = _jwtService.GenerateToken(
            user.UserName!,
            role);

        _logger.LogInformation("Login de paciente exitoso para: {Email}", email);

        return new LoginPatientModel.Response(
            token,
            role.ToUpperInvariant()
        );
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID,
            nameof(ErrorCodes.REGISTER_USER_INVALID));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
       
        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
