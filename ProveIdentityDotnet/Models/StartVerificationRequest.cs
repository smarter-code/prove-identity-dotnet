using System.ComponentModel.DataAnnotations;

namespace ProveIdentityDotnet.Models;

public class StartVerificationRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(4, MinimumLength = 4)]
    public string LastFourSSN { get; set; } = string.Empty;

    [Required]
    public string FlowType { get; set; } = string.Empty;
}