using TaskEntity = TaskManagement.Core.Entities.Task;

namespace TaskManagement.Core.Entities;

public class TaskAssignment
{
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public TaskEntity Task { get; set; } = null!;
    public User User { get; set; } = null!;
}
