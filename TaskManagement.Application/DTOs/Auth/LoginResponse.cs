using System.Text.Json.Serialization;

namespace TaskManagement.Application.DTOs.Auth;

public class LoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 900;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}
