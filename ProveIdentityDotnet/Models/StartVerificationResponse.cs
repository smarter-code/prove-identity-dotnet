namespace ProveIdentityDotnet.Models;

public class StartVerificationResponse
{
    public string AuthToken { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}