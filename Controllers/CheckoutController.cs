using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Models;
using Ecom_api.Models.ViewModels;
using Ecom_api.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;


namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = SD.Role_User)]
    public class CheckoutController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;
        private readonly ILogService _logger;

        public CheckoutController(IConfiguration config, ApplicationDbContext db, ILogService logger)
        {
            _config = config;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckoutDetails()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var cartItems = await _db.Cart
                .Include(c => c.Product)
                .Where(c => c.User.Id == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("Cart is empty.");

            var vm = new CheckoutViewModel
            {
                StreetAddress = user.StreetAddress,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode,
                CartItems = cartItems
            };

            return Ok(vm);
        }

        [HttpPost("session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var cartItems = await _db.Cart
                .Include(c => c.Product)
                .Where(c => c.User.Id == user.Id)
                .ToListAsync();

            model.CartItems = cartItems;

            if (!ModelState.IsValid)
                return BadRequest("Invalid address fields.");

            if (!cartItems.Any())
                return BadRequest("Cart is empty.");

            user.StreetAddress = model.StreetAddress;
            user.City = model.City;
            user.State = model.State;
            user.PostalCode = model.PostalCode;

            await _db.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                if (item.Quantity > item.Product.Quantity)
                {
                    await _logger.LogAsync(SD.Log_Warning, $"Stock issue: {item.Product.Title}", "Checkout", "CreateCheckoutSession", null, Request.Path, username);
                    return BadRequest($"Only {item.Product.Quantity} left for {item.Product.Title}.");
                }
            }

            var domain = $"{Request.Scheme}://{Request.Host}";
            var lineItems = cartItems.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Product.Price * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Title
                    }
                },
                Quantity = item.Quantity
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/api/Checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/api/Checkout/cancel?session_id={{CHECKOUT_SESSION_ID}}"
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Ok(new { sessionUrl = session.Url });
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return BadRequest();

            var stripeSession = new SessionService().Get(session_id);
            if (stripeSession.PaymentStatus != "paid")
            {
                await _logger.LogAsync(SD.Log_Error, $"Failed payment: {session_id}", "Checkout", "Success", null, Request.Path, User.Identity?.Name);
                return BadRequest("Payment not completed.");
            }

            var username = User.Identity?.Name;
            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var cartItems = await _db.Cart
                .Include(c => c.Product)
                .Where(c => c.User.Id == user.Id)
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest("Cart is empty.");

            foreach (var item in cartItems)
            {
                var order = new Order
                {
                    UserId = user.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.Price,
                    Status = SD.OrderStatusConfirmed,
                    paymentStatus = SD.Payment_Status_Completed,
                    PaymentSessionId = session_id,
                    OrderDate = DateTime.UtcNow
                };
                _db.Order.Add(order);
                item.Product.Quantity -= item.Quantity;
            }

            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            await _logger.LogAsync(SD.Log_Success, $"Order confirmed for {username}", "Checkout", "Success", null, Request.Path, username);
            return Ok("Payment successful! Order confirmed.");
        }

        [HttpGet("cancel")]
        public IActionResult Cancel(string session_id)
        {
            _logger.LogAsync(SD.Log_Info, $"Payment cancelled: {session_id}", "Checkout", "Cancel", null, Request.Path, User.Identity?.Name);
            return Ok("Payment was cancelled.");
        }
    }
}
