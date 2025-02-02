// Program.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchemaBot.SettingService.Core;

public  static partial class ConfigureSettingService
{
    public static void MapSettingService(this WebApplication app, string jwtKey)
    {
        app.MapPut("/api/configurations/{id}",
            //[Authorize(Roles = "Editor,Admin")]
        async (Guid id, ApiConfiguration updatedConfig, SchemaBotDbContext context) =>
    {
        var existingConfig = await context.ApiConfigurations.FindAsync(id);
        if (existingConfig == null) return Results.NotFound();

        if (updatedConfig.SchemaType == SchemaType.Swagger &&
            !SwaggerValidator.IsValidSwagger3(updatedConfig.SchemaJson))
        {
            return Results.Problem("Invalid Swagger 3.0 specification");
        }

        existingConfig.Name = updatedConfig.Name;
        existingConfig.SchemaJson = updatedConfig.SchemaJson;
        existingConfig.SchemaType = updatedConfig.SchemaType;

        context.ApiConfigurations.Update(existingConfig);
        await context.SaveChangesAsync();
        return Results.Ok(existingConfig);
    }).Produces<ApiConfiguration>();

        // Existing endpoints from previous implementation
        app.MapGet("/api/configurations",
          //[Authorize(Roles = "Viewer,Editor,Admin")]
        async (SchemaBotDbContext context, AesEncryptionService aes) =>
          {
              var configs = await context.ApiConfigurations
                  .Include(a => a.ContextPrompts)
                  .Include(a => a.AuthConfig)
                  .ToListAsync();

              foreach (var config in configs)
              {
                  if (config?.AuthConfig != null)
                  {
                      config.AuthConfig.EncryptedCredentials = aes.Decrypt(config.AuthConfig.EncryptedCredentials);
                  }
              }
              return configs != null ? Results.Ok(configs) : Results.NotFound();
          }).Produces<ApiConfiguration>();

        app.MapGet("/api/configurations/{id}",
            //[Authorize(Roles = "Viewer,Editor,Admin")]
        async (Guid id, SchemaBotDbContext context, AesEncryptionService aes) =>
            {
                var config = await context.ApiConfigurations
                    .Include(a => a.ContextPrompts)
                    .Include(a => a.AuthConfig)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (config?.AuthConfig != null)
                {
                    config.AuthConfig.EncryptedCredentials = aes.Decrypt(config.AuthConfig.EncryptedCredentials);
                }

                return config != null ? Results.Ok(config) : Results.NotFound();
            }).Produces<ApiConfiguration>();

        app.MapPost("/api/configurations",
            //[Authorize(Roles = "Editor,Admin")]
        async (ApiConfiguration config, SchemaBotDbContext context) =>
            {
                if (config.SchemaType == SchemaType.Swagger && !SwaggerValidator.IsValidSwagger3(config.SchemaJson))
                {
                    return Results.Problem("Invalid Swagger 3.0 specification");
                }
                config.UpdatedAt = DateTime.UtcNow;
                context.ApiConfigurations.Add(config);
                await context.SaveChangesAsync();
                return Results.Created($"/api/configurations/{config.Id}", config);
            }).Produces<ApiConfiguration>(201);

        app.MapPost("/api/context-prompts",
           // [Authorize(Roles = "Editor,Admin")]
        async (ContextPrompt prompt, SchemaBotDbContext context) =>
            {
                var config = await context.ApiConfigurations.FindAsync(prompt.ApiConfigurationId);
                if (config == null) return Results.NotFound("Configuration not found");

                context.ContextPrompts.Add(prompt);
                await context.SaveChangesAsync();
                return Results.Created($"/api/context-prompts/{prompt.Id}", prompt);
            }).Produces<ContextPrompt>(201);

        app.MapPost("/api/auth-configurations",
            //[Authorize(Roles = "Admin")]
        async (AuthConfiguration authConfig, SchemaBotDbContext context, AesEncryptionService aes) =>
            {
                authConfig.EncryptedCredentials = aes.Encrypt(authConfig.EncryptedCredentials);
                context.AuthConfigurations.Add(authConfig);
                await context.SaveChangesAsync();
                return Results.Created($"/api/auth-configurations/{authConfig.Id}", authConfig);
            }).Produces<AuthConfiguration>(201);

        // Add login endpoint for testing
        app.MapPost("/login", (LoginRequest request, IConfiguration config) =>
        {
            var user = TestUsers.Users.FirstOrDefault(u =>
                u.Username == request.Username && u.Password == request.Password);

            if (user == null) return Results.Unauthorized();

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Convert.FromBase64String(config["Jwt:Key"]!);

            // Ensure key is at least 256 bits
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
        });
    }
}