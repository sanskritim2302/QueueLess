using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLess.Data;
using QueueLess.Entities;

namespace QueueLess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueuesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QueuesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/queues
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Queue>>> GetQueues()
        {
            return await _context.Queues.ToListAsync();
        }

        // GET: api/queues/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Queue>> GetQueueById(int id)
        {
            var queue = await _context.Queues.FindAsync(id);

            if (queue == null)
                return NotFound();

            return queue;
        }

        // POST: api/queues
        [HttpPost]
        public async Task<ActionResult<Queue>> CreateQueue(Queue queue)
        {
            if (string.IsNullOrWhiteSpace(queue.Name))
                return BadRequest("Queue name is required.");

            queue.IsActive = true;
            queue.CreatedAt = DateTime.UtcNow;

            await _context.Queues.AddAsync(queue); // Using async alternative for I/O efficiency
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetQueueById),
                new { id = queue.Id },
                queue);
        }

        // PUT: api/queues/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateQueue(int id, Queue updatedQueue)
        {
            if (string.IsNullOrWhiteSpace(updatedQueue.Name))
                return BadRequest("Queue name is required.");

            var queue = await _context.Queues.FindAsync(id);

            if (queue == null)
                return NotFound();

            queue.Name = updatedQueue.Name;
            queue.IsActive = updatedQueue.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QueueExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/queues/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteQueue(int id)
        {
            // Modern EF Core approach executes a direct delete statement in one DB roundtrip
            var deletedRows = await _context.Queues
                .Where(q => q.Id == id)
                .ExecuteDeleteAsync();

            if (deletedRows == 0)
                return NotFound();

            return NoContent();
        }

        private bool QueueExists(int id)
        {
            return _context.Queues.Any(e => e.Id == id);
        }
    }
}