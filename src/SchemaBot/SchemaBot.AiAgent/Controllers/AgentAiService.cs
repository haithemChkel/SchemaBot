using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp.Models.Chat;
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

        public async Task<ApiCommand> ProcessUserQueryAsync(UserQueryEvent query)
        {
            // Step 1: Fetch schema and context
            var apiConfig = await _settingsClient.GetConfigurationAsync(query.ApiConfigId);
            ChatHistory history = [];

            // var definitionPrompt = BuildDefintionAugmentedPrompt(query, apiConfig.Content);
            // history.AddUserMessage(definitionPrompt);
            // var responseOfDefinition = await _chatCompletionService.GetChatMessageContentAsync(
            //    history,
            //    kernel: _kernel
            //);
            // if (responseOfDefinition.InnerContent 
            //     is OllamaSharp.Models.Chat.ChatDoneResponseStream streamDef)
            // {
            //     var definition = streamDef.Message.Content;
            // }
            // Step 2: Build augmented prompt
            var prompt = BuildAugmentedPrompt(query, apiConfig.Content);
            Console.WriteLine(prompt);
            history.AddUserMessage(prompt);
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                  history,
                  kernel: _kernel
              );
            if (response.InnerContent is OllamaSharp.Models.Chat.ChatDoneResponseStream stream)
            {
                return ValidateAndParseResponse(stream.Message.Content);
            }
            else if (response.InnerContent is string text)
            {
                return ValidateAndParseResponse(text);
            }
            else return default;
        }

        private string BuildAugmentedPrompt(UserQueryEvent query, ApiConfiguration config)
        {
            var schema = JsonSerializer.Serialize(config.SchemaJson);
            var context = string.Join("\n", config.ContextPrompts.Select(p => p.Prompt));

            return $@"
                ## Context Instructions
                You are an intelligent API assistant capable of executing queries on any REST API by analyzing its Swagger (OpenAPI) definition file. Your task is to:  
                1. Analyse Path one by one and then identify the correct API action from the Swagger JSON definition.  
                2. Extract the HTTP method, endpoint, and required parameters.  
                3. Format the API request for execution.  
                4. Return the response as a structured JSON FORMAT as Response Format section.  
                {context}
                ## API Schema
                {config.SchemaJson}

                ## User Query
                {query.Text}

                ## Response Format
                {{
                    ""endpoint"": ""string"",
                    ""method"": ""GET|POST|PUT|DELETE"",
                    ""parameters"": {{}},
                    ""body"": {{}}
                }}

                Generate only valid JSON response matching the schema.";
        }

        private string BuildDefintionAugmentedPrompt(UserQueryEvent query, ApiConfiguration config)
        {
            return $@"
                ## Context Instructions
                You are an intelligent API assistant capable of analyzing  Swagger (OpenAPI) definition file. Your task is to:  
                1. Analyse Paths one by one and then Swagger JSON definition.  
                2. By path extract the HTTP method, endpoint, and required parameters.  
                ## API Schema
                {config.SchemaJson}";
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
