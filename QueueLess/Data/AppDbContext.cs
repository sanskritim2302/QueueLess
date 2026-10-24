using Microsoft.EntityFrameworkCore;
using QueueLess.Entities;
namespace QueueLess.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }
        public DbSet<Queue> Queues { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;

    }
}
