// Program.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace SchemaBot.SettingService.Core;

public class AuthConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string EncryptedCredentials { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthType AuthType { get; set; }

    public Guid ApiConfigurationId { get; set; }
    public ApiConfiguration? ApiConfiguration { get; set; }
}
