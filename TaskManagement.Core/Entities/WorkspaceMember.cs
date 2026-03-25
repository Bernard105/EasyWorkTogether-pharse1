namespace TaskManagement.Core.Entities;

public class WorkspaceMember
{
    public int WorkspaceId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public User User { get; set; } = null!;
}