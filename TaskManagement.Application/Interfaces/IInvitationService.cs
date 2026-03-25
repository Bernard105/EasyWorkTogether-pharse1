using TaskManagement.Application.DTOs.Invitation;

namespace TaskManagement.Application.Interfaces;

public interface IInvitationService
{
    Task<InvitationResponse> CreateInvitationAsync(int userId, int workspaceId, CreateInvitationRequest request);
    Task<WorkspaceResponse> AcceptInvitationAsync(int userId, string code);
    Task RejectInvitationAsync(int userId, string code);
}