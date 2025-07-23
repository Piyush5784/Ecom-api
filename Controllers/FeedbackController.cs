using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Utility;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_Admin)]
    public class FeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly ILogService logger;

        public FeedbackController(ApplicationDbContext db, ILogService logger)
        {
            this.db = db;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedback()
        {
            try
            {
                var feedbacks = await db.Feedback
                    .OrderByDescending(f => f.SubmittedAt)
                    .ToListAsync();

                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load feedback list", "Feedback", "GetAllFeedback", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Unable to display feedback.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackDetails(int id)
        {
            try
            {
                var feedback = await db.Feedback.FindAsync(id);
                if (feedback == null)
                {
                    await logger.LogAsync(SD.Log_Error, $"Feedback entry #{id} not found", "Feedback", "GetFeedbackDetails", null, Request.Path, User.Identity?.Name);
                    return NotFound($"Feedback entry #{id} not found.");
                }

                return Ok(feedback);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading feedback details", "Feedback", "GetFeedbackDetails", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load feedback details.");
            }
        }
    }
}
