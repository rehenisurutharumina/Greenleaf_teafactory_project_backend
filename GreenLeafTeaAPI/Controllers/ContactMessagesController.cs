using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.DTOs;
using GreenLeafTeaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GreenLeafTeaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactMessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ContactMessagesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// POST /api/contactmessages — Public: anyone can send a message
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ContactMessageDto>> SendMessage([FromBody] CreateContactMessageDto dto)
        {
            var senderEmail = dto.SenderEmail.Trim().ToLowerInvariant();
            var body = dto.Message.Trim();

            if (string.IsNullOrWhiteSpace(senderEmail))
                ModelState.AddModelError(nameof(dto.SenderEmail), "Email address is required.");

            if (string.IsNullOrWhiteSpace(body))
                ModelState.AddModelError(nameof(dto.Message), "Message cannot be empty.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var message = new ContactMessage
            {
                SenderName = dto.SenderName?.Trim(),
                SenderEmail = senderEmail,
                Subject = dto.Subject?.Trim(),
                Message = body,
                IsRead = false,
                ReceivedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, ToDto(message));
        }

        /// <summary>
        /// GET /api/contactmessages — Admin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ContactMessageDto>>> GetAllMessages()
        {
            var messages = await _context.ContactMessages
                .AsNoTracking()
                .OrderByDescending(m => m.ReceivedAt)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Ok(messages);
        }

        /// <summary>
        /// GET /api/contactmessages/{id} — Admin only
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ContactMessageDto>> GetMessage(int id)
        {
            var message = await _context.ContactMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                return NotFound(new { message = $"Message #{id} not found." });

            return Ok(ToDto(message));
        }

        /// <summary>
        /// PUT /api/contactmessages/{id}/read — Admin only
        /// </summary>
        [HttpPut("{id:int}/read")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);

            if (message == null)
                return NotFound(new { message = $"Message #{id} not found." });

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        private static ContactMessageDto ToDto(ContactMessage message)
        {
            return new ContactMessageDto
            {
                Id = message.Id,
                SenderName = message.SenderName,
                SenderEmail = message.SenderEmail,
                Subject = message.Subject,
                Message = message.Message,
                IsRead = message.IsRead,
                ReceivedAt = message.ReceivedAt
            };
        }
    }
}
