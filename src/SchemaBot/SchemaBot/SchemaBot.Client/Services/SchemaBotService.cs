using SchemaBot.SettingService.Client;

namespace SchemaBot.Client.Services
{
    public class SchemaBotService
    {
        private readonly ISchemaBotApi _api;
        private readonly IAuthTokenProvider _tokenProvider;

        public SchemaBotService(
            ISchemaBotApi api,
            IAuthTokenProvider tokenProvider)
        {
            _api = api;
            _tokenProvider = tokenProvider;
        }

        public async Task InitializeAsync(string username, string password)
        {
            var loginResponse = await _api.LoginAsync(new LoginRequest(
                Username: username,
                Password: password));

            if (loginResponse.IsSuccessStatusCode)
            {
                await _tokenProvider.SetTokenAsync(loginResponse.Content.Token);
            }
        }

        public async Task<ApiConfiguration> UpdateConfigAsync(
            Guid configId,
            ApiConfiguration updatedConfig)
        {
            var response = await _api.UpdateConfigurationAsync(configId, updatedConfig);
            return response.Content;
        }
    }
}
