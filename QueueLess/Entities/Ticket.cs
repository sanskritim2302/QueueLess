namespace QueueLess.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        public int QueueId { get; set; }

        public Queue Queue { get; set; } = null!;

        public int TicketNumber { get; set; }

        public required string CustomerName { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Waiting;

        public DateTime CreatedAt { get; set; }

        public DateTime? CalledAt { get; set; }

        public DateTime? ServedAt { get; set; }

        // SQL Server automatically updates this value whenever the row changes.
        public byte[] RowVersion { get; set; } = null!;
    }
}
