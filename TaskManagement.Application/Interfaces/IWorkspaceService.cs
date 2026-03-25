using TaskManagement.Application.DTOs.Workspace;

namespace TaskManagement.Application.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceResponse> CreateWorkspaceAsync(int userId, CreateWorkspaceRequest request);
    Task<WorkspaceResponse> GetWorkspaceAsync(int userId, int workspaceId);
    Task<WorkspaceResponse> UpdateWorkspaceAsync(int userId, int workspaceId, UpdateWorkspaceRequest request);
    Task DeleteWorkspaceAsync(int userId, int workspaceId);
}