// Program.cs
using System.ComponentModel.DataAnnotations;
namespace SchemaBot.SettingService.Core;
public class ContextPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(500)]
    public string Prompt { get; set; } = null!;

    public Guid ApiConfigurationId { get; set; }
    public ApiConfiguration? ApiConfiguration { get; set; }
}
