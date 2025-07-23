
using Ecom_api.Utility;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ecom_api.Models
{
    public class User
    {

        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = SD.Role_User;

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }


        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }


        [JsonIgnore]
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        [JsonIgnore]
        public ICollection<Order> OrderItems { get; set; } = new List<Order>();
    }
}
