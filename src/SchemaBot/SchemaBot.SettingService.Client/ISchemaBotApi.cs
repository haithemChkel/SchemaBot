namespace SchemaBot.SettingService.Client
{
    // SchemaBotClient.cs
    using Refit;
    using System.Threading.Tasks;

    public interface ISchemaBotApi
    {
        [Get("/api/configurations")]
        Task<ApiResponse<List<ApiConfiguration>>> GetConfigurationsAsync();

        [Get("/api/configurations/{id}")]
        Task<ApiResponse<ApiConfiguration>> GetConfigurationAsync(Guid id);

        [Put("/api/configurations/{id}")]
        Task<ApiResponse<ApiConfiguration>> UpdateConfigurationAsync(
            Guid id,
            [Body] ApiConfiguration configuration);

        [Post("/api/configurations")]
        Task<ApiResponse<ApiConfiguration>> CreateConfigurationAsync(
            [Body] ApiConfiguration configuration);

        [Post("/api/context-prompts")]
        Task<ApiResponse<ContextPrompt>> CreateContextPromptAsync(
        [Body] ContextPrompt prompt);

        [Post("/api/auth-configurations")]
        Task<ApiResponse<AuthConfiguration>> CreateAuthConfigurationAsync(
            [Body] AuthConfiguration authConfig);

        [Post("/login")]
        Task<ApiResponse<LoginResponse>> LoginAsync(
            [Body] LoginRequest request);
    }

    public record LoginResponse(string Token);
}
