using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace TaskManagement.Application.DTOs.Workspace;

public class CreateWorkspaceRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public JsonDocument? Config { get; set; }
}