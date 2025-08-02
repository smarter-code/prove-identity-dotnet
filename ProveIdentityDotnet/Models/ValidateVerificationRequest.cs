using System.ComponentModel.DataAnnotations;

namespace ProveIdentityDotnet.Models;

public class ValidateVerificationRequest
{
    [Required]
    public string CorrelationId { get; set; } = string.Empty;
}