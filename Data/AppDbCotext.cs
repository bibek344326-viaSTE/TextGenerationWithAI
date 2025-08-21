using Microsoft.EntityFrameworkCore;
using TextGenerationWithAI.Models;

namespace TextGenerationWithAI.Data
{
    public class AppDbCotext : DbContext
    {
        public AppDbCotext(DbContextOptions<AppDbCotext> options) : base(options)
        {
        }

        public DbSet<GeneratedText> GeneratedText { get; set; }
    }
}
