using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Invitation;

public class CreateInvitationRequest
{
    [Required]
    [EmailAddress]
    public string InviteeEmail { get; set; } = string.Empty;
}