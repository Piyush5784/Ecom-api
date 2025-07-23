using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Utility;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_Admin)]
    public class LogController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly ILogService logger;

        public LogController(ApplicationDbContext db, ILogService logger)
        {
            this.db = db;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            try
            {
                var logs = await db.Logs
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading log list", "Log", "GetLogs", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load logs.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLogDetails(int id)
        {
            try
            {
                var log = await db.Logs.FindAsync(id);
                if (log == null)
                {
                    await logger.LogAsync(SD.Log_Error, $"Log entry #{id} not found", "Log", "GetLogDetails", null, Request.Path, User.Identity?.Name);
                    return NotFound($"Log entry #{id} not found.");
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading log details", "Log", "GetLogDetails", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load log details.");
            }
        }
    }
}
