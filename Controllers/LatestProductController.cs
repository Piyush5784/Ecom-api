using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecom_api.Data;
using Ecom_api.Models;
using Ecom_api.Models.ViewModels;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LatestProductController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public LatestProductController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await db.LatestProduct
                .Include(lp => lp.Product)
                .OrderBy(lp => lp.DisplayOrder)
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("create")]
        public async Task<IActionResult> GetCreateModel()
        {
            var addModel = new AddLatestProductViewModel
            {
                Products = await db.Product.ToListAsync(),
                NewProduct = new LatestProduct()
            };

            return Ok(addModel);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] AddLatestProductViewModel data)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await db.Product.FindAsync(data.NewProduct.ProductId);
            if (product == null)
                return BadRequest("Invalid product selected.");

            data.NewProduct.Product = product;

            await db.LatestProduct.AddAsync(data.NewProduct);
            await db.SaveChangesAsync();

            return Ok("Latest product added successfully.");
        }

        [HttpGet("update/{id}")]
        public async Task<IActionResult> GetUpdateModel(int id)
        {
            var product = await db.LatestProduct.FindAsync(id);
            if (product == null) return NotFound();

            var addModel = new AddLatestProductViewModel
            {
                Products = await db.Product.ToListAsync(),
                NewProduct = product
            };

            return Ok(addModel);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] AddLatestProductViewModel data)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var latestProductFromDb = await db.LatestProduct.FindAsync(data.NewProduct.Id);
            if (latestProductFromDb == null)
                return NotFound();

            var selectedProduct = await db.Product.FindAsync(data.NewProduct.ProductId);
            if (selectedProduct == null)
                return BadRequest("Invalid product selected.");

            latestProductFromDb.ProductId = data.NewProduct.ProductId;
            latestProductFromDb.DisplayOrder = data.NewProduct.DisplayOrder;

            await db.SaveChangesAsync();

            return Ok("Latest product updated successfully.");
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> GetDeleteModel(int id)
        {
            var product = await db.LatestProduct.FindAsync(id);
            if (product == null)
                return NotFound();

            var addModel = new AddLatestProductViewModel
            {
                Products = await db.Product.ToListAsync(),
                NewProduct = product
            };

            return Ok(addModel);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await db.LatestProduct.FindAsync(id);
            if (product == null)
                return NotFound();

            db.LatestProduct.Remove(product);
            await db.SaveChangesAsync();

            return Ok("Latest product deleted successfully.");
        }
    }
}
