namespace TaskManagement.Core.Entities;

public class Task : BaseEntity
{
    public int WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "pending";
    public int CreatedBy { get; set; }
    
    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
}