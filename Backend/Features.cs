using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace AzureAppConfigurationChatBot
{
    /// <summary>
    /// Describes all constants related to features within this application
    /// Includes feature names as well as configuration names tied to features
    /// </summary>
    public static class Features
    {
        public const string ChatbotLLMFeatureName = "NewChatLLMVersion";

        public const string ChatLLMConfigurationName = "ChatLLM";
        public const string ChatLLM2ConfigurationName = "ChatLLM-2";
    }
}