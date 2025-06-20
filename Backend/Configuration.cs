namespace AzureAppConfigurationChatBot
{
    /// <summary>
    /// Represents connection information required to authenticate with Azure OpenAI services.
    /// This class contains the credentials and endpoint information needed to establish a connection.
    /// </summary>
    public class AzureOpenAIConnectionInfo
    {
        /// <summary>
        /// Gets or sets the API key used for authentication with Azure OpenAI services.
        /// </summary>
        /// <value>A string containing the authentication key.</value>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the endpoint URL for the Azure OpenAI service.
        /// </summary>
        /// <value>A string containing the service endpoint URL.</value>
        public string Endpoint { get; set; }
    }

    /// <summary>
    /// Represents configuration settings for interacting with a Large Language Model (LLM).
    /// This class defines parameters that control the behavior and output of AI model interactions.
    /// </summary>
    public class CompletionConfiguration
    {
        /// <summary>
        /// Gets or sets the identifier of the LLM to use for processing.
        /// </summary>
        /// <value>The name or version of the AI model.</value>
        [ConfigurationKeyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the temperature parameter that controls randomness in the model's responses.
        /// Lower values produce more deterministic outputs, while higher values increase creativity and variability.
        /// </summary>
        [ConfigurationKeyName("temperature")]
        public float Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens the model should generate in its response.
        /// </summary>
        /// <value>An integer representing the token limit for completions.</value>
        [ConfigurationKeyName("max_tokens")]
        public int MaxCompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the collection of messages to be used in the conversation with the LLM.
        /// These typically include system prompts, user inputs, and previous model responses.
        /// </summary>
        [ConfigurationKeyName("messages")]
        public List<MessageConfiguration> Messages { get; set; } = new List<MessageConfiguration>();

        /// <summary>
        /// Gets or sets the top_p parameter.
        /// Lower values make the output more focused, while higher values increase diversity.
        /// </summary>
        /// <value>A float representing the nucleus sampling probability threshold.</value>
        [ConfigurationKeyName("top_p")]
        public float TopP { get; set; }
    }

    /// <summary>
    /// Represents a message configuration for communication with an AI model.
    /// This class defines the structure of messages exchanged in a conversation with the LLM.
    /// </summary>
    public class MessageConfiguration
    {
        /// <summary>
        /// Gets or sets the role of the message sender in the conversation.
        /// Common roles include "system", "user", and "assistant".
        /// </summary>
        /// <value>A string representing the message sender's role.</value>
        [ConfigurationKeyName("role")]
        public required string Role { get; set; }

        /// <summary>
        /// Gets or sets the actual content of the message to be processed by the LLM.
        /// </summary>
        /// <value>A string containing the message text.</value>
        [ConfigurationKeyName("content")]
        public string Content { get; set; }
    }
}