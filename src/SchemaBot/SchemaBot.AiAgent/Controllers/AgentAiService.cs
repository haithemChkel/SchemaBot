using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SchemaBot.SettingService.Client;
using SchemaBot.SettingService.Core;

namespace SchemaBot.AiAgent.Controllers
{
    // AgentAiService.cs
    public class AgentAiService : IAgentAiService
    {
        private readonly ISchemaBotApi _settingsClient;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly Kernel _kernel;
        private readonly ILogger<AgentAiService> _logger;

        public AgentAiService(
            ISchemaBotApi settingsClient,
            ILogger<AgentAiService> logger,
            IServiceProvider serviceProvider,
            Kernel kernel)
        {
            _settingsClient = settingsClient;
            _logger = logger;
            _chatCompletionService = serviceProvider.GetRequiredKeyedService<IChatCompletionService>("Ollama");
            _kernel = kernel;
        }

        private IEnumerable<string> ChunkText(string text, int chunkSize = 2048)
        {
            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }
            return chunks;
        }

        private async Task<string> ProcessChunkedPromptAsync(string schema, string context, string userQuery)
        {
            var schemaChunks = ChunkText(schema);
            var contextChunks = ChunkText(context);

            ChatHistory history = [];

            foreach (var chunk in schemaChunks)
            {
                history.AddUserMessage($"## API Schema (Part)\n{chunk}");
                var response = await _chatCompletionService.GetChatMessageContentAsync(history, kernel: _kernel);
                if (response.InnerContent is string text) history.AddAssistantMessage(text);
            }

            foreach (var chunk in contextChunks)
            {
                history.AddUserMessage($"## Context Instructions (Part)\n{chunk}");
                var response = await _chatCompletionService.GetChatMessageContentAsync(history, kernel: _kernel);
                if (response.InnerContent is string text) history.AddAssistantMessage(text);
            }

            // Final step: process user query after chunking schema/context
            history.AddUserMessage(GetFormatMessage());
            history.AddUserMessage($"## User Query\n{userQuery}\n\nGenerate only valid JSON response matching the schema.");
            var finalResponse = await _chatCompletionService.GetChatMessageContentAsync(history, kernel: _kernel);

            return finalResponse.InnerContent is string finalText ? finalText : string.Empty;
        }

        public async Task<ApiCommand> ProcessUserQueryAsync(UserQueryEvent query)
        {
            // Fetch schema and context
            var apiConfig = await _settingsClient.GetConfigurationAsync(query.ApiConfigId);

            // Process chunked prompt
            var responseText = await ProcessChunkedPromptAsync(
                JsonSerializer.Serialize(apiConfig.Content.SchemaJson),
                string.Join("\n", apiConfig.Content.ContextPrompts.Select(p => p.Prompt)),
                query.Text
            );

            return ValidateAndParseResponse(responseText);
        }

        private string GetFormatMessage()
        {
            return $@"
                ## Response Format
                {{
                    ""endpoint"": ""string"",
                    ""method"": ""GET|POST|PUT|DELETE"",
                    ""parameters"": {{}},
                    ""body"": {{}}
                }}";
        }


        private ApiCommand ValidateAndParseResponse(string response)
        {
            try
            {
                var command = JsonSerializer.Deserialize<ApiCommand>(response);

                if (string.IsNullOrEmpty(command.Endpoint) ||
                    string.IsNullOrEmpty(command.Method))
                {
                    throw new InvalidOperationException("Invalid API command structure");
                }

                return command;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse model response");
                throw new InvalidOperationException("Invalid response format from AI model");
            }
        }
    }

}
