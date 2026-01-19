namespace EmployeeApi.DTO
{
    public class EmployeeRequest
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
        public string? Remarks { get; set; }
        public int? createdBy { get; set; }
        public string subject { get; set; } = string.Empty;
        public string ToYear { get; set; } = string.Empty;
        public DateTime? createdOn { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public List<IFormFile>? Documents { get; set; }
    }

    public class AddEmployeeRequest
    {

        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Address { get; set; } = string.Empty;
        public string ToYear { get; set; } = string.Empty;
        public int? createdBy { get; set; }

        //public List<IFormFile>? Documents { get; set; }
    }

    public class EmployeeResponse
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public bool? isApproved { get; set; }
        public string Remarks { get; set; }
        public string subject { get; set; }
        public string ToYear { get; set; }
        public List<EmployeeDocumentResponse> EmployeeDocuments { get; set; } = new();
    }

    public class EmployeeDocumentResponse
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }
        public DateTime UploadedOn { get; set; }
        public byte[] ImageData { get; set; }

    }

    public class ApprovalRequest
    {
        public int Id { get; set; }
        public bool isApproved { get; set; }
        public string Remark { get; set; } = string.Empty;
        public int userId { get; set; }
    }
}
