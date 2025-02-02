namespace SchemaBot.Client.Services
{
    using SchemaBot.SettingService.Client;
    // Services/AuthenticationService.cs
    using System.Net.Http.Json;

    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthTokenProvider _tokenProvider;

        public AuthenticationService(
            HttpClient httpClient,
            IAuthTokenProvider tokenProvider)
        {
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
        }

        public async Task<LoginResult> LoginAsync(LoginModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/login", model);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    await _tokenProvider.SetTokenAsync(result.Token);
                    return LoginResult.Success();
                }

                return LoginResult.Failure("Invalid username or password");
            }
            catch
            {
                return LoginResult.Failure("Connection error");
            }
        }
    }

    public record LoginResponse(string Token);
    public record LoginResult(bool IsSuccess, string ErrorMessage = "")
    {
        public static LoginResult Success() => new(true);
        public static LoginResult Failure(string message) => new(false, message);
    }
}
