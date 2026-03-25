using System.Text.Json;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    
    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _auditService = auditService;
    }
    
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Step 1: Normalize email to lowercase
        var normalizedEmail = request.Email.Trim().ToLower();
        
        // Step 2: Check if email already exists
        var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == normalizedEmail)).FirstOrDefault();
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }
        
        // Step 3: Hash password
        var passwordHash = _passwordService.HashPassword(request.Password);
        
        // Step 4: Create user
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Name = request.Name.Trim(),
            Status = "active",
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();
        
        // Step 5: Audit log
        await _auditService.LogAsync(
            user.Id, 
            "User", 
            user.Id, 
            "user_created");
        
        // Step 6: Return response
        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
}