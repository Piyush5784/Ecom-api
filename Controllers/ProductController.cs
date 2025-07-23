

using Ecom_api.Data;
using Ecom_api.Dto;
using Ecom_api.Interfaces;
using Ecom_api.Models;
using Ecom_api.Services;
using Ecom_api.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecom_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly CloudinaryService cloud;
        private readonly ILogService logger;

        public ProductController(ApplicationDbContext db, CloudinaryService cloud, ILogService logger)
        {
            this.db = db;
            this.cloud = cloud;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Index(ProductFilterViewModel filter)
        {
            try
            {
                var productsQuery = db.Product.Include(p => p.Category).AsQueryable();

                if (!string.IsNullOrEmpty(filter.Search))
                    productsQuery = productsQuery.Where(p => p.Title.Contains(filter.Search));

                if (filter.MaxPrice.HasValue)
                    productsQuery = productsQuery.Where(p => p.Price <= filter.MaxPrice.Value);

                if (filter.MinRating.HasValue)
                    productsQuery = productsQuery.Where(p => p.Ratings >= filter.MinRating.Value);

                if (!string.IsNullOrEmpty(filter.Category))
                    productsQuery = productsQuery.Where(p => p.Category.Name == filter.Category);

                switch (filter.SortBy)
                {
                    case "price-low":
                        productsQuery = productsQuery.OrderBy(p => p.Price);
                        break;
                    case "price-high":
                        productsQuery = productsQuery.OrderByDescending(p => p.Price);
                        break;
                    case "name":
                        productsQuery = productsQuery.OrderBy(p => p.Title);
                        break;
                    default:
                        productsQuery = productsQuery.OrderBy(p => p.Id);
                        break;
                }

                int totalItems = productsQuery.Count();
                filter.TotalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);
                filter.Products = productsQuery.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToList();
                filter.Categories = db.Category.Select(c => c.Name).ToList();

                return Ok(filter);
            }
            catch (Exception ex)
            {
                logger.LogAsync(SD.Log_Error, "Failed to load products", "Product", "Index", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Failed to load products." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("list")]
        public async Task<IActionResult> List(ProductFilterViewModel filter)
        {
            try
            {
                var productsQuery = db.Product.Include(p => p.Category).AsQueryable();

                if (!string.IsNullOrEmpty(filter.Search))
                    productsQuery = productsQuery.Where(p => p.Title.Contains(filter.Search));

                if (filter.MaxPrice.HasValue)
                    productsQuery = productsQuery.Where(p => p.Price <= filter.MaxPrice.Value);

                if (filter.MinRating.HasValue)
                    productsQuery = productsQuery.Where(p => p.Ratings >= filter.MinRating.Value);

                if (!string.IsNullOrEmpty(filter.Category))
                    productsQuery = productsQuery.Where(p => p.Category.Name == filter.Category);

                switch (filter.SortBy)
                {
                    case "price-low": productsQuery = productsQuery.OrderBy(p => p.Price); break;
                    case "price-high": productsQuery = productsQuery.OrderByDescending(p => p.Price); break;
                    case "name": productsQuery = productsQuery.OrderBy(p => p.Title); break;
                    default: productsQuery = productsQuery.OrderBy(p => p.Id); break;
                }

                filter.Products = await productsQuery.ToListAsync();
                filter.Categories = await db.Category.Select(c => c.Name).ToListAsync();

                return Ok(filter);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Unable to load filtered product list", "Product", "List", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Unable to load filtered product list." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                var product = await db.Product.FindAsync(id);
                if (product is null) return NotFound(new { error = "Product not found." });
                return Ok(product);
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load product details", "Product", "Details", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Failed to load product details." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("create")]
        public IActionResult Create()
        {
            return Ok(new CreateProductDto { Categories = GetOrderedCategories() });
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto model)
        {
            if (!ModelState.IsValid || model.Image is null)
            {
                if (model.Image is null)
                    ModelState.AddModelError("Image", "Please upload an image.");

                model.Categories = GetOrderedCategories();
                return BadRequest(new { error = "Invalid model data", errors = ModelState });
            }

            try
            {
                var result = await cloud.UploadImageAsync(model.Image);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Image upload failed");
                    model.Categories = GetOrderedCategories();
                    return BadRequest(new { error = "Image upload failed", errors = ModelState });
                }

                var category = await db.Category.FindAsync(model.Product.CategoryId);
                model.Product.ImageUrl = result.Url;
                model.Product.Category = category;

                db.Product.Add(model.Product);
                await db.SaveChangesAsync();

                return Ok(new { message = "Product created successfully!", product = model.Product });
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error while creating product", "Product", "Create", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "An error occurred while creating the product." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id is null) return NotFound(new { error = "Product ID is required." });

                var product = await db.Product.FindAsync(id);
                if (product is null) return NotFound(new { error = "Product not found." });

                return Ok(new CreateProductDto
                {
                    Product = product,
                    Categories = GetOrderedCategories()
                });
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load edit form", "Product", "Edit", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Failed to load edit form." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(CreateProductDto model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = GetOrderedCategories();
                return BadRequest(new { error = "Invalid model data", errors = ModelState });
            }

            try
            {
                if (model.Image != null && model.Image.Length > 0)
                {
                    var result = await cloud.UploadImageAsync(model.Image);
                    if (!result.Success)
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Image upload failed");
                        model.Categories = GetOrderedCategories();
                        return BadRequest(new { error = "Image upload failed", errors = ModelState });
                    }

                    var category = await db.Category.FindAsync(model.Product.CategoryId);
                    model.Product.ImageUrl = result.Url;
                    model.Product.Category = category;
                }
                else
                {
                    var existing = await db.Product.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.Product.Id);
                    if (existing != null)
                    {
                        model.Product.ImageUrl = existing.ImageUrl;
                    }
                }

                db.Product.Update(model.Product);
                await db.SaveChangesAsync();

                return Ok(new { message = "Product successfully updated!", product = model.Product });
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to update product", "Product", "Edit", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Failed to update product." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id is null) return NotFound(new { error = "Product ID is required." });

                var product = await db.Product.FindAsync(id);
                if (product is null) return NotFound(new { error = "Product not found." });

                return Ok(new CreateProductDto
                {
                    Product = product,
                    Categories = GetOrderedCategories()
                });
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Failed to load delete form", "Product", "Delete", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Failed to load delete confirmation." });
            }
        }

        [Authorize(Roles = SD.Role_Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            try
            {
                if (id is null) return NotFound(new { error = "Product ID is required." });

                var product = await db.Product.FindAsync(id);
                if (product is null) return NotFound(new { error = "Product not found." });

                db.Product.Remove(product);
                await db.SaveChangesAsync();

                return Ok(new { message = "Product successfully deleted!" });
            }
            catch (Exception ex)
            {
                await logger.LogAsync(SD.Log_Error, "Error while deleting product", "Product", "DeletePOST", ex.ToString(), Request.Path, User.Identity?.Name);
                return BadRequest(new { error = "Error while deleting product." });
            }
        }

        private List<Category> GetOrderedCategories()
        {
            try
            {
                return db.Category.OrderBy(c => c.DisplayOrder).ToList();
            }
            catch (Exception ex)
            {
                logger.LogAsync(SD.Log_Error, "Error loading categories", "Product", "GetOrderedCategories", ex.ToString(), Request.Path, User.Identity?.Name);
                return new List<Category>();
            }
        }
    }
}