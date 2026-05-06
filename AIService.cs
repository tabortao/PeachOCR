using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PeachOCR
{
    public class AIService : IDisposable
    {
        private readonly ChatClient _chatClient;
        private readonly AISettings _settings;

        public AIService(AISettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (!settings.IsConfigured)
            {
                throw new InvalidOperationException("AI settings are not properly configured");
            }

            // Initialize the OpenAI chat client with custom endpoint
            var credentials = new ApiKeyCredential(settings.ApiKey);
            var openAIClientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(settings.ApiUrl)
            };
            var openAIClient = new OpenAIClient(credentials, openAIClientOptions);
            _chatClient = openAIClient.GetChatClient(settings.ModelName);
        }

        public async Task<string> EnhanceOCRTextAsync(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                return string.Empty;
            }

            var prompt = _settings.OcrEnhancementPrompt + Environment.NewLine + ocrText;

            try
            {
                var response = await _chatClient.CompleteChatAsync(prompt);
                return response.Value.Content[0].Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"AI text enhancement failed: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalyzeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var prompt = _settings.AnalysisPrompt + Environment.NewLine + text;

            try
            {
                var response = await _chatClient.CompleteChatAsync(prompt);
                return response.Value.Content[0].Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"AI text analysis failed: {ex.Message}", ex);
            }
        }

        public async Task<string> TranslateTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var prompt = _settings.TranslationPrompt + Environment.NewLine + text;

            try
            {
                var response = await _chatClient.CompleteChatAsync(prompt);
                return response.Value.Content[0].Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"AI translation failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testPrompt = "Hello, this is a test message. Please respond with 'OK'.";
                var response = await _chatClient.CompleteChatAsync(testPrompt);
                var responseText = response.Value.Content[0].Text;

                return !string.IsNullOrEmpty(responseText) &&
                       responseText.Contains("OK", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            // ChatClient doesn't implement IDisposable in this version
            // No cleanup needed
        }
    }
}