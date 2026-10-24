namespace QueueLess.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public int QueueId { get; set; } //Guid - unique identifier for the queue
        public Queue Queue { get; set; } = null!;
        public int TicketNumber { get; set; }
        public string? CustomerName { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Waiting;
        public DateTime CreatedAt { get; set; }

        public DateTime? CalledAt { get; set; }

        public DateTime? ServedAt { get; set; }
    }
}
