using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using AzureAppConfigurationChatBot.Models;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace AzureAppConfigurationChatBot.Services
{
    public class AzureOpenAIService : IOpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly IConfiguration _configuration;
        private readonly IVariantFeatureManager _featureManager;

        public AzureOpenAIService(
            IOptions<AzureOpenAIConnectionInfo> connectionInfo,
            IConfiguration configuration,
            IVariantFeatureManager featureManager)
        {
            if (connectionInfo?.Value == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

            _featureManager = featureManager ??
                throw new ArgumentNullException(nameof(featureManager));

            string endpoint = connectionInfo.Value.Endpoint;

            string key = connectionInfo.Value.ApiKey;

            // Use key authentication if key is provided, otherwise use DefaultAzureCredential
            _client = !string.IsNullOrEmpty(key)
                ? new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
                : new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        }

        public async ValueTask<ChatResponse> GetChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken)
        {
            // Create a list of messages from the history
            List<ChatMessage> messages = new();

            // Add conversation history if available
            if (request.History != null)
            {
                foreach (ChatbotMessage message in request.History)
                {
                    if (message.Role.ToLower() == "user")
                    {
                        messages.Add(new UserChatMessage(message.Content));
                    }
                    else if (message.Role.ToLower() == "assistant")
                    {
                        messages.Add(new AssistantChatMessage(message.Content));
                    }
                }
            }

            // Add the current user message
            messages.Add(new UserChatMessage(request.Message));

            // Get the response content
            string responseContent = (await GetCompletion(messages, cancellationToken)).Content[0].Text;

            // Create response object with updated history
            List<ChatbotMessage> history = request.History ?? new List<ChatbotMessage>();

            // Add user's message to history
            history.Add(new ChatbotMessage
            {
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });

            // Add assistant's response to history
            history.Add(new ChatbotMessage
            {
                Role = "assistant",
                Content = responseContent,
                Timestamp = DateTime.UtcNow
            });

            return new ChatResponse
            {
                Message = responseContent,
                History = history
            };
        }

        private async ValueTask<ChatCompletion> GetCompletion(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            CompletionConfiguration completionConfiguration = await GetCompletionConfiguration(cancellationToken);

            ChatClient chatClient = _client.GetChatClient(completionConfiguration.Model);

            // Create chat completion options if needed
            ChatCompletionOptions options = new ChatCompletionOptions
            {
                Temperature = completionConfiguration.Temperature,
                MaxOutputTokenCount = completionConfiguration.MaxCompletionTokens,
                TopP = completionConfiguration.TopP
            };

            //
            // Prepend system messages
            IEnumerable<ChatMessage> systemMessages = completionConfiguration
                .Messages
                .Where(x => x.Role == "system")
                .Select(x => new SystemChatMessage(x.Content));

            messages = systemMessages.Concat(messages);

            // Call Azure OpenAI
            return await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        }

        private async ValueTask<CompletionConfiguration> GetCompletionConfiguration(CancellationToken cancellationToken)
        {
            Variant variant = await _featureManager.GetVariantAsync(Features.CompletionFeatureName, cancellationToken);

            return _configuration.GetSection(variant.Configuration.Value).Get<CompletionConfiguration>();
        }
    }
}