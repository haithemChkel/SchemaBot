// Program.cs
using Microsoft.EntityFrameworkCore;
using SchemaBot.SettingService.Core;
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
