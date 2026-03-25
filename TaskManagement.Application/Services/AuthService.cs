using System.Security.Cryptography;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Core.Models;

namespace TaskManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IAuditService auditService,
        IJwtService jwtService,
        JwtSettings jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _auditService = auditService;
        _jwtService = jwtService;
        _jwtSettings = jwtSettings;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var trimmedName = request.Name?.Trim() ?? string.Empty;

        ValidateRegisterRequest(normalizedEmail, request.Password, trimmedName);

        var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == normalizedEmail)).FirstOrDefault();
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.HashPassword(request.Password),
            Name = trimmedName,
            AvatarUrl = null,
            Status = "active",
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();
        await _auditService.LogAsync(user.Id, "User", user.Id, "user_created");

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        ValidateLoginRequest(normalizedEmail, request.Password);

        var user = (await _unitOfWork.Users.FindAsync(u => u.Email == normalizedEmail)).FirstOrDefault();
        if (user == null)
        {
            await _auditService.LogAsync(null, "User", null, "login_failed");
            throw new UnauthorizedAccessException("Email or password is incorrect");
        }

        if (user.FailedLoginAttempts >= 3 && string.IsNullOrWhiteSpace(request.CaptchaToken))
        {
            throw new InvalidOperationException("Captcha required");
        }

        if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Account is locked");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts += 1;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(user.Id, "User", user.Id, "login_failed");
            throw new UnauthorizedAccessException("Email or password is incorrect");
        }

        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);

        var sessionId = Guid.NewGuid();
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = _jwtService.HashRefreshToken(refreshToken);
        var refreshLifetimeDays = request.RememberMe
            ? _jwtSettings.RememberMeRefreshTokenExpirationDays
            : _jwtSettings.RefreshTokenExpirationDays;

        var session = new UserSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            RefreshTokenHash = refreshTokenHash,
            RefreshExpiresAt = DateTime.UtcNow.AddDays(refreshLifetimeDays),
            RememberMe = request.RememberMe,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = null,
            RevokedAt = null
        };

        await _unitOfWork.UserSessions.AddAsync(session);
        await _unitOfWork.CompleteAsync();

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name, sessionId);
        await _auditService.LogAsync(user.Id, "User", user.Id, "login_success");

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            TokenType = "Bearer"
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("Refresh token is required");
        }

        var refreshTokenHash = _jwtService.HashRefreshToken(request.RefreshToken.Trim());

        var currentSession = (await _unitOfWork.UserSessions.FindAsync(s =>
            s.RefreshTokenHash == refreshTokenHash &&
            s.Status == "active" &&
            s.RefreshExpiresAt > DateTime.UtcNow)).FirstOrDefault();

        if (currentSession == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(currentSession.UserId);
        if (user == null || !string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        currentSession.Status = "revoked";
        currentSession.RevokedAt = DateTime.UtcNow;
        currentSession.LastUsedAt = DateTime.UtcNow;
        await _unitOfWork.UserSessions.UpdateAsync(currentSession);

        var newSessionId = Guid.NewGuid();
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = _jwtService.HashRefreshToken(newRefreshToken);
        var refreshLifetimeDays = currentSession.RememberMe
            ? _jwtSettings.RememberMeRefreshTokenExpirationDays
            : _jwtSettings.RefreshTokenExpirationDays;

        var newSession = new UserSession
        {
            SessionId = newSessionId,
            UserId = user.Id,
            RefreshTokenHash = newRefreshTokenHash,
            RefreshExpiresAt = DateTime.UtcNow.AddDays(refreshLifetimeDays),
            RememberMe = currentSession.RememberMe,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = null,
            RevokedAt = null
        };

        await _unitOfWork.UserSessions.AddAsync(newSession);
        await _unitOfWork.CompleteAsync();

        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Name, newSessionId);

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            TokenType = "Bearer"
        };
    }

    public async Task LogoutAsync(int userId, Guid sessionId)
    {
        var session = (await _unitOfWork.UserSessions.FindAsync(s =>
            s.SessionId == sessionId &&
            s.UserId == userId &&
            s.Status == "active")).FirstOrDefault();

        if (session == null)
        {
            throw new UnauthorizedAccessException("Session is not active");
        }

        session.Status = "revoked";
        session.RevokedAt = DateTime.UtcNow;
        session.LastUsedAt = DateTime.UtcNow;
        await _unitOfWork.UserSessions.UpdateAsync(session);
        await _unitOfWork.CompleteAsync();

        await _auditService.LogAsync(userId, "UserSession", session.Id, "logout");
    }

    public async Task LogoutAllAsync(int userId)
    {
        var sessions = (await _unitOfWork.UserSessions.FindAsync(s => s.UserId == userId && s.Status == "active")).ToList();
        if (sessions.Count == 0)
        {
            throw new UnauthorizedAccessException("Session is not active");
        }

        foreach (var session in sessions)
        {
            session.Status = "revoked";
            session.RevokedAt = DateTime.UtcNow;
            session.LastUsedAt = DateTime.UtcNow;
            await _unitOfWork.UserSessions.UpdateAsync(session);
        }

        await _unitOfWork.CompleteAsync();
        await _auditService.LogAsync(userId, "User", userId, "logout_all");
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static void ValidateRegisterRequest(string email, string password, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required");
        }

        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must be at least 8 characters and include uppercase, lowercase, and number");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            throw new ArgumentException("Name is required and must be at most 100 characters");
        }
    }

    private static void ValidateLoginRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Email and password are required");
        }
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
