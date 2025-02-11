﻿// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SchemaBot.SettingService;

public static partial class ConfigureSettingService
{
    public static void AddSettingService(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Any",
                policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader();
                });
        });
        builder.Services.AddDbContext<SchemaBotDbContext>(options => options.UseSqlite("Data Source=./Data/schemas.db"));

        builder.Services.AddEndpointsApiExplorer();
        // Program.cs
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchemaBot API", Version = "v1" });

            // Add JWT Auth to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            // Make security requirement global
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
        });

        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<AesEncryptionService>();
    }
}