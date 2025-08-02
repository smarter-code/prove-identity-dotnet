using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Prove.Proveapi;
using Prove.Proveapi.Models.Components;
using ProveIdentityDotnet.Models;

public class ProveVerificationService
{
    private readonly ProveSettings _settings;
    private readonly ILogger<ProveVerificationService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly SDKConfig.Server _serverConfig;
    private const string TokenCacheKey = "ProveAPI_AccessToken";

    public ProveVerificationService(
        IOptions<ProveSettings> settings, 
        ILogger<ProveVerificationService> logger,
        IMemoryCache memoryCache)
    {
        _settings = settings.Value;
        _logger = logger;
        _memoryCache = memoryCache;

        // Initialize Prove SDK client with the specified server environment
        _serverConfig = _settings.ServerEnvironment switch
        {
            "uat-us" => SDKConfig.Server.UatUs,
            "prod-us" => SDKConfig.Server.ProdUs,
            "uat-eu" => SDKConfig.Server.UatEu,
            "prod-eu" => SDKConfig.Server.ProdEu,
            _ => SDKConfig.Server.UatUs
        };
    }

    private async Task<string> GetAccessTokenAsync()
    {
        // Try to get token from cache first
        if (_memoryCache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        try
        {
            var tokenRequest = new V3TokenRequest
            {
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret,
                GrantType = "client_credentials"
            };

            var proveClient = new ProveAPI(server: _serverConfig);
            var tokenResponse = await proveClient.V3.V3TokenRequestAsync(tokenRequest);

            if (tokenResponse.V3TokenResponse != null)
            {
                var accessToken = tokenResponse.V3TokenResponse.AccessToken;
                
                // Cache the token with expiration (subtract 5 minutes for buffer)
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.V3TokenResponse.ExpiresIn - 300),
                    Priority = CacheItemPriority.High,
                    Size = 1
                };

                _memoryCache.Set(TokenCacheKey, accessToken, cacheOptions);
                
                _logger.LogDebug("Access token cached for {ExpirationTime} seconds", 
                    tokenResponse.V3TokenResponse.ExpiresIn - 300);

                return accessToken;
            }

            throw new InvalidOperationException("Failed to obtain access token from Prove API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining access token from Prove API");
            throw new ApplicationException("Authentication with Prove API failed", ex);
        }
    }

    public async Task<StartVerificationResponse> StartVerificationAsync(StartVerificationRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var startRequest = new V3StartRequest
            {
                PhoneNumber = request.PhoneNumber,
                FinalTargetUrl = "https://www.example.com",
                FlowType = request.FlowType,
                Ssn = request.LastFourSSN,
                AllowOTPRetry = true,
                IpAddress = "127.0.0.1" // You might want to get the actual client IP
            };

            var proveClient = new ProveAPI(server: _serverConfig, auth: accessToken);
            var response = await proveClient.V3.V3StartRequestAsync(startRequest);

            if (response.V3StartResponse != null)
            {
                return new StartVerificationResponse
                {
                    AuthToken = response.V3StartResponse.AuthToken ?? string.Empty,
                    CorrelationId = response.V3StartResponse.CorrelationId ?? string.Empty
                };
            }

            throw new InvalidOperationException("Failed to initiate verification flow");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating verification flow");
            throw new ApplicationException("Failed to start verification process", ex);
        }
    }

    public async Task<object> ValidatePhoneAsync(ValidateVerificationRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var validateRequest = new V3ValidateRequest
            {
                CorrelationId = request.CorrelationId
            };

            var proveClient = new ProveAPI(server: _serverConfig, auth: accessToken);
            var response = await proveClient.V3.V3ValidateRequestAsync(validateRequest);

            if (response.V3ValidateResponse != null)
            {
                return new { success = true, data = response.V3ValidateResponse };
            }

            return new { success = false, message = "Phone validation failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating phone number");
            throw new ApplicationException("Phone validation failed", ex);
        }
    }

    public async Task<object> CompleteVerificationAsync(CompleteVerificationRequest request)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var completeRequest = new V3CompleteRequest
            {
                CorrelationId = request.CorrelationId,
                Individual = (V3CompleteIndividualRequest)request.Individual
            };

            var proveClient = new ProveAPI(server: _serverConfig, auth: accessToken);
            var response = await proveClient.V3.V3CompleteRequestAsync(completeRequest);

            if (response != null)
            {
                return new { success = true, data = response.V3CompleteResponse };
            }

            return new { success = false, message = "Verification completion failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing verification");
            throw new ApplicationException("Verification completion failed", ex);
        }
    }
}