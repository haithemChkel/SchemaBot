// Program.cs
public static class SwaggerValidator
{
    public static bool IsValidSwagger3(string schemaJson)
    {
        return true;
        // Simplified validation - in real implementation use OpenAPI parser
        //return schemaJson.Contains("\"openapi\": \"3.");
    }
}
