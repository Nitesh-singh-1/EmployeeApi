using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        public AdminApiController(EmployeeDbContext context)
        {
            _context = context;
        }
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] int adminId, [FromQuery] string role = "All")
        {
            
            var query = _context.Users.Where(u => u.CreatedBy == adminId);

            
            if (!string.Equals(role, "All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Role == role);

            var users = await (from u in query
                               join p in _context.Users
                                   on u.ParentUserId equals p.Id into parentGroup
                               from parent in parentGroup.DefaultIfEmpty()
                               select new
                               {
                                   u.Id,
                                   u.Username,
                                   u.Password,
                                   u.Role,
                                   u.CreatedBy,
                                   u.CreatedOn,
                                   ReportingPerson = parent != null ? parent.Username : null
                               })
                      .ToListAsync();

            return Ok(users);
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] User model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest(new { success = false, message = "Username and password are required." });

            if (string.IsNullOrEmpty(model.Role))
                return BadRequest(new { success = false, message = "Role is required (Supervisor or Operator)." });

            // Ensure role validity
            if (model.Role != "Supervisor" && model.Role != "Operator")
                return BadRequest(new { success = false, message = "Invalid role specified." });
            if (model.Role == "Supervisor")
            {
                
                model.ParentUserId = model.CreatedBy;
            }
            else if (model.Role == "Operator")
            {

                model.ParentUserId = model.ParentUserId;
            }
            model.CreatedOn = DateTime.Now;
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{model.Role} created successfully."
            });
        }
        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] User model)
        {
            if (model.Id == 0)
                return BadRequest(new { success = false, message = "User ID is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.Id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // Allow editing only name/password/role (CreatedBy remains fixed)
            user.Username = model.Username;
            user.Password = model.Password;
            user.Role = model.Role;
            user.CreatedOn = DateTime.Now; // Optional: refresh last modified time

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{model.Role} updated successfully."
            });
        }

        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            // 🧠 Optional: check hierarchy rules
            if (user.Role == "Supervisor")
            {
                bool hasOperators = await _context.Users.AnyAsync(o => o.CreatedBy == user.Id && o.Role == "Operator");
                if (hasOperators)
                    return BadRequest(new { success = false, message = "Cannot delete Supervisor with active Operators." });
            }

            if (user.Role == "Operator")
            {
                bool hasEmployees = await _context.Employees.AnyAsync(e => e.createdBy == user.Id);
                if (hasEmployees)
                    return BadRequest(new { success = false, message = "Cannot delete Operator with linked Employees." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"{user.Role} deleted successfully."
            });
        }

    }
}
