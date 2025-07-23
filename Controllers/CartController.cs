using Ecom_api.Data;
using Ecom_api.Dto;
using Ecom_api.Interfaces;
using Ecom_api.Models;
using Ecom_api.Services;
using Ecom_api.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_User)]
    public class CartController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly ApplicationDbContext db;
        private readonly ILogService logEntry;

        public CartController(IAuthService authService, ApplicationDbContext db, ILogService logEntry)
        {
            this.authService = authService;
            this.db = db;
            this.logEntry = logEntry;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var user = await authService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                var cartItems = await db.Cart
                    .Include(c => c.Product)
                    .Include(c => c.User)
                    .Where(c => c.UserId == user.Id)
                    .Select(c => new CartDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        Username = c.User != null ? c.User.Username : null,
                        ProductId = c.ProductId,
                        ProductName = c.Product != null ? c.Product.Title : null,
                        ProductDescription = c.Product != null ? c.Product.Description : null,
                        ProductPrice = c.Product != null ? c.Product.Price : 0,
                        ProductImageUrl = c.Product != null ? c.Product.ImageUrl : null,
                        Quantity = c.Quantity
                    })
                    .ToListAsync();

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                await logEntry.LogAsync(SD.Log_Error, "Failed to load cart", "Cart", "GetCart", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load your cart.");
            }
        }

        [HttpPost("add/{id}")]
        public async Task<IActionResult> AddToCart(int id)
        {
            try
            {
                var user = await authService.GetCurrentUserAsync();
                if (user == null) return Unauthorized();

                var product = await db.Product.FindAsync(id);
                if (product == null) return NotFound();

                var existingCartItem = await db.Cart
                    .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += 1;
                    await db.SaveChangesAsync();
                    return Ok("Item quantity updated.");
                }

                var cartItem = new Cart
                {
                    ProductId = product.Id,
                    UserId = user.Id,
                    Quantity = 1
                };

                await db.Cart.AddAsync(cartItem);
                await db.SaveChangesAsync();

                return Ok("Item added to cart.");
            }
            catch (Exception ex)
            {
                await logEntry.LogAsync(SD.Log_Error, "Error adding item to cart", "Cart", "AddToCart", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error adding item to cart.");
            }
        }

        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                var user = await authService.GetCurrentUserAsync();
                if (user == null) return Unauthorized();

                var cartItem = await db.Cart
                    .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);

                if (cartItem == null) return NotFound();

                db.Cart.Remove(cartItem);
                await db.SaveChangesAsync();

                return Ok("Item removed from cart.");
            }
            catch (Exception ex)
            {
                await logEntry.LogAsync(SD.Log_Error, "Error removing item from cart", "Cart", "RemoveFromCart", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error removing item from cart.");
            }
        }

        [HttpPost("increase/{id}")]
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            try
            {
                var user = await authService.GetCurrentUserAsync();
                if (user == null) return Unauthorized();

                var cartItem = await db.Cart
                    .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);

                if (cartItem == null) return NotFound();

                cartItem.Quantity += 1;
                await db.SaveChangesAsync();

                return Ok("Item quantity increased.");
            }
            catch (Exception ex)
            {
                await logEntry.LogAsync(SD.Log_Error, "Error increasing item quantity", "Cart", "IncreaseQuantity", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error increasing item quantity.");
            }
        }

        [HttpPost("decrease/{id}")]
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            try
            {
                var user = await authService.GetCurrentUserAsync();
                if (user == null) return Unauthorized();

                var cartItem = await db.Cart
                    .FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);

                if (cartItem == null) return NotFound();

                cartItem.Quantity -= 1;
                if (cartItem.Quantity <= 0)
                    db.Cart.Remove(cartItem);

                await db.SaveChangesAsync();
                return Ok("Item quantity decreased.");
            }
            catch (Exception ex)
            {
                await logEntry.LogAsync(SD.Log_Error, "Error decreasing item quantity", "Cart", "DecreaseQuantity", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Error decreasing item quantity.");
            }
        }
    }
}