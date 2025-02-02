namespace SchemaBot.AiAgent.Controllers
{
    // Models
    public record UserQueryEvent(
        string Text,
        Guid ApiConfigId,
        string? SessionId = null);

}
