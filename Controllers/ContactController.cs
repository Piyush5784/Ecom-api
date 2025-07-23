using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Ecom_api.Utility;
using Ecom_api.Data;
using Ecom_api.Interfaces;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ContactController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly ILogService logger;

        public ContactController(ApplicationDbContext db, ILogService logger)
        {
            this.db = db;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var messages = await db.Contact
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load contact messages", "Contact", "GetMessages", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Unable to display contact messages.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessageDetails(int id)
        {
            try
            {
                var contact = await db.Contact.FindAsync(id);
                if (contact == null)
                {
                    await logger.LogAsync(SD.Log_Error, $"Contact entry #{id} not found", "Contact", "GetMessageDetails", null, Request.Path, User.Identity?.Name);
                    return NotFound($"Contact entry #{id} not found.");
                }

                return Ok(contact);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading contact details", "Contact", "GetMessageDetails", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load contact details.");
            }
        }
    }
}
