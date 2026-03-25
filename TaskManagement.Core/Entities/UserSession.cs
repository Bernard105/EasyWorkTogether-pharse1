namespace TaskManagement.Core.Entities;

public class UserSession
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public int UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime RefreshExpiresAt { get; set; }
    public bool RememberMe { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
