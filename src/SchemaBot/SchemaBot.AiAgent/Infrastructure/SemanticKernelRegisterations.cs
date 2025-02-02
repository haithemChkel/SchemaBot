using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace SchemaBot.AiAgent.Infrastructure
{
    public static class SemanticKernelRegisterations
    {
        private const string ServiceId = "Ollama";
        private const int MaxInflightAutoInvokes = 128;
        public static void RegisterSemanticKernel(this WebApplicationBuilder builder)
        {

            builder.Services.AddKeyedSingleton(ServiceId, (serviceProvider, _) =>
            {
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(@"http://localhost:11434/");
                client.Timeout = TimeSpan.FromMinutes(5);
                var builder = ((IChatClient)new OllamaApiClient(client, "phi3"))
                    .AsBuilder()
                    .UseFunctionInvocation(loggerFactory, config => config.MaximumIterationsPerRequest = MaxInflightAutoInvokes);

                if (loggerFactory is not null)
                {
                    builder.UseLogging(loggerFactory);
                }

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                return builder.Build(serviceProvider).AsChatCompletionService(serviceProvider);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            });

            builder.Services.AddTransient((serviceProvider) =>
            {
                return new Kernel(serviceProvider);
            });
        }
    }
}
