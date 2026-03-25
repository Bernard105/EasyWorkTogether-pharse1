using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public AuditService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task LogAsync(int? userId, string entityType, long? entityId, string action, JsonDocument? metadata = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Metadata = metadata
        };
        
        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        await _unitOfWork.CompleteAsync();
    }
}