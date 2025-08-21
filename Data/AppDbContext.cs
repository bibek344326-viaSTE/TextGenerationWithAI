using Microsoft.EntityFrameworkCore;
using TextGenerationWithAI.Models;

namespace TextGenerationWithAI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<GeneratedText> GeneratedText { get; set; }
    }
}
