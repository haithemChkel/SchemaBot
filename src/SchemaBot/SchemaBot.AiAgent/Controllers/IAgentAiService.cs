
namespace SchemaBot.AiAgent.Controllers
{
    public interface IAgentAiService
    {
        Task<ApiCommand> ProcessUserQueryAsync(UserQueryEvent query);
    }
}