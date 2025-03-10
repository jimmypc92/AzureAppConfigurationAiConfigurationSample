using Microsoft.AspNetCore.Mvc;
using AzureAppConfigurationChatBot.Models;
using AzureAppConfigurationChatBot.Services;
using Microsoft.Extensions.Options;

namespace AzureAppConfigurationChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<ChatController> _logger;
        private readonly IOptionsMonitor<AIModelConfiguration> _modelConfiguration;

        public ChatController(IOpenAIService openAIService, ILogger<ChatController> logger, IOptionsMonitor<AIModelConfiguration> modelConfiguration)
        {
            _openAIService = openAIService;
            _logger = logger;
            _modelConfiguration = modelConfiguration;
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

                var response = await _openAIService.GetChatCompletionAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpGet("model")]
        public ActionResult<string> GetModelName()
        {
            return Ok(_modelConfiguration.CurrentValue.Model);
        }
    }
}
