using Microsoft.AspNetCore.Mvc;

namespace SchemaBot.AiAgent.Controllers
{
    // AgentAiController.cs
    [ApiController]
    [Route("api/agent")]
    public class AgentAiController : ControllerBase
    {
        private readonly IAgentAiService _agentService;

        public AgentAiController(IAgentAiService agentService)
        {
            _agentService = agentService;
        }

        [HttpPost("process-query")]
        public async Task<ActionResult<ApiCommand>> ProcessQuery([FromBody] UserQueryEvent query)
        {
            try
            {
                var command = await _agentService.ProcessUserQueryAsync(query);
                return Ok(command);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

}
