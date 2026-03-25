using TaskManagement.Application.DTOs.Profile;

namespace TaskManagement.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetProfileAsync(int userId);
    Task<ProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, Stream? avatarStream, string? avatarFileName);
}