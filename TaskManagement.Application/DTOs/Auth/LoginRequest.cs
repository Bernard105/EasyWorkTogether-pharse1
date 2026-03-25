using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagement.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("remember_me")]
    public bool RememberMe { get; set; }

    [JsonPropertyName("captcha_token")]
    public string? CaptchaToken { get; set; }
}
