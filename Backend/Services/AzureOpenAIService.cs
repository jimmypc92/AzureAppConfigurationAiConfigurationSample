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
        private readonly IOptionsMonitor<LLMConfiguration> _modelConfiguration;
        private readonly IVariantFeatureManagerSnapshot _featureManager;

        public AzureOpenAIService(
            IOptions<AzureOpenAIConnectionInfo> connectionInfo,
            IOptionsMonitor<LLMConfiguration> modelConfiguration,
            IVariantFeatureManagerSnapshot featureManager)
        {
            if (connectionInfo?.Value == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            _modelConfiguration = modelConfiguration ??
                throw new ArgumentNullException(nameof(modelConfiguration));

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
            var messages = await GetSystemMessages(cancellationToken);

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

        private async ValueTask<List<ChatMessage>> GetSystemMessages(CancellationToken cancellationToken)
        {
            var messages = new List<ChatMessage>();

            var llmConfiguration = await GetLLMConfiguration(cancellationToken);

            foreach (MessageConfiguration messageConfiguration in
                llmConfiguration.Messages.Where(x => x.Role == "system"))
            {
                messages.Add(new SystemChatMessage(messageConfiguration.Content));
            }

            return messages;
        }

        private async ValueTask<ChatCompletion> GetCompletion(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            var llmConfiguration = await GetLLMConfiguration(cancellationToken);

            ChatClient chatClient = _client.GetChatClient(llmConfiguration.Model);

            // Create chat completion options if needed
            ChatCompletionOptions options = new ChatCompletionOptions
            {
                Temperature = llmConfiguration.Temperature,
                MaxOutputTokenCount = llmConfiguration.MaxCompletionTokens
            };

            // Call Azure OpenAI
            return await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        }

        private async ValueTask<LLMConfiguration> GetLLMConfiguration(CancellationToken cancellationToken)
        {
            return (await _featureManager.IsEnabledAsync(Features.ChatbotLLMFeatureName, cancellationToken)) ?
                _modelConfiguration.Get(Features.ChatLLM2ConfigurationName) :
                _modelConfiguration.Get(Features.ChatLLMConfigurationName);
        }
    }
}