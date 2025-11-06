using Microsoft.AspNetCore.Mvc;
using EmployeeApi.Models;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity.Data;
using Azure.Messaging;
using Microsoft.EntityFrameworkCore;


namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginApiController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        public LoginApiController(EmployeeDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User request)
        {
            if(string.IsNullOrEmpty(request.Username)|| string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new {message = "Username and password is required."});
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(new
            {
                message = "Login successful.",
                username = user.Username,
                role = user.Role
            });
        }
    }
}
