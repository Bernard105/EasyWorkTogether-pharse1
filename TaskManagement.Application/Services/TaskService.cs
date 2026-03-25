using TaskManagement.Application.DTOs.Task;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public TaskService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<TaskResponse> CreateTaskAsync(int userId, int workspaceId, CreateTaskRequest request)
    {
        // Check if user is a member of workspace
        var isMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.UserId == userId);
        
        if (!isMember)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        // Get user role
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == workspaceId && wm.UserId == userId)).FirstOrDefault();
        
        // Check permissions for assignees
        if (request.AssigneeIds.Any())
        {
            if (member?.Role == "member")
            {
                // Member can only assign to themselves
                if (request.AssigneeIds.Count != 1 || request.AssigneeIds[0] != userId)
                {
                    throw new UnauthorizedAccessException("Members can only assign tasks to themselves");
                }
            }
            
            // Verify all assignees are workspace members
            foreach (var assigneeId in request.AssigneeIds)
            {
                var isAssigneeMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
                    wm.WorkspaceId == workspaceId && wm.UserId == assigneeId);
                
                if (!isAssigneeMember)
                {
                    throw new ArgumentException($"User {assigneeId} is not a member of this workspace");
                }
            }
        }
        
        // Validate due date
        if (request.DueDate.HasValue && request.DueDate.Value.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Due date cannot be in the past");
        }
        
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Create task
            var task = new Core.Entities.Task
            {
                WorkspaceId = workspaceId,
                Title = request.Title.Trim(),
                Description = request.Description,
                DueDate = request.DueDate,
                Status = "pending",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.CompleteAsync();
            
            // Create task assignments
            var assignees = new List<AssigneeResponse>();
            
            foreach (var assigneeId in request.AssigneeIds)
            {
                var assignment = new Core.Entities.TaskAssignment
                {
                    TaskId = task.Id,
                    UserId = assigneeId,
                    AssignedAt = DateTime.UtcNow
                };
                
                await _unitOfWork.TaskAssignments.AddAsync(assignment);
                
                // Get assignee info
                var assigneeUser = await _unitOfWork.Users.GetByIdAsync(assigneeId);
                if (assigneeUser != null)
                {
                    assignees.Add(new AssigneeResponse
                    {
                        Id = assigneeUser.Id,
                        Name = assigneeUser.Name,
                        Email = assigneeUser.Email
                    });
                }
            }
            
            await _unitOfWork.CompleteAsync();
            
            await _auditService.LogAsync(userId, "Task", task.Id, "task_created");
            
            await _unitOfWork.CommitTransactionAsync();
            
            // Get creator info for response
            var creator = await _unitOfWork.Users.GetByIdAsync(userId);
            
            return new TaskResponse
            {
                Id = task.Id,
                WorkspaceId = task.WorkspaceId,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                CreatedBy = task.CreatedBy,
                Assignees = assignees,
                CreatedAt = task.CreatedAt
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task<TaskResponse> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }
        
        // Check permissions
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == task.WorkspaceId && wm.UserId == userId)).FirstOrDefault();
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        // Only owner, admin, or creator can update task content
        if (member.Role != "owner" && member.Role != "admin" && task.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this task");
        }
        
        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            task.Title = request.Title.Trim();
        }
        
        if (request.Description != null)
        {
            task.Description = request.Description;
        }
        
        if (request.DueDate.HasValue)
        {
            if (request.DueDate.Value.Date < DateTime.UtcNow.Date)
            {
                throw new ArgumentException("Due date cannot be in the past");
            }
            task.DueDate = request.DueDate;
        }
        
        // Update assignees if provided
        if (request.AssigneeIds != null)
        {
            // Verify all assignees are workspace members
            foreach (var assigneeId in request.AssigneeIds)
            {
                var isAssigneeMember = await _unitOfWork.WorkspaceMembers.ExistsAsync(wm => 
                    wm.WorkspaceId == task.WorkspaceId && wm.UserId == assigneeId);
                
                if (!isAssigneeMember)
                {
                    throw new ArgumentException($"User {assigneeId} is not a member of this workspace");
                }
            }
            
            // Remove existing assignments
            var existingAssignments = await _unitOfWork.TaskAssignments.FindAsync(ta => ta.TaskId == taskId);
            foreach (var assignment in existingAssignments)
            {
                await _unitOfWork.TaskAssignments.DeleteAsync(assignment);
            }
            
            // Add new assignments
            foreach (var assigneeId in request.AssigneeIds)
            {
                var assignment = new Core.Entities.TaskAssignment
                {
                    TaskId = taskId,
                    UserId = assigneeId,
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.TaskAssignments.AddAsync(assignment);
            }
        }
        
        task.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "Task", task.Id, "task_updated");
        
        // Get assignees for response
        var assignments = await _unitOfWork.TaskAssignments.FindAsync(ta => ta.TaskId == taskId);
        var assignees = new List<AssigneeResponse>();
        
        foreach (var assignment in assignments)
        {
            var assigneeUser = await _unitOfWork.Users.GetByIdAsync(assignment.UserId);
            if (assigneeUser != null)
            {
                assignees.Add(new AssigneeResponse
                {
                    Id = assigneeUser.Id,
                    Name = assigneeUser.Name,
                    Email = assigneeUser.Email
                });
            }
        }
        
        return new TaskResponse
        {
            Id = task.Id,
            WorkspaceId = task.WorkspaceId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            CreatedBy = task.CreatedBy,
            Assignees = assignees,
            CreatedAt = task.CreatedAt
        };
    }
    
    public async Task<TaskResponse> UpdateTaskStatusAsync(int userId, int taskId, UpdateTaskStatusRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }
        
        // Check permissions
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == task.WorkspaceId && wm.UserId == userId)).FirstOrDefault();
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        // Check if user is assignee
        var isAssignee = await _unitOfWork.TaskAssignments.ExistsAsync(ta => 
            ta.TaskId == taskId && ta.UserId == userId);
        
        // Owner, admin, creator, or assignee can update status
        if (member.Role != "owner" && member.Role != "admin" && task.CreatedBy != userId && !isAssignee)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this task status");
        }
        
        // Validate status transition
        var validTransitions = new Dictionary<string, string[]>
        {
            { "pending", new[] { "in_progress" } },
            { "in_progress", new[] { "completed" } }
        };
        
        if (!validTransitions.ContainsKey(task.Status) || 
            !validTransitions[task.Status].Contains(request.Status))
        {
            throw new InvalidOperationException($"Cannot change status from {task.Status} to {request.Status}");
        }
        
        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "Task", task.Id, "task_status_updated");
        
        // Get assignees for response
        var assignments = await _unitOfWork.TaskAssignments.FindAsync(ta => ta.TaskId == taskId);
        var assignees = new List<AssigneeResponse>();
        
        foreach (var assignment in assignments)
        {
            var assigneeUser = await _unitOfWork.Users.GetByIdAsync(assignment.UserId);
            if (assigneeUser != null)
            {
                assignees.Add(new AssigneeResponse
                {
                    Id = assigneeUser.Id,
                    Name = assigneeUser.Name,
                    Email = assigneeUser.Email
                });
            }
        }
        
        return new TaskResponse
        {
            Id = task.Id,
            WorkspaceId = task.WorkspaceId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Status = task.Status,
            CreatedBy = task.CreatedBy,
            Assignees = assignees,
            CreatedAt = task.CreatedAt
        };
    }
    
    public async Task DeleteTaskAsync(int userId, int taskId)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }
        
        // Check permissions - only workspace owner or task creator can delete
        var member = (await _unitOfWork.WorkspaceMembers.FindAsync(wm => 
            wm.WorkspaceId == task.WorkspaceId && wm.UserId == userId)).FirstOrDefault();
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this workspace");
        }
        
        if (member.Role != "owner" && task.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only workspace owner or task creator can delete this task");
        }
        
        // Cascade delete will handle task assignments
        await _unitOfWork.Tasks.DeleteAsync(task);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(userId, "Task", task.Id, "task_deleted");
    }
}