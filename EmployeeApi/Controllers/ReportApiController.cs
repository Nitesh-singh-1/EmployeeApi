using EmployeeApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportApiController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        public ReportApiController(EmployeeDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetUserReport")]
        public async Task<IActionResult> GetUserReport(
            [FromQuery] string role = "All",
            [FromQuery] int? userId = null,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            // Convert DateOnly to DateTime for filtering
            DateTime start = startDate?.ToDateTime(TimeOnly.MinValue) ?? new DateTime(1753, 1, 1);
            DateTime end = endDate?.ToDateTime(TimeOnly.MaxValue) ?? new DateTime(9999, 12, 31, 23, 59, 59);

            // Inline SQL string (⚠️ non-parameterized)
            string sql = $@"
        SELECT 
            u.Username,
            u.Role,
            COUNT(CASE WHEN e.CreatedBy = u.Id THEN 1 END) AS EntryMade,
            COUNT(CASE WHEN e.ApprovedBy = u.Id 
                AND u.ParentUserId IS NOT NULL THEN 1 END) AS [ApprovedOrRejected],
            MAX(e.CreatedOn) AS LastEntry
        FROM Users u
        LEFT JOIN Employees e 
            ON u.Id IN (e.CreatedBy, e.ApprovedBy)
        WHERE u.Role IN ('Operator', 'Supervisor')
          AND e.CreatedOn BETWEEN '{start:yyyy-MM-dd HH:mm:ss}' AND '{end:yyyy-MM-dd HH:mm:ss}'
        GROUP BY u.Username, u.Role
        ORDER BY u.Role, u.Username;
    ";

            // Optional: apply role filter dynamically
            if (!string.Equals(role, "All", StringComparison.OrdinalIgnoreCase))
            {
                sql = sql.Replace("IN ('Operator', 'Supervisor')",
                                  $"= '{role}'");
            }

            var report = await _context.Set<UserReportViewModel>()
                .FromSqlRaw(sql)
                .ToListAsync();

            return Ok(report);
        }

        [HttpGet("GetSupervisorReport/{supervisorId}")]
        public async Task<IActionResult> GetSupervisorReport(
            int supervisorId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Fetch operators created by this supervisor
            var operators = await _context.Users
                .Where(u => u.Role == "Operator" && u.CreatedBy == supervisorId)
                .ToListAsync();

            var operatorIds = operators.Select(o => o.Id).ToList();

            var query = _context.Employees
                .Where(e => operatorIds.Contains(e.createdBy ?? 0));

            if (startDate.HasValue)
                query = query.Where(e => e.createdOn >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.createdOn <= endDate.Value);

            var report = await query
                .GroupBy(e => e.createdBy)
                .Select(g => new
                {
                    OperatorId = g.Key,
                    OperatorName = _context.Users.FirstOrDefault(u => u.Id == g.Key).Username,
                    TotalEntries = g.Count(),
                    Approvals = g.Count(e => e.IsApproved == true),
                    Rejections = g.Count(e => e.IsApproved == false),
                    Pending = g.Count(e => e.IsApproved == null)
                })
                .OrderByDescending(r => r.TotalEntries)
                .ToListAsync();

            return Ok(report);
        }


    }
}
