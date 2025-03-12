using AzureAppConfigurationChatBot.Models;

namespace AzureAppConfigurationChatBot.Services
{
    public interface IOpenAIService
    {
        ValueTask<ChatResponse> GetChatCompletionAsync(ChatRequest request, CancellationToken cancellationToken);
    }
}
