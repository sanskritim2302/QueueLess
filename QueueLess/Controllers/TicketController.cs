using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLess.Data;
using QueueLess.DTOs;
using QueueLess.Entities;

namespace QueueLess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            var tickets = await _context.Tickets
                .AsNoTracking()
                .OrderBy(t => t.QueueId)
                .ThenBy(t => t.TicketNumber)
                .ToListAsync();

            return Ok(tickets);
        }

        // GET: api/tickets/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Ticket>> GetTicketById(int id)
        {
            var ticket = await _context.Tickets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound("Ticket not found.");

            return Ok(ticket);
        }

        // GET: api/tickets/queue/1
        [HttpGet("queue/{queueId:int}")]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTicketsByQueue(
            int queueId)
        {
            var queueExists = await _context.Queues
                .AnyAsync(q => q.Id == queueId);

            if (!queueExists)
                return NotFound("Queue not found.");

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.QueueId == queueId)
                .OrderBy(t => t.TicketNumber)
                .ToListAsync();

            return Ok(tickets);
        }

        // POST: api/tickets
        // Public endpoint: customers can join a queue.
        [HttpPost]
        public async Task<ActionResult<Ticket>> CreateTicket(
            CreateTicketRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest("Customer name is required.");

            if (request.QueueId <= 0)
                return BadRequest("A valid queue ID is required.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var queue = await _context.Queues
                    .FirstOrDefaultAsync(q => q.Id == request.QueueId);

                if (queue == null)
                    return NotFound("Queue not found.");

                if (!queue.IsActive)
                    return BadRequest("This queue is not active.");

                var lastTicketNumber = await _context.Tickets
                    .Where(t => t.QueueId == request.QueueId)
                    .Select(t => (int?)t.TicketNumber)
                    .MaxAsync() ?? 0;

                var ticket = new Ticket
                {
                    QueueId = request.QueueId,
                    CustomerName = request.CustomerName.Trim(),
                    TicketNumber = lastTicketNumber + 1,
                    Status = TicketStatus.Waiting,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tickets.Add(ticket);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(GetTicketById),
                    new { id = ticket.Id },
                    ticket);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();

                return Conflict(
                    "The ticket data changed concurrently. Please try again.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return Conflict(
                    "A database conflict occurred while creating the ticket. Please try again.");
            }
        }

        // POST: api/tickets/queue/1/call-next
        // Staff only.
        [Authorize(Roles = "Staff")]
        [HttpPost("queue/{queueId:int}/call-next")]
        public async Task<ActionResult<Ticket>> CallNextTicket(int queueId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var queue = await _context.Queues
                    .FirstOrDefaultAsync(q => q.Id == queueId);

                if (queue == null)
                    return NotFound("Queue not found.");

                if (!queue.IsActive)
                    return BadRequest("This queue is not active.");

                var alreadyCalled = await _context.Tickets
                    .AnyAsync(t =>
                        t.QueueId == queueId &&
                        t.Status == TicketStatus.Called);

                if (alreadyCalled)
                {
                    return Conflict(
                        "A customer has already been called. Serve or skip them first.");
                }

                var ticket = await _context.Tickets
                    .Where(t =>
                        t.QueueId == queueId &&
                        t.Status == TicketStatus.Waiting)
                    .OrderBy(t => t.TicketNumber)
                    .FirstOrDefaultAsync();

                if (ticket == null)
                    return NotFound("No waiting tickets in this queue.");

                ticket.Status = TicketStatus.Called;
                ticket.CalledAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ticket);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();

                return Conflict(
                    "The ticket changed concurrently. Refresh and try again.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                return Conflict(
                    "A conflicting database update occurred. Refresh and try again.");
            }
        }

        // PUT: api/tickets/1/serve
        // Staff only.
        [Authorize(Roles = "Staff")]
        [HttpPut("{id:int}/serve")]
        public async Task<IActionResult> ServeTicket(int id)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound("Ticket not found.");

            if (ticket.Status != TicketStatus.Called)
            {
                return BadRequest(
                    "Only a called ticket can be marked as served.");
            }

            ticket.Status = TicketStatus.Served;
            ticket.ServedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(
                    "The ticket was changed by another request. Refresh and try again.");
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "A database conflict occurred while serving the ticket.");
            }
        }

        // PUT: api/tickets/1/skip
        // Staff only.
        [Authorize(Roles = "Staff")]
        [HttpPut("{id:int}/skip")]
        public async Task<IActionResult> SkipTicket(int id)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound("Ticket not found.");

            if (ticket.Status != TicketStatus.Called)
            {
                return BadRequest(
                    "Only a called ticket can be skipped.");
            }

            ticket.Status = TicketStatus.Skipped;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(
                    "The ticket was changed by another request. Refresh and try again.");
            }
            catch (DbUpdateException)
            {
                return Conflict(
                    "A database conflict occurred while skipping the ticket.");
            }
        }
    }
}
