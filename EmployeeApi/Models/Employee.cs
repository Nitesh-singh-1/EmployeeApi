using System;
using System.Collections.Generic;

namespace EmployeeApi.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string? FileUniqueId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int? Age { get; set; }

    public string Gender { get; set; } = null!;

    public string? Address { get; set; }

    public bool? IsApproved { get; set; }
    public string? Remarks { get; set; }
    public int? createdBy { get; set; }

    public DateTime? createdOn { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedBy { get; set; }

    public string? Subject { get; set; }

    public string? toYear { get; set; }
    public bool IsDeleted { get; set; } = false; 
    public bool IsDeleteRequested { get; set; } = false;
    public int? DeleteRequestedBy { get; set; }
    public DateTime? DeleteRequestedOn { get; set; }

    public int? DeleteApprovedBy { get; set; }
    public DateTime? DeleteApprovedOn { get; set; }

    public int TotalpageCount { get; set; }

    public virtual ICollection<EmployeeDocument> EmployeeDocuments { get; set; } = new List<EmployeeDocument>();
}
