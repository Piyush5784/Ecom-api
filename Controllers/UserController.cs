using Ecom_api.Data;
using Ecom_api.Dto;
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
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public UserController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            try
            {
                var users = await _context.User.ToListAsync();
                var userDtos = users.Select(user => new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
                }).ToList();

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch users", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            try
            {
                var user = await _context.User.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch user", error = ex.Message });
            }
        }
    }
}