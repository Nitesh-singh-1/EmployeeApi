namespace EmployeeApi.DTO
{
    public class EmployeeAdminDto
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public bool? IsApproved { get; set; }
        public string? Remarks { get; set; }

        public int? DocumentId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime? UploadedOn { get; set; }

        public string? EnteryMadeBy { get; set; }
    }

}
