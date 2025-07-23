using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Models;
using Ecom_api.Models.ViewModels;
using Ecom_api.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly IEmailSenderApplicationInterface emailSender;
        private readonly ILogService logger;

        public HomeController(ApplicationDbContext db, IEmailSenderApplicationInterface emailSender, ILogService logger)
        {
            this.db = db;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await db.Product.ToListAsync();

                var latestProducts = await db.LatestProduct
                    .Include(lp => lp.Product)
                    .OrderBy(lp => lp.DisplayOrder)
                    .Select(lp => lp.Product)
                    .ToListAsync();

                var viewModel = new ProductViewModel
                {
                    AllProducts = products,
                    LatestProducts = latestProducts
                };

                return Ok(viewModel);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error loading homepage products", "Home", "GetProducts", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to load products.");
            }
        }


        [HttpPost("feedback")]
        [Authorize(Roles = SD.Role_User)]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                db.Feedback.Add(feedback);
                await db.SaveChangesAsync();

                return Ok("Thank you for your feedback!");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to submit feedback", "Home", "SubmitFeedback", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Failed to submit feedback.");
            }
        }

        [HttpPost("contact")]
        public async Task<IActionResult> SubmitContact([FromBody] Contact c)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                db.Contact.Add(c);
                await db.SaveChangesAsync();
                await emailSender.SendContactMessageToAdminAsync(c);

                return Ok("Message successfully sent!");
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to send contact message", "Home", "SubmitContact", ex.ToString(), Request.Path, User.Identity?.Name);
                return StatusCode(500, "Something went wrong while sending your message.");
            }
        }

        [HttpGet("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult GetError()
        {
            var errorId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return Ok(new
            {
                Message = "An unexpected error occurred.",
                RequestId = errorId
            });
        }
    }
}
