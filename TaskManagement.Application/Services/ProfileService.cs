using TaskManagement.Application.DTOs.Profile;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public ProfileService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<ProfileResponse> GetProfileAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }
        
        return new ProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
    
    public async Task<ProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, Stream? avatarStream, string? avatarFileName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }
        
        // Update name
        user.Name = request.Name.Trim();
        
        // Handle avatar
        if (request.RemoveAvatar)
        {
            user.AvatarUrl = null;
        }
        else if (avatarStream != null && !string.IsNullOrEmpty(avatarFileName))
        {
            // Validate file type and size
            var extension = Path.GetExtension(avatarFileName).ToLower();
            if (extension != ".jpg" && extension != ".png")
            {
                throw new ArgumentException("Only JPG and PNG files are allowed");
            }
            
            if (avatarStream.Length > 5 * 1024 * 1024) // 5MB
            {
                throw new ArgumentException("File size must be less than 5MB");
            }
            
            // In real implementation, upload to cloud storage or local storage
            // For now, we'll simulate by setting a placeholder URL
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            
            // In production, you would save the file here:
            // var path = Path.Combine("wwwroot", "uploads", "avatars", fileName);
            // using var fileStream = new FileStream(path, FileMode.Create);
            // await avatarStream.CopyToAsync(fileStream);
        }
        
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.CompleteAsync();
        
        await _auditService.LogAsync(user.Id, "User", user.Id, "profile_updated");
        
        return new ProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}