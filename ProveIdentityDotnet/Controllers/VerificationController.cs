using Microsoft.AspNetCore.Mvc;
using ProveIdentityDotnet.Models;

namespace ProveIdentity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly ProveVerificationService _proveService;
        private readonly ILogger<VerificationController> _logger;

        public VerificationController(ProveVerificationService proveService, ILogger<VerificationController> logger)
        {
            _proveService = proveService;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<StartVerificationResponse>>> StartVerification([FromBody] StartVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<StartVerificationResponse>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        )
                    });
                }

                var result = await _proveService.StartVerificationAsync(request);

                return Ok(new ApiResponse<StartVerificationResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Verification initiated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating verification");
                return StatusCode(500, new ApiResponse<StartVerificationResponse>
                {
                    Success = false,
                    Message = "An error occurred while initiating verification"
                });
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<object>>> ValidatePhone([FromBody] ValidateVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _proveService.ValidatePhoneAsync(request);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "Phone validation completed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during phone validation"
                });
            }
        }

        [HttpPost("complete")]
        public async Task<ActionResult<ApiResponse<object>>> CompleteVerification([FromBody] CompleteVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _proveService.CompleteVerificationAsync(request);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "Verification completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing verification");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while completing verification"
                });
            }
        }
    }
}