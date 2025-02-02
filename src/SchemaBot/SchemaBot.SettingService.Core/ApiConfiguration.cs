// Program.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace SchemaBot.SettingService.Core;
public class ApiConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public string SchemaJson { get; set; } = null!;


    [Required]
    public string ApiUrl { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SchemaType SchemaType { get; set; }

    public List<ContextPrompt> ContextPrompts { get; set; } = new();
    public AuthConfiguration? AuthConfig { get; set; }
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
