using MicrosoftChatbot.Models;

namespace MicrosoftChatbot.Services
{
    public interface IOpenAIService
    {
        Task<ChatResponse> GetChatCompletionAsync(ChatRequest request);
    }
}
