using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;

namespace TaskManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; }
    public DbSet<Task> Tasks { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("uq_users_email_lower");
        
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasDefaultValue("active");
        
        // Workspaces
        modelBuilder.Entity<Workspace>()
            .Property(w => w.Config)
            .HasDefaultValueSql("'{}'::jsonb");
        
        modelBuilder.Entity<Workspace>()
            .HasOne(w => w.Owner)
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // WorkspaceMembers
        modelBuilder.Entity<WorkspaceMember>()
            .HasKey(wm => new { wm.WorkspaceId, wm.UserId });
        
        modelBuilder.Entity<WorkspaceMember>()
            .HasIndex(wm => new { wm.WorkspaceId, wm.UserId })
            .IsUnique();
        
        modelBuilder.Entity<WorkspaceMember>()
            .HasIndex(wm => wm.UserId)
            .HasDatabaseName("idx_workspace_members_user_id");
        
        // UserSessions
        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.SessionId)
            .IsUnique();
        
        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.UserId)
            .HasDatabaseName("idx_user_sessions_user_id");
        
        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.Status)
            .HasDatabaseName("idx_user_sessions_status");
        
        // PasswordResets
        modelBuilder.Entity<PasswordReset>()
            .HasIndex(pr => new { pr.Email, pr.Status })
            .IsUnique()
            .HasDatabaseName("uq_password_resets_pending_email")
            .HasFilter("\"status\" = 'pending'");
        
        // WorkspaceInvitations
        modelBuilder.Entity<WorkspaceInvitation>()
            .HasIndex(wi => wi.Code)
            .IsUnique();
        
        modelBuilder.Entity<WorkspaceInvitation>()
            .HasIndex(wi => new { wi.WorkspaceId, wi.InviteeEmail, wi.Status })
            .IsUnique()
            .HasDatabaseName("uq_workspace_invitations_pending")
            .HasFilter("\"status\" = 'pending'");
        
        // Tasks
        modelBuilder.Entity<Task>()
            .HasIndex(t => t.WorkspaceId)
            .HasDatabaseName("idx_tasks_workspace_id");
        
        modelBuilder.Entity<Task>()
            .HasIndex(t => t.Status)
            .HasDatabaseName("idx_tasks_status");
        
        modelBuilder.Entity<Task>()
            .HasIndex(t => t.DueDate)
            .HasDatabaseName("idx_tasks_due_date");
        
        // TaskAssignments
        modelBuilder.Entity<TaskAssignment>()
            .HasKey(ta => new { ta.TaskId, ta.UserId });
        
        modelBuilder.Entity<TaskAssignment>()
            .HasIndex(ta => ta.UserId)
            .HasDatabaseName("idx_task_assignments_user_id");
        
        // AuditLogs
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.UserId)
            .HasDatabaseName("idx_audit_logs_user_id");
        
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.EntityType, al.EntityId })
            .HasDatabaseName("idx_audit_logs_entity");
        
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAt)
            .HasDatabaseName("idx_audit_logs_created_at");
    }
}