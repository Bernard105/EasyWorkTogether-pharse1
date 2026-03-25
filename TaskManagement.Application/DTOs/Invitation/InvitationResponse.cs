namespace TaskManagement.Application.DTOs.Invitation;

public class InvitationResponse
{
    public int Id { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}