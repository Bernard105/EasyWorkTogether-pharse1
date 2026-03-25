using System.Text.Json;

namespace TaskManagement.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string entityType, long? entityId, string action, JsonDocument? metadata = null);
}