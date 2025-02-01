// Program.cs
public static partial class ConfigureSettingService
{
    public static void AddSettingService(this WebApplication app, string jwtKey)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
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