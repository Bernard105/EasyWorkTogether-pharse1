namespace TaskManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Workspace> Workspaces { get; }
    IRepository<WorkspaceMember> WorkspaceMembers { get; }
    IRepository<UserSession> UserSessions { get; }
    IRepository<PasswordReset> PasswordResets { get; }
    IRepository<WorkspaceInvitation> WorkspaceInvitations { get; }
    IRepository<Task> Tasks { get; }
    IRepository<TaskAssignment> TaskAssignments { get; }
    IRepository<AuditLog> AuditLogs { get; }
    
    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}