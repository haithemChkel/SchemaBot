// Program.cs
using System.ComponentModel.DataAnnotations;

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
