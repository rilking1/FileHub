using Microsoft.EntityFrameworkCore;

namespace FileHub.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileRecord> Files { get; set; }
    }
}
