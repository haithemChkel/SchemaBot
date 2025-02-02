using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SchemaBot.SettingService
{
    public static class JwtExtensions
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var key = config["Jwt:Key"];
            if (string.IsNullOrEmpty(key) || Convert.FromBase64String(key).Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT key must be at least 256 bits (32 bytes) in Base64 format");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(key)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });
        }
    }
}
