using AzureAppConfigurationChatBot.Models;

namespace AzureAppConfigurationChatBot.Services
{
    public interface IOpenAIService
    {
        Task<ChatResponse> GetChatCompletionAsync(ChatRequest request);
    }
}
