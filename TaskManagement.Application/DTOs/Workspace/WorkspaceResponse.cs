using System.Text.Json;

namespace TaskManagement.Application.DTOs.Workspace;

public class WorkspaceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Config { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}