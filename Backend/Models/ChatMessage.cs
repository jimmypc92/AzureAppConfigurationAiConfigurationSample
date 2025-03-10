namespace AzureAppConfigurationChatBot.Models
{
    public class ChatbotMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatbotMessage> History { get; set; }
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatbotMessage> History { get; set; } = new List<ChatbotMessage>();
    }
}
