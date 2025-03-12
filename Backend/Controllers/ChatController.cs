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
        private readonly IOptionsMonitor<LLMConfiguration> _modelConfiguration;
        private readonly IVariantFeatureManagerSnapshot _featureManager;

        public ChatController(
            IOpenAIService openAIService,
            ILogger<ChatController> logger,
            IOptionsMonitor<LLMConfiguration> modelConfiguration,
            IVariantFeatureManagerSnapshot featureManager)
        {
            _openAIService = openAIService;
            _logger = logger;
            _modelConfiguration = modelConfiguration;
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
            return Ok((await GetLLMConfiguration(HttpContext.RequestAborted)).Model);
        }

        private async ValueTask<LLMConfiguration> GetLLMConfiguration(CancellationToken cancellationToken)
        {
            return (await _featureManager.IsEnabledAsync(Features.ChatbotLLMFeatureName, cancellationToken)) ?
                _modelConfiguration.Get(Features.ChatLLM2ConfigurationName) :
                _modelConfiguration.Get(Features.ChatLLMConfigurationName);
        }
    }
}
