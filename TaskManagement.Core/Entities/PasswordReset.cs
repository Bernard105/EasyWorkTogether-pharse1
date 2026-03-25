namespace TaskManagement.Core.Entities;

public class PasswordReset : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? UsedAt { get; set; }
}