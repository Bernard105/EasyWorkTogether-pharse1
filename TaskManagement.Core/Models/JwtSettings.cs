namespace TaskManagement.Core.Models;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int RememberMeRefreshTokenExpirationDays { get; set; } = 30;
}