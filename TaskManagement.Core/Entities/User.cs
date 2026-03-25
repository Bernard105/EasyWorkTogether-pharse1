using System.Text.Json.Serialization;

namespace TaskManagement.Core.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "active";
    public int FailedLoginAttempts { get; set; } = 0;
    
    // Navigation properties
    public ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}