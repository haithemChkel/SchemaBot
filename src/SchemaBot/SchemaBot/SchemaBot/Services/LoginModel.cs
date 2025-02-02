namespace SchemaBot.Client.Services
{
    using System.ComponentModel.DataAnnotations;

    // Services/AuthenticationService.cs
    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
