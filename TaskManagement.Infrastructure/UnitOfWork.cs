using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using TaskEntity = TaskManagement.Core.Entities.Task;

namespace TaskManagement.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<User>? _users;
    private IRepository<Workspace>? _workspaces;
    private IRepository<WorkspaceMember>? _workspaceMembers;
    private IRepository<UserSession>? _userSessions;
    private IRepository<PasswordReset>? _passwordResets;
    private IRepository<WorkspaceInvitation>? _workspaceInvitations;
    private IRepository<TaskEntity>? _tasks;
    private IRepository<TaskAssignment>? _taskAssignments;
    private IRepository<AuditLog>? _auditLogs;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Workspace> Workspaces => _workspaces ??= new Repository<Workspace>(_context);
    public IRepository<WorkspaceMember> WorkspaceMembers => _workspaceMembers ??= new Repository<WorkspaceMember>(_context);
    public IRepository<UserSession> UserSessions => _userSessions ??= new Repository<UserSession>(_context);
    public IRepository<PasswordReset> PasswordResets => _passwordResets ??= new Repository<PasswordReset>(_context);
    public IRepository<WorkspaceInvitation> WorkspaceInvitations => _workspaceInvitations ??= new Repository<WorkspaceInvitation>(_context);
    public IRepository<TaskEntity> Tasks => _tasks ??= new Repository<TaskEntity>(_context);
    public IRepository<TaskAssignment> TaskAssignments => _taskAssignments ??= new Repository<TaskAssignment>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
