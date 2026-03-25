namespace TaskManagement.Application.DTOs.Task;

public class TaskResponse
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public List<AssigneeResponse> Assignees { get; set; } = new List<AssigneeResponse>();
    public DateTime CreatedAt { get; set; }
}

public class AssigneeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}