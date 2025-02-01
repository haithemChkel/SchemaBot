// Program.cs
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

#region Configuration
// Switch to SQLite
builder.Services.AddDbContext<SchemaBotDbContext>(options =>
    options.UseSqlite("Data Source=schemas.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchemaBot Settings API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AesEncryptionService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Database Entities
public class ApiConfiguration { /* unchanged from previous */ }
public enum SchemaType { Swagger, GraphQL }
public class ContextPrompt { /* unchanged from previous */ }
public class AuthConfiguration { /* unchanged from previous */ }
public enum AuthType { ApiKey, OAuth2, JWT }
#endregion

#region Database Context
public class SchemaBotDbContext : DbContext { /* unchanged from previous */ }
#endregion

#region Services
public class AesEncryptionService { /* unchanged from previous */ }

public static class SwaggerValidator {
    public static bool IsValidSwagger3(string schemaJson) {
        return schemaJson.Contains("\"openapi\": \"3.");
    }
}
#endregion

#region Endpoints
// Added Update endpoint
app.MapPut("/api/configurations/{id}", 
    [Authorize(Roles = "Editor,Admin")] 
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
app.MapGet("/api/configurations/{id}", 
    [Authorize(Roles = "Viewer,Editor,Admin")] 
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
    [Authorize(Roles = "Editor,Admin")] 
    async (ApiConfiguration config, SchemaBotDbContext context) =>
{
    if (config.SchemaType == SchemaType.Swagger && !SwaggerValidator.IsValidSwagger3(config.SchemaJson))
    {
        return Results.Problem("Invalid Swagger 3.0 specification");
    }

    context.ApiConfigurations.Add(config);
    await context.SaveChangesAsync();
    return Results.Created($"/api/configurations/{config.Id}", config);
}).Produces<ApiConfiguration>(201);

app.MapPost("/api/context-prompts",
    [Authorize(Roles = "Editor,Admin")] 
    async (ContextPrompt prompt, SchemaBotDbContext context) =>
{
    var config = await context.ApiConfigurations.FindAsync(prompt.ApiConfigurationId);
    if (config == null) return Results.NotFound("Configuration not found");

    context.ContextPrompts.Add(prompt);
    await context.SaveChangesAsync();
    return Results.Created($"/api/context-prompts/{prompt.Id}", prompt);
}).Produces<ContextPrompt>(201);

app.MapPost("/api/auth-configurations",
    [Authorize(Roles = "Admin")] 
    async (AuthConfiguration authConfig, SchemaBotDbContext context, AesEncryptionService aes) =>
{
    authConfig.EncryptedCredentials = aes.Encrypt(authConfig.EncryptedCredentials);
    context.AuthConfigurations.Add(authConfig);
    await context.SaveChangesAsync();
    return Results.Created($"/api/auth-configurations/{authConfig.Id}", authConfig);
}).Produces<AuthConfiguration>(201);

// Add login endpoint for testing
app.MapPost("/login", (LoginRequest request) => {
    // For testing purposes only - replace with real authentication
    var user = TestUsers.Users.FirstOrDefault(u => 
        u.Username == request.Username && u.Password == request.Password);
    
    if (user == null) return Results.Unauthorized();
    
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Convert.FromBase64String(builder.Configuration["Jwt:Key"]!);
    
    var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature)
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
});

public record LoginRequest(string Username, string Password);

public static class TestUsers {
    public static List<TestUser> Users = new() {
        new TestUser("admin", "admin123", "Admin"),
        new TestUser("editor", "editor123", "Editor"),
        new TestUser("viewer", "viewer123", "Viewer")
    };
}

public record TestUser(string Username, string Password, string Role);
#endregion

#region Error Handling Middleware
/* unchanged from previous */
#endregion

// Database initialization
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<SchemaBotDbContext>();
    db.Database.EnsureCreated();
}

app.Run();