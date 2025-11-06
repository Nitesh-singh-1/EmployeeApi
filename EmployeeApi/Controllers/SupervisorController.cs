using EmployeeApi.DTO;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupervisorController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        public SupervisorController(EmployeeDbContext context)
        {
            _context = context;
            
        }

        [HttpPost("Approve")]
        public async Task<IActionResult> Approve([FromBody] ApprovalRequest request)
        {
            var emp = await _context.Employees.FindAsync(request.Id);
            if (emp == null)
                return NotFound(new { messgae = "Employee Not Found." });
            if (string.IsNullOrWhiteSpace(request.Remark))
                return BadRequest(new { message = "Remark is required for approval or rejection." });
            emp.IsApproved = request.isApproved;
            emp.Remarks = request.Remark;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = request.isApproved ? "Employee approved successfully." : "Employee rejected successfully.",
                employeeId = emp.Id,
                isApproved = emp.IsApproved

            });
        }
        
    }
}
