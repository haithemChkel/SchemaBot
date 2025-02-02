using System.Text.Json.Serialization;

namespace SchemaBot.AiAgent.Controllers
{
    public record ApiCommand(
        string Endpoint,
        string Method,
        Dictionary<string, string>? Parameters = null,
        object? Body = null)
    {
        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrEmpty(Endpoint) &&
            !string.IsNullOrEmpty(Method) &&
            HttpMethods.All.Contains(Method.ToUpper());
    }

}
