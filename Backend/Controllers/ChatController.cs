using Microsoft.AspNetCore.Mvc;
using AzureAppConfigurationChatBot.Models;
using AzureAppConfigurationChatBot.Services;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace AzureAppConfigurationChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<ChatController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IVariantFeatureManagerSnapshot _featureManager;

        public ChatController(
            IOpenAIService openAIService,
            ILogger<ChatController> logger,
            IConfiguration configuration,
            IVariantFeatureManagerSnapshot featureManager)
        {
            _openAIService = openAIService;
            _logger = logger;
            _configuration = configuration;
            _featureManager = featureManager;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest("Message cannot be empty");
                }

                var response = await _openAIService.GetChatCompletionAsync(request, HttpContext.RequestAborted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpGet("model")]
        public async Task<ActionResult<string>> GetModelName()
        {
            return Ok((await GetCompletionConfiguration(HttpContext.RequestAborted)).Model);
        }

        private async ValueTask<CompletionConfiguration> GetCompletionConfiguration(CancellationToken cancellationToken)
        {
            Variant variant = await _featureManager.GetVariantAsync(Features.CompletionFeatureName, cancellationToken);

            return _configuration.GetSection(variant.Configuration.Value).Get<CompletionConfiguration>();
        }
    }
}
