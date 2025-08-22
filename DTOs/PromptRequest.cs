namespace TextGenerationWithAI.DTOs
{
    public class PromptRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
    }
}
