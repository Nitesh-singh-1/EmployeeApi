using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace EmployeeApi.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;
    public int? CreatedBy { get; set; }
    //public int? ReportsToId { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int? ParentUserId { get; set; }
}

[Keyless]
public class UserReportViewModel
{
    public string Username { get; set; }
    public string Role { get; set; }
    public int EntryMade { get; set; }
    public int ApprovedOrRejected { get; set; }
    public DateTime? LastEntry { get; set; }
}
