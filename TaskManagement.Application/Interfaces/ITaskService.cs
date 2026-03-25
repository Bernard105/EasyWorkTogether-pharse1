using TaskManagement.Application.DTOs.Task;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateTaskAsync(int userId, int workspaceId, CreateTaskRequest request);
    Task<TaskResponse> UpdateTaskAsync(int userId, int taskId, UpdateTaskRequest request);
    Task<TaskResponse> UpdateTaskStatusAsync(int userId, int taskId, UpdateTaskStatusRequest request);
    Task DeleteTaskAsync(int userId, int taskId);
}