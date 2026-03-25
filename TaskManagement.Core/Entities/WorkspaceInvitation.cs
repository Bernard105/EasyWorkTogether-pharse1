namespace TaskManagement.Core.Entities;

public class WorkspaceInvitation
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int InviterId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public User Inviter { get; set; } = null!;
}
