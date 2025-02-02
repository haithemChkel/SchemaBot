namespace SchemaBot.SettingService.Client
{
    // RegistrationExtensions.cs
    using Microsoft.Extensions.DependencyInjection;
    using Polly.Extensions.Http;
    using Polly;
    using Refit;
    using System.Net.Http.Headers;
    using System.Text.Json.Serialization;
    using System.Text.Json;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSettingServiceClient(
            this IServiceCollection services,
            string baseAddress)
        {
            services.AddRefitClient<ISchemaBotApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            Converters = { new JsonStringEnumConverter() }
                        })
            })
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress))
              //  .AddHttpMessageHandler<AuthHeaderHandler>()
                .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }

    // AuthHeaderHandler.cs
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IAuthTokenProvider _tokenProvider;

        public AuthHeaderHandler(IAuthTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }

    // IAuthTokenProvider.cs
    public interface IAuthTokenProvider
    {
        Task<string?> GetTokenAsync();
        Task SetTokenAsync(string token);
    }
}
