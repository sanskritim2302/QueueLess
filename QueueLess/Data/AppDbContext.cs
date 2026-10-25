using Microsoft.EntityFrameworkCore;
using QueueLess.Entities;

namespace QueueLess.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Queue> Queues { get; set; } = null!;

        public DbSet<Ticket> Tickets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.RowVersion)
                .IsRowVersion();

            // A ticket number must be unique within its queue.
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => new { t.QueueId, t.TicketNumber })
                .IsUnique();

            // Only one ticket may be in the Called state per queue.
            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.QueueId)
                .IsUnique()
                .HasFilter($"[Status] = {(int)TicketStatus.Called}");

            // Preserve ticket history: don't cascade-delete tickets
            // when someone attempts to delete their queue.
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Queue)
                .WithMany()
                .HasForeignKey(t => t.QueueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
