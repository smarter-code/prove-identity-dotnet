using System.ComponentModel.DataAnnotations;
using Prove.Proveapi.Models.Components;

namespace ProveIdentityDotnet.Models;

public class CompleteVerificationRequest
{
    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public V3CompleteIndividualRequest Individual { get; set; } = new();
}