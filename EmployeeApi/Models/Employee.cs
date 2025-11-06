using System;
using System.Collections.Generic;

namespace EmployeeApi.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int? Age { get; set; }

    public string Gender { get; set; } = null!;

    public string? Address { get; set; }

    public bool? IsApproved { get; set; } = false!;
    public string? Remarks { get; set; }
    public virtual ICollection<EmployeeDocument> EmployeeDocuments { get; set; } = new List<EmployeeDocument>();
}
