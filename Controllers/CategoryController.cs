using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Models;
using Ecom_api.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly ILogService logger;

        public CategoryController(ApplicationDbContext db, ILogService logger)
        {
            this.db = db;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var categories = await db.Category.ToListAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load category list", "Category", "GetAll", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load categories.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await db.Category.AddAsync(category);
                await db.SaveChangesAsync();

                await logger.LogAsync(SD.Log_Success, $"Created category '{category.Name}'", "Category", "Create", null, Request.Path, User.Identity?.Name);
                return Ok("Category successfully created.");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error while creating category", "Category", "Create", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error while creating category.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var category = await db.Category.FindAsync(id);
                if (category == null) return NotFound();

                return Ok(category);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading category", "Category", "GetById", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error while loading category.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] Category category)
        {
            if (id != category.Id)
                return BadRequest("Category ID mismatch.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                db.Category.Update(category);
                await db.SaveChangesAsync();

                await logger.LogAsync(SD.Log_Success, $"Updated category '{category.Name}'", "Category", "Edit", null, Request.Path, User.Identity?.Name);
                return Ok("Category successfully updated.");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error updating category", "Category", "Edit", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error while updating category.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await db.Category.FindAsync(id);
                if (category == null) return NotFound();

                db.Category.Remove(category);
                await db.SaveChangesAsync();

                await logger.LogAsync(SD.Log_Success, $"Deleted category '{category.Name}'", "Category", "Delete", null, Request.Path, User.Identity?.Name);
                return Ok("Category successfully deleted.");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error deleting category", "Category", "Delete", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error while deleting category.");
            }
        }
    }
}
