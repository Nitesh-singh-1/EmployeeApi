using System;
using System.Collections.Generic;

namespace EmployeeApi.Models;

public partial class EmployeeDocument
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public DateTime? UploadedOn { get; set; }

    public byte[]? ImageData { get; set; }

    
    public virtual Employee Employee { get; set; } = null!;
}
