namespace TaskManagement.Core.Entities;

public class WorkspaceInvitation : BaseEntity
{
    public int WorkspaceId { get; set; }
    public int InviterId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? RespondedAt { get; set; }
    
    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public User Inviter { get; set; } = null!;
}