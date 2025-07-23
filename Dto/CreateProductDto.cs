using Microsoft.AspNetCore.Http;
using Ecom_api.Models;

namespace Ecom_api.Dto
{
    public class CreateProductDto
    {
        public Product Product { get; set; } = new();
        public IFormFile? Image { get; set; }
        public List<Category> Categories { get; set; } = new();
    }
}
