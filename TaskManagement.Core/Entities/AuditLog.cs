using System.Text.Json;

namespace TaskManagement.Core.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}