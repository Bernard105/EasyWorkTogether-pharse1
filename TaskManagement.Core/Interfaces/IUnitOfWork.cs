using TaskManagement.Core.Entities;
using TaskEntity = TaskManagement.Core.Entities.Task;

namespace TaskManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Workspace> Workspaces { get; }
    IRepository<WorkspaceMember> WorkspaceMembers { get; }
    IRepository<UserSession> UserSessions { get; }
    IRepository<PasswordReset> PasswordResets { get; }
    IRepository<WorkspaceInvitation> WorkspaceInvitations { get; }
    IRepository<TaskEntity> Tasks { get; }
    IRepository<TaskAssignment> TaskAssignments { get; }
    IRepository<AuditLog> AuditLogs { get; }

    System.Threading.Tasks.Task<int> CompleteAsync();
    System.Threading.Tasks.Task BeginTransactionAsync();
    System.Threading.Tasks.Task CommitTransactionAsync();
    System.Threading.Tasks.Task RollbackTransactionAsync();
}
