using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserService.AppData;
using UserService.Contracts;
using UserService.Models;
using UserService.Services.Messaging;

namespace UserService.Services;

public class UserAccountService : UserAccount.UserAccountBase
{
    private readonly AppDBContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UserAccountService> _logger;
    private readonly JwtOptions _jwtOptions;
    private readonly RabbitMqEventPublisher _eventPublisher;

    public UserAccountService(
        AppDBContext db,
        UserManager<AppUser> userManager,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserAccountService> logger,
        RabbitMqEventPublisher eventPublisher)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
        _eventPublisher = eventPublisher;
    }

    public override async Task<UserAuthReply> RegisterUser(RegisterUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Register request received for user {UserName}.", request.UserName);

        var existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration rejected for user {UserName}: username already exists.", request.UserName);
            return Fail("A user with that username already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailUser = await _userManager.FindByEmailAsync(request.Email);
            if (emailUser != null)
            {
                _logger.LogWarning("Registration rejected for user {UserName}: email already exists.", request.UserName);
                return Fail("A user with that email already exists.");
            }
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName,
            Address = request.Address,
            Gender = request.Gender
        };

        if (DateTime.TryParse(request.DateOfBirth, out var dob))
        {
            user.DateOfBirth = dob;
        }

        try
        {
            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Registration failed while creating user {UserName}.", request.UserName);
                return Fail(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            var roleName = request.IsAdmin ? "Admin" : "User";
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Failed to add role {RoleName} to new user {UserName}.", roleName, request.UserName);
                await _userManager.DeleteAsync(user);
                return Fail(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("User {UserName} registered successfully with role(s): {Roles}.", user.UserName, string.Join(", ", roles));
            await _eventPublisher.PublishAsync("UserRegistered", new
            {
                userId = user.Id,
                userName = user.UserName,
                email = user.Email,
                isAdmin = roles.Contains("Admin")
            }, context.CancellationToken);
            return Success(user, roles, BuildToken(user, roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while registering user {UserName}.", request.UserName);
            if (!string.IsNullOrWhiteSpace(user.Id))
            {
                await _userManager.DeleteAsync(user);
            }

            return Fail("Registration failed unexpectedly.");
        }
    }

    public override async Task<UserAuthReply> LoginUser(LoginUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Login request received for user {UserName}.", request.UserName);

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
        {
            _logger.LogWarning("Login rejected for user {UserName}: user not found.", request.UserName);
            return Fail("Invalid login attempt.");
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
        {
            _logger.LogWarning("Login rejected for user {UserName}: invalid password.", request.UserName);
            return Fail("Invalid login attempt.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("User {UserName} logged in successfully with role(s): {Roles}.", user.UserName, string.Join(", ", roles));
        return Success(user, roles, BuildToken(user, roles));
    }

    public override async Task StreamUsers(StreamUsersRequest request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("StreamUsers request received. Role filter: {RoleFilter}.", request.RoleFilter ?? string.Empty);
        var users = await _db.Users.AsNoTracking().ToListAsync(context.CancellationToken);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(request.RoleFilter) && !roles.Contains(request.RoleFilter))
            {
                continue;
            }

            await responseStream.WriteAsync(new UserProfile
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                DateOfBirth = user.DateOfBirth.ToString("O"),
                Address = user.Address ?? string.Empty,
                Gender = user.Gender ?? string.Empty,
                Roles = { roles },
                IsAdmin = roles.Contains("Admin")
            });
        }
    }

    private UserAuthReply Success(AppUser user, IEnumerable<string> roles, string token) => new()
    {
        Success = true,
        Message = "OK",
        Token = token,
        UserId = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Roles = { roles },
        IsAdmin = roles.Contains("Admin")
    };

    private UserAuthReply Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    private string BuildToken(AppUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
