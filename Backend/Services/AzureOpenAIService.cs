using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using AzureAppConfigurationChatBot.Models;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace AzureAppConfigurationChatBot.Services
{
    public class AzureOpenAIService : IOpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly IOptionsMonitor<CompletionConfiguration> _completionConfiguration;

        public AzureOpenAIService(
            IOptions<AzureOpenAIConnectionInfo> connectionInfo,
            IOptionsMonitor<CompletionConfiguration> completionConfiguration)
        {
            if (connectionInfo?.Value == null)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            _completionConfiguration = completionConfiguration ??
                throw new ArgumentNullException(nameof(completionConfiguration));

            string endpoint = connectionInfo.Value.Endpoint;

            string key = connectionInfo.Value.ApiKey;

            // Use key authentication if key is provided, otherwise use DefaultAzureCredential
            _client = !string.IsNullOrEmpty(key)
                ? new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
                : new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        }

        public async Task<ChatResponse> GetChatCompletionAsync(ChatRequest request)
        {
            // Create a list of messages from the history
            List<ChatMessage> messages = _completionConfiguration
                .CurrentValue
                .Messages
                .Where(x => x.Role == "system")
                .Select(x => new SystemChatMessage(x.Content))
                .ToList<ChatMessage>();

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
            string responseContent = (await GetCompletion(messages)).Content[0].Text;

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

        private async Task<ChatCompletion> GetCompletion(IEnumerable<ChatMessage> messages)
        {
            ChatClient chatClient = _client.GetChatClient(_completionConfiguration.CurrentValue.Model);

            // Create chat completion options if needed
            ChatCompletionOptions options = new ChatCompletionOptions
            {
                Temperature = _completionConfiguration.CurrentValue.Temperature,
                MaxOutputTokenCount = _completionConfiguration.CurrentValue.MaxCompletionTokens,
                TopP = _completionConfiguration.CurrentValue.TopP
            };

            //
            // Prepend system messages
            IEnumerable<ChatMessage> systemMessages = _completionConfiguration
                .CurrentValue
                .Messages
                .Where(x => x.Role == "system")
                .Select(x => new SystemChatMessage(x.Content));

            messages = systemMessages.Concat(messages);

            // Call Azure OpenAI
            return await chatClient.CompleteChatAsync(messages, options);
        }
    }
}