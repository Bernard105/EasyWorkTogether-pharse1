using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;
using TaskEntity = TaskManagement.Core.Entities.Task;

namespace TaskManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("uq_users_email_lower");
            entity.Property(u => u.Status).HasDefaultValue("active");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.Property(w => w.Config).HasDefaultValueSql("'{}'::jsonb");
            entity.HasOne(w => w.Owner)
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("workspace_members");
            entity.HasKey(wm => new { wm.WorkspaceId, wm.UserId });
            entity.HasIndex(wm => wm.UserId).HasDatabaseName("idx_workspace_members_user_id");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasIndex(us => us.SessionId).IsUnique();
            entity.HasIndex(us => us.UserId).HasDatabaseName("idx_user_sessions_user_id");
            entity.HasIndex(us => us.Status).HasDatabaseName("idx_user_sessions_status");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.ToTable("password_resets");
            entity.HasIndex(pr => new { pr.Email, pr.Status })
                .IsUnique()
                .HasDatabaseName("uq_password_resets_pending_email")
                .HasFilter("\"status\" = 'pending'");
            entity.HasIndex(pr => pr.ExpiresAt).HasDatabaseName("idx_password_resets_expires_at");
        });

        modelBuilder.Entity<WorkspaceInvitation>(entity =>
        {
            entity.ToTable("workspace_invitations");
            entity.HasIndex(wi => wi.Code).IsUnique();
            entity.HasIndex(wi => wi.WorkspaceId).HasDatabaseName("idx_workspace_invitations_workspace_id");
            entity.HasIndex(wi => new { wi.WorkspaceId, wi.InviteeEmail, wi.Status })
                .IsUnique()
                .HasDatabaseName("uq_workspace_invitations_pending")
                .HasFilter("\"status\" = 'pending'");
        });

        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasIndex(t => t.WorkspaceId).HasDatabaseName("idx_tasks_workspace_id");
            entity.HasIndex(t => t.Status).HasDatabaseName("idx_tasks_status");
            entity.HasIndex(t => t.DueDate).HasDatabaseName("idx_tasks_due_date");
            entity.HasIndex(t => t.CreatedBy).HasDatabaseName("idx_tasks_created_by");
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.ToTable("task_assignments");
            entity.HasKey(ta => new { ta.TaskId, ta.UserId });
            entity.HasIndex(ta => ta.UserId).HasDatabaseName("idx_task_assignments_user_id");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasIndex(al => al.UserId).HasDatabaseName("idx_audit_logs_user_id");
            entity.HasIndex(al => new { al.EntityType, al.EntityId }).HasDatabaseName("idx_audit_logs_entity");
            entity.HasIndex(al => al.Action).HasDatabaseName("idx_audit_logs_action");
            entity.HasIndex(al => al.CreatedAt).HasDatabaseName("idx_audit_logs_created_at");
        });
    }
}
