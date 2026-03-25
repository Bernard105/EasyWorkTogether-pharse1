using System.Text.Json;
using TaskManagement.Application.DTOs.Invitation;
using TaskManagement.Application.DTOs.Workspace;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class InvitationService : IInvitationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public InvitationService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<InvitationResponse> CreateInvitationAsync(int userId, int workspaceId, CreateInvitationRequest request)
    {
        var normalizedEmail = request.InviteeEmail.Trim().ToLower();
        
        // Check if user has permission (owner or admin)
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.UserId == userId)).FirstOrDefault();
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        if (member.Role != "owner" && member.Role != "admin")
        {
            throw new UnauthorizedAccessException("Only workspace owners and admins can invite members");
        }
        
        // Check if email is already a member
        var existingMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.User.Email == normalizedEmail);
        
        if (existingMember)
        {
            throw new InvalidOperationException("User is already a member of this workspace");
        }
        
        // Invalidate any existing pending invitations
        var existingInvitations = await _unitOfWork.WorkspaceInvitations.FindAsync(wi => 
            wi.WorkspaceId == workspaceId && 
            wi.InviteeEmail == normalizedEmail && 
            wi.Status == "pending");
        
        foreach (var inv in existingInvitations)
        {
            inv.Status = "expired";
            inv.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.WorkspaceInvitations.UpdateAsync(inv);
        }
        
        // Generate invitation code
        var code = Guid.NewGuid().ToString("N").Substring(0, 16);
        
        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            InviterId = userId,
            InviteeEmail = normalizedEmail,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.WorkspaceInvitations.AddAsync(invitation);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "WorkspaceInvitation", invitation.Id, "workspace_invitation_sent");
        
        // In production, send email here
        // await _emailService.SendInvitationAsync(normalizedEmail, code, workspaceId);
        
        return new InvitationResponse
        {
            Id = invitation.Id,
            InviteeEmail = invitation.InviteeEmail,
            Code = invitation.Code,
            ExpiresAt = invitation.ExpiresAt,
            Status = invitation.Status
        };
    }
    
    public async Task<WorkspaceResponse> AcceptInvitationAsync(int userId, string code)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var invitation = (await _unitOfWork.WorkspaceInvitations.FindAsync(wi => 
                wi.Code == code && wi.Status == "pending" && wi.ExpiresAt > DateTime.UtcNow)).FirstOrDefault();
            
            if (invitation == null)
            {
                throw new InvalidOperationException("Invalid or expired invitation");
            }
            
            // Get user
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }
            
            // Check if email matches
            if (user.Email != invitation.InviteeEmail)
            {
                throw new UnauthorizedAccessException("This invitation was sent to a different email address");
            }
            
            // Check if already a member
            var isMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
                wm.WorkspaceId == invitation.WorkspaceId && wm.UserId == userId);
            
            if (isMember)
            {
                throw new InvalidOperationException("You are already a member of this workspace");
            }
            
            // Add as member
            var member = new WorkspaceMember
            {
                WorkspaceId = invitation.WorkspaceId,
                UserId = userId,
                Role = "member",
                JoinedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.WorkspaceMembers.AddAsync(member);
            
            // Update invitation
            invitation.Status = "accepted";
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.WorkspaceInvitations.UpdateAsync(invitation);
            
            await _unitOfWork.CompleteAsync();
            
            await _auditService.LogAsync(userId, "WorkspaceInvitation", invitation.Id, "workspace_invitation_accepted");
            
            // Get workspace info
            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(invitation.WorkspaceId);
            
            await _unitOfWork.CommitTransactionAsync();
            
            return new WorkspaceResponse
            {
                Id = workspace!.Id,
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
    
    public async Task RejectInvitationAsync(int userId, string code)
    {
        var invitation = (await _unitOfWork.WorkspaceInvitations.FindAsync(wi => 
            wi.Code == code && wi.Status == "pending" && wi.ExpiresAt > DateTime.UtcNow)).FirstOrDefault();
        
        if (invitation == null)
        {
            throw new InvalidOperationException("Invalid or expired invitation");
        }
        
        // Get user
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }
        
        // Check if email matches
        if (user.Email != invitation.InviteeEmail)
        {
            throw new UnauthorizedAccessException("This invitation was sent to a different email address");
        }
        
        // Update invitation
        invitation.Status = "rejected";
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.WorkspaceInvitations.UpdateAsync(invitation);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "WorkspaceInvitation", invitation.Id, "workspace_invitation_rejected");
    }
}