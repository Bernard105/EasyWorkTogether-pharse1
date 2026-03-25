using System.Security.Claims;

namespace TaskManagement.Core.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(int userId, string email, string name, Guid sessionId);
    ClaimsPrincipal? ValidateAccessToken(string token);
    string HashRefreshToken(string refreshToken);
    bool VerifyRefreshToken(string refreshToken, string hash);
}