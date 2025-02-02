// Program.cs
using Microsoft.AspNetCore.Builder;

public static partial class ConfigureSettingService
{
    public static void AddSettingService(this WebApplication app, string jwtKey)
    {
        app.UseCors("Any");
        app.UseSwagger();
        app.UseSwagger();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSettingService(jwtKey);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchemaBotDbContext>();
            db.Database.EnsureCreated();
        }
    }
}