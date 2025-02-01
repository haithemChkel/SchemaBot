// Program.cs
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

#region Configuration
builder.Services.AddDbContext<SchemaBotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchemaBot Settings API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
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
public class ApiConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, StringLength(100)]
    public string Name { get; set; } = null!;
    
    [Required]
    public string SchemaJson { get; set; } = null!;
    
    [Required]
    public SchemaType SchemaType { get; set; }
    
    public List<ContextPrompt> ContextPrompts { get; set; } = new();
    public AuthConfiguration? AuthConfig { get; set; }
}

public enum SchemaType { Swagger, GraphQL }

public class ContextPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, StringLength(500)]
    public string Prompt { get; set; } = null!;
    
    public Guid ApiConfigurationId { get; set; }
    public ApiConfiguration? ApiConfiguration { get; set; }
}

public class AuthConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string EncryptedCredentials { get; set; } = null!;
    
    [Required]
    public AuthType AuthType { get; set; }
    
    public Guid ApiConfigurationId { get; set; }
    public ApiConfiguration? ApiConfiguration { get; set; }
}

public enum AuthType { ApiKey, OAuth2, JWT }
#endregion

#region Database Context
public class SchemaBotDbContext : DbContext
{
    public SchemaBotDbContext(DbContextOptions<SchemaBotDbContext> options) : base(options) { }

    public DbSet<ApiConfiguration> ApiConfigurations => Set<ApiConfiguration>();
    public DbSet<ContextPrompt> ContextPrompts => Set<ContextPrompt>();
    public DbSet<AuthConfiguration> AuthConfigurations => Set<AuthConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiConfiguration>()
            .HasMany(a => a.ContextPrompts)
            .WithOne(c => c.ApiConfiguration)
            .HasForeignKey(c => c.ApiConfigurationId);

        modelBuilder.Entity<ApiConfiguration>()
            .HasOne(a => a.AuthConfig)
            .WithOne(a => a.ApiConfiguration)
            .HasForeignKey<AuthConfiguration>(a => a.ApiConfigurationId);
    }
}
#endregion

#region Services
public class AesEncryptionService
{
    private readonly byte[] _key = Convert.FromBase64String("BASE64_ENCRYPTION_KEY"); // Replace with secure key
    private readonly byte[] _iv = new byte[16]; // Should be generated and stored securely

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}

public static class SwaggerValidator
{
    public static bool IsValidSwagger3(string schemaJson)
    {
        // Simplified validation - in real implementation use OpenAPI parser
        return schemaJson.Contains("\"openapi\": \"3.");
    }
}
#endregion

#region Endpoints
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

#endregion

#region RBAC Extensions
public static class RoleRequirementHandler
{
    public static void RequireRole(this RouteHandlerBuilder builder, params string[] roles)
    {
        builder.RequireAuthorization(policy => 
            policy.RequireRole(roles)
                  .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    }
}
#endregion

#region Error Handling Middleware
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            Status = 500,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred"
        });
    });
});
#endregion

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchemaBotDbContext>();
    db.Database.Migrate();
}

app.Run();