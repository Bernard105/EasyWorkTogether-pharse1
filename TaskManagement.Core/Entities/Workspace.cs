using System.Text.Json;
using TaskEntity = TaskManagement.Core.Entities.Task;

namespace TaskManagement.Core.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Config { get; set; }
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<WorkspaceInvitation> Invitations { get; set; } = new List<WorkspaceInvitation>();
    public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
}
