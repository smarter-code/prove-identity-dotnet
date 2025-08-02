namespace ProveIdentityDotnet.Models;

public class ProveSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ServerEnvironment { get; set; } = "uat-us";
}