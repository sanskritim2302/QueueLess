namespace QueueLess.DTOs
{
    public class CreateTicketRequest
    {
        public int QueueId { get; set; }
        public required string CustomerName { get; set; }
    }
}
