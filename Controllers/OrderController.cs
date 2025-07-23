using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Utility;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ILogService logger;

        public OrderController(ApplicationDbContext db, UserManager<IdentityUser> userManager, ILogService logger)
        {
            this.db = db;
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserOrders()
        {
            try
            {
                var user = await userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                if (!Guid.TryParse(user.Id, out Guid userId))
                    return BadRequest("Invalid user ID format.");

                var orders = await db.Order
                    .Include(o => o.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load user orders", "Order", "GetUserOrders", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load your orders.");
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await db.Order
                    .Include(o => o.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load order management", "Order", "GetAllOrders", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load order management.");
            }
        }

        [HttpPost("ship/{id}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> MarkAsShipped(int id)
        {
            try
            {
                var order = await db.Order.FindAsync(id);
                if (order == null) return NotFound();

                order.Status = SD.OrderStatusShipped;
                await db.SaveChangesAsync();

                return Ok($"Order #{order.Id} marked as shipped.");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, $"Failed to mark order #{id} as shipped", "Order", "MarkAsShipped", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error marking order as shipped.");
            }
        }

        [HttpPost("cancel/{id}")]
        [Authorize(Roles = SD.Role_User)]
        public async Task<IActionResult> CancelOrderByUser(int id)
        {
            try
            {
                var order = await db.Order
                    .Include(o => o.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                if (order.Status == SD.OrderStatusCancelled)
                    return BadRequest($"Order #{order.Id} is already cancelled.");

                if (order.Product != null)
                    order.Product.Quantity += order.Quantity;

                order.Status = SD.OrderStatusCancelled;
                await db.SaveChangesAsync();

                return Ok($"Order #{order.Id} has been cancelled.");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, $"Failed to cancel order #{id}", "Order", "CancelOrderByUser", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error cancelling the order.");
            }
        }
    }
}
