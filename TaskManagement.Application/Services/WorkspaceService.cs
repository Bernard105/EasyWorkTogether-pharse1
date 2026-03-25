using System.Text.Json;
using TaskManagement.Application.DTOs.Workspace;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public WorkspaceService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<WorkspaceResponse> CreateWorkspaceAsync(int userId, CreateWorkspaceRequest request)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Create workspace
            var workspace = new Core.Entities.Workspace
            {
                Name = request.Name.Trim(),
                Config = request.Config ?? JsonDocument.Parse("{}"),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Workspaces.AddAsync(workspace);
            await _unitOfWork.CompleteAsync();
            
            // Add owner as member
            var member = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = "owner",
                JoinedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.WorkspaceMembers.AddAsync(member);
            await _unitOfWork.CompleteAsync();
            
            await _auditService.LogAsync(userId, "Workspace", workspace.Id, "workspace_created");
            
            await _unitOfWork.CommitTransactionAsync();
            
            return new WorkspaceResponse
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Config = workspace.Config,
                OwnerId = workspace.OwnerId,
                CreatedAt = workspace.CreatedAt
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task<WorkspaceResponse> GetWorkspaceAsync(int userId, int workspaceId)
    {
        // Check membership
        var isMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.UserId == userId);
        
        if (!isMember)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            throw new InvalidOperationException("Workspace not found");
        }
        
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Config = workspace.Config,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt
        };
    }
    
    public async Task<WorkspaceResponse> UpdateWorkspaceAsync(int userId, int workspaceId, UpdateWorkspaceRequest request)
    {
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            throw new InvalidOperationException("Workspace not found");
        }
        
        // Check permission
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.UserId == userId)).FirstOrDefault();
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        if (member.Role != "owner" && member.Role != "admin")
        {
            throw new UnauthorizedAccessException("You don't have permission to update this workspace");
        }
        
        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            workspace.Name = request.Name.Trim();
        }
        
        if (request.Config != null)
        {
            workspace.Config = request.Config;
        }
        
        workspace.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Workspaces.UpdateAsync(workspace);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "Workspace", workspace.Id, "workspace_updated");
        
        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Config = workspace.Config,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt
        };
    }
    
    public async Task DeleteWorkspaceAsync(int userId, int workspaceId)
    {
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            throw new InvalidOperationException("Workspace not found");
        }
        
        // Check permission - only owner can delete
        if (workspace.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only workspace owner can delete the workspace");
        }
        
        await _unitOfWork.Workspaces.DeleteAsync(workspace);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "Workspace", workspace.Id, "workspace_deleted");
    }
}