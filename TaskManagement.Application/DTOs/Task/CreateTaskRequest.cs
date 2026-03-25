using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Task;

public class CreateTaskRequest
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public List<int> AssigneeIds { get; set; } = new List<int>();
}