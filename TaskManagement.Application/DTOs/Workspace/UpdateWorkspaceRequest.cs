using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace TaskManagement.Application.DTOs.Workspace;

public class UpdateWorkspaceRequest
{
    [MinLength(3)]
    [MaxLength(100)]
    public string? Name { get; set; }
    
    public JsonDocument? Config { get; set; }
}