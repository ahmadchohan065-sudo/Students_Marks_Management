using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;

namespace MarksApi.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IPasswordHasher<AppUser> hasher, ITokenService tokens, ILogger<AuthService> logger)
    {
        _db = db; _hasher = hasher; _tokens = tokens; _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username!.Trim();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

        var valid = user is not null &&
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) != PasswordVerificationResult.Failed;

        if (!valid)
        {
            _logger.LogWarning("Failed login attempt for username {Username}", username);   // never log the password
            throw new UnauthorizedException("Invalid credentials", "Username or password is incorrect.");
        }

        var (token, expires) = _tokens.CreateToken(user!);
        _logger.LogInformation("User {UserId} logged in", user!.Id);
        return new LoginResponse(token, expires, user.Role);
    }
}