using System.ComponentModel.DataAnnotations;

namespace TextGenerationWithAI.Models
{
    public class GeneratedText
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Prompt { get; set; } = string.Empty;

        [Required]
        public string Response { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
