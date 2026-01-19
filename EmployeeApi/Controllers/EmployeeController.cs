using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using EmployeeApi.DTO;
using EmployeeApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.VisualBasic;
using System.Linq;
using System.Text;
using PdfSharpCore.Pdf.IO;

namespace EmployeeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public EmployeeController(EmployeeDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        [HttpGet("getAll")]
        public IActionResult GetAll([FromQuery] int userId)
        {
            var query2 = _context.Users.Where(e => e.ParentUserId == userId);

            var query = _context.Employees.AsQueryable();

            // ✅ Apply WHERE condition only if userId != 0
            if (userId != 0)
            {
                // 🔍 Step 1: Find child user
                var childUserId = _context.Users.AsNoTracking()
                    .Where(u => u.ParentUserId == userId)
                    .Select(u => u.Id)
                    .FirstOrDefault();

                // 🔁 Step 2: Decide which userId to use
                var effectiveUserId = childUserId != 0
                    ? childUserId
                    : userId;

                // ✅ Step 3: Apply employee filter
                query = query.Where(e => e.createdBy == effectiveUserId);
            }

            var employees = query
                .Select(e => new
                {
                    id = e.Id,
                    employeeName = e.EmployeeName,
                    department = e.Department,
                    designation = e.Designation,
                    age = e.Age ?? 0,
                    gender = e.Gender,
                    address = e.Address,
                    isApproved = e.IsApproved,
                    remarks = e.Remarks,
                    subject = e.Subject,
                    ToYear = e.toYear,
                    isDeleteRequested = e.IsDeleteRequested,
                    isDeleted = e.IsDeleted,
                    fileUniqueId = e.FileUniqueId,
                    TotalpageCount = e.TotalpageCount,


                    employeeDocuments = e.EmployeeDocuments.Select(doc => new
                    {
                        Id = doc.Id,
                        EmployeeId = doc.EmployeeId,
                        FileName = doc.FileName,
                        FilePath = doc.FilePath,
                        //FileUniqueId = doc.file
                    }).ToList()
                })
                .ToList();

            return Ok(employees);
        }

        [HttpGet("getAllEmployee")]
        public IActionResult GetAllEmployee([FromQuery] int userId, [FromQuery] int superVisorID)
        {
            // Step 1️⃣: Start with base query
            var query = _context.Employees.AsQueryable();

            // Step 2️⃣: If a specific userId is provided → get only that user's employees
            if (userId != 0)
            {
                query = query.Where(e => e.createdBy == userId);
            }
            // Step 3️⃣: Else, if supervisor ID is provided → find operators under that supervisor
            else if (superVisorID != 0)
            {
                var childUserIds = _context.Users
                    .Where(u => u.ParentUserId == superVisorID)
                    .Select(u => u.Id)
                    .ToList();

                // Get all employees created by those child users
                query = query.Where(e => e.createdBy.HasValue && childUserIds.Contains(e.createdBy.Value));

            }

            // Step 4️⃣: Select the data and shape it
            var employees = query
                .Select(e => new
                {
                    id = e.Id,
                    employeeName = e.EmployeeName,
                    department = e.Department,
                    designation = e.Designation,
                    age = e.Age ?? 0,
                    gender = e.Gender,
                    address = e.Address,
                    isApproved = e.IsApproved,
                    remarks = e.Remarks,
                    subject = e.Subject,
                    ToYear = e.toYear,
                    employeeDocuments = e.EmployeeDocuments.Select(doc => new
                    {
                        Id = doc.Id,
                        EmployeeId = doc.EmployeeId,
                        FileName = doc.FileName,
                        FilePath = doc.FilePath,
                    }).ToList()
                })
                .ToList();

            return Ok(employees);
        }
        [HttpGet("getAllEmployeeforAdmin")]
        public IActionResult GetAllEmployeeforAdmin()
        {
            var query = _context.Employees.AsQueryable();
            var employees = query
         .Select(e => new
         {
             id = e.Id,
             employeeName = e.EmployeeName,
             department = e.Department,
             designation = e.Designation,
             age = e.Age ?? 0,
             gender = e.Gender,
             address = e.Address,
             isApproved = e.IsApproved,
             remarks = e.Remarks,
             subject = e.Subject,
             ToYear = e.toYear,
             isDeleteRequested = e.IsDeleteRequested,
             isDeleted = e.IsDeleted,
             employeeDocuments = e.EmployeeDocuments.Select(doc => new
             {
                 Id = doc.Id,
                 EmployeeId = doc.EmployeeId,
                 FileName = doc.FileName,
                 FilePath = doc.FilePath,
             }).ToList()
         })
         .ToList();

            return Ok(employees);
        }

        [HttpGet("GetEmpById/{id}")]
        public IActionResult Get(int id)
        {
            var emp = _context.Employees
        .Include(e => e.EmployeeDocuments)
        .FirstOrDefault(e => e.Id == id);

            if (emp == null) return NotFound();
            var response = new EmployeeResponse
            {
                Id = emp.Id,
                EmployeeName = emp.EmployeeName,
                Department = emp.Department,
                Designation = emp.Designation,
                Age = emp.Age??0,
                Gender = emp.Gender,
                Address = emp.Address,
                isApproved = emp.IsApproved,
                Remarks = emp.Remarks,
                subject = emp.Subject,
                ToYear = emp.toYear,
                EmployeeDocuments = emp.EmployeeDocuments.Select(d => new EmployeeDocumentResponse
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedOn = Convert.ToDateTime(d.UploadedOn),
                    ImageData = d.ImageData


                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddEmployee(
     [FromForm] AddEmployeeRequest employeeRequest,
     List<IFormFile>? documents)
        {
            if (employeeRequest == null)
                return BadRequest("Invalid employee data");

            // 🔑 One Unique ID = One File
           // Guid fileUniqueId = Guid.NewGuid();

            var employee = new Employee
            {
                //FileUniqueId = fileUniqueId,
                EmployeeName = employeeRequest.EmployeeName,
                Department = employeeRequest.Department,
                Designation = employeeRequest.Designation,
                Subject = employeeRequest.Subject,
                Gender = "Gender",
                //Address = employeeRequest.Address,
                //toYear = employeeRequest.ToYear,
                Address = string.IsNullOrWhiteSpace(employeeRequest.Address)
                ? null
                : employeeRequest.Address,

                toYear = string.IsNullOrWhiteSpace(employeeRequest.ToYear)
                ? null
                : employeeRequest.ToYear,
                createdOn = DateTime.Now,
                createdBy = employeeRequest.createdBy,
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            employee.FileUniqueId = employee.Id.ToString("D7");

            _context.Entry(employee)
        .Property(e => e.FileUniqueId)
        .IsModified = true;

            await _context.SaveChangesAsync();

            string fileUniqueId = employee.FileUniqueId;

            string uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                fileUniqueId
            );

            Directory.CreateDirectory(uploadsFolder);

            int documentSequence = 1;

            if (documents != null && documents.Count > 0)
            {
                foreach (var file in documents)
                {
                    if (file.Length == 0) continue;

                    string extension = Path.GetExtension(file.FileName);
                    string storedFileName = $"{fileUniqueId}-{documentSequence}{extension}";
                    string filePath = Path.Combine(uploadsFolder, storedFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    
                    //await System.IO.File.WriteAllTextAsync(textFilePath, extractedText);

                    var document = new EmployeeDocument
                    {
                        EmployeeId = employee.Id,
                        FileUniqueId = fileUniqueId,
                        DocumentSequence = documentSequence,
                        FileName = storedFileName,
                        FilePath = $"/uploads/{fileUniqueId}/{storedFileName}",
                        UploadedOn = DateTime.Now,
                        CreatedBy = employee.createdBy,
                        CreatedOn = DateTime.Now
                    };

                    _context.EmployeeDocuments.Add(document);

                    documentSequence++;
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "File created successfully!",
                fileUniqueId = fileUniqueId,
                employeeId = employee.Id,
                totalDocuments = documentSequence - 1
            });
        }

        private string ExtractTextFromPdf(string filePath)
        {
            var sb = new StringBuilder();

            try
            {

                using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(612, 792));

                int pageCount = docReader.GetPageCount();
                for (int i = 0; i < pageCount; i++)
                {
                    using var pageReader = docReader.GetPageReader(i);

                    var pageText = pageReader.GetText();
                    sb.AppendLine(pageText);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Docnet text extraction failed: {ex.Message}");
            }

            return sb.ToString();
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> EditEmployee(
     int id,
     [FromForm] EmployeeRequest model,
     List<IFormFile>? documents)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = await _context.Employees
                .Include(e => e.EmployeeDocuments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound(new { message = "Employee not found." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔹 Update employee fields (MATCH ADD API)
                employee.EmployeeName = model.EmployeeName;
                employee.Department = model.Department;
                employee.Designation = model.Designation;
                employee.Subject = model.subject;
                employee.Address = model.Address;
                employee.toYear = model.ToYear;
                employee.ModifiedBy = model.ModifiedBy;
                employee.ModifiedOn = DateTime.Now;

               // _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                string fileUniqueId = employee.FileUniqueId!;
                if (string.IsNullOrWhiteSpace(fileUniqueId))
                    throw new Exception("FileUniqueId missing.");


                int documentSequence =
                    employee.EmployeeDocuments.Any()
                        ? employee.EmployeeDocuments.Max(d => d.DocumentSequence) + 1
                        : 1;

                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    fileUniqueId
                );

                Directory.CreateDirectory(uploadsFolder);

                // 🔹 Save new documents (same as ADD API)
                if (documents != null && documents.Count > 0)
                {
                    foreach (var file in documents)
                    {
                        if (file.Length == 0) continue;

                        string extension = Path.GetExtension(file.FileName);
                        string storedFileName =
                            $"{fileUniqueId}-{documentSequence}{extension}";

                        string filePath = Path.Combine(uploadsFolder, storedFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Optional: extract text
                        // string extractedText = ExtractTextFromPdf(filePath);

                        var document = new EmployeeDocument
                        {
                            EmployeeId = employee.Id,
                            FileUniqueId = fileUniqueId,
                            DocumentSequence = documentSequence,
                            FileName = storedFileName,
                            FilePath = $"/uploads/{fileUniqueId}/{storedFileName}",
                            UploadedOn = DateTime.Now,
                            CreatedBy = employee.createdBy,
                            CreatedOn = DateTime.Now
                        };

                        _context.EmployeeDocuments.Add(document);
                        documentSequence++;
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Employee updated successfully!",
                    employeeId = employee.Id,
                    fileUniqueId = fileUniqueId,
                    totalDocuments = documentSequence - 1
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Failed to update employee. Transaction rolled back.",
                    error = ex.Message
                });
            }
        }

        //    [HttpPost("edit/{id}")]
        //    public async Task<IActionResult> EditEmployee(
        //int id,
        //[FromForm] EmployeeRequest model,
        //List<IFormFile>? documents)
        //    {
        //        if (!ModelState.IsValid)
        //            return BadRequest(ModelState);

        //        var employee = await _context.Employees
        //            .Include(e => e.EmployeeDocuments)
        //            .FirstOrDefaultAsync(e => e.Id == id);

        //        if (employee == null)
        //            return NotFound(new { message = "Employee not found." });

        //        using var transaction = await _context.Database.BeginTransactionAsync();

        //        try
        //        {
        //            // 🔹 Update employee fields
        //            employee.EmployeeName = model.EmployeeName;
        //            employee.Department = model.Department;
        //            employee.Designation = model.Designation;
        //            employee.Subject = model.subject;
        //            employee.Address = model.Address;
        //            employee.toYear = model.ToYear;
        //            employee.ModifiedBy = model.ModifiedBy;
        //            employee.ModifiedOn = DateTime.Now;

        //            await _context.SaveChangesAsync();

        //            // 🔑 IMPORTANT: reuse existing FileUniqueId
        //            string fileUniqueId = employee.FileUniqueId!;

        //            if (string.IsNullOrWhiteSpace(fileUniqueId))
        //                throw new Exception("FileUniqueId missing for employee.");

        //            // 🔢 Continue document sequence
        //            int documentSequence =
        //                employee.EmployeeDocuments.Any()
        //                    ? employee.EmployeeDocuments.Max(d => d.DocumentSequence) + 1
        //                    : 1;

        //            string uploadsFolder = Path.Combine(
        //                Directory.GetCurrentDirectory(),
        //                "wwwroot",
        //                "uploads",
        //                fileUniqueId
        //            );

        //            Directory.CreateDirectory(uploadsFolder);

        //            // 🔹 Save new documents
        //            if (documents != null && documents.Count > 0)
        //            {
        //                foreach (var file in documents)
        //                {
        //                    if (file.Length == 0) continue;

        //                    string extension = Path.GetExtension(file.FileName);
        //                    string storedFileName =
        //                        $"{fileUniqueId}-{documentSequence}{extension}";

        //                    string filePath = Path.Combine(uploadsFolder, storedFileName);

        //                    using var stream = new FileStream(filePath, FileMode.Create);
        //                    await file.CopyToAsync(stream);

        //                    var document = new EmployeeDocument
        //                    {
        //                        EmployeeId = employee.Id,
        //                        FileUniqueId = fileUniqueId,
        //                        DocumentSequence = documentSequence,
        //                        FileName = storedFileName,
        //                        FilePath = $"/uploads/{fileUniqueId}/{storedFileName}",
        //                        UploadedOn = DateTime.Now,
        //                        CreatedBy = employee.createdBy,
        //                        CreatedOn = DateTime.Now
        //                    };

        //                    _context.EmployeeDocuments.Add(document);
        //                    documentSequence++;
        //                }

        //                await _context.SaveChangesAsync();
        //            }

        //            await transaction.CommitAsync();

        //            return Ok(new
        //            {
        //                message = "Employee updated successfully!",
        //                employeeId = employee.Id,
        //                fileUniqueId = fileUniqueId,
        //                totalDocuments = documentSequence - 1
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            await transaction.RollbackAsync();
        //            return StatusCode(500, new
        //            {
        //                message = "Failed to update employee. Transaction rolled back.",
        //                error = ex.Message
        //            });
        //        }
        //    }




        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var emp = await _context.Employees.FindAsync(id);


            if (emp == null)
                return NotFound(new { messgae = "Employee Not Found." });

            var docs = _context.EmployeeDocuments.Where(emp => emp.EmployeeId == id).ToList();
            foreach (var doc in docs)
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath.TrimStart('/'));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Delete extracted .txt file also
                string txtFilePath = Path.ChangeExtension(fullPath, ".txt");
                if (System.IO.File.Exists(txtFilePath))
                {
                    System.IO.File.Delete(txtFilePath);
                }
            }

            _context.EmployeeDocuments.RemoveRange(docs);
            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Employee Deleted Successfully",
                employeeId = id

            });
        }

        [HttpDelete("deleteDoc/{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.EmployeeDocuments.FindAsync(id);

            if (doc == null)
                return NotFound(new { message = "Document not found." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Build absolute physical path from stored FilePath
                string fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    doc.FilePath.TrimStart('/')
                );

                // 🔹 Delete physical file (PDF)
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // 🔹 Delete extracted text file IF it exists
                string txtFilePath = Path.ChangeExtension(fullPath, ".txt");
                if (System.IO.File.Exists(txtFilePath))
                {
                    System.IO.File.Delete(txtFilePath);
                }

                // 🔹 Remove DB record
                _context.EmployeeDocuments.Remove(doc);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Document deleted successfully",
                    documentId = id,
                    employeeId = doc.EmployeeId,
                    fileUniqueId = doc.FileUniqueId
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Failed to delete document",
                    error = ex.Message
                });
            }
        }


        [HttpGet("ViewEmployeeDocument/{id}")]
        public async Task<IActionResult> ViewEmployeeDocument(int id, int? docId = null)
        {
            var employee = await _context.Employees
        .Include(e => e.EmployeeDocuments)
        .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound("Employee not found.");

            EmployeeDocument? document;

            if (docId.HasValue)
            {
                document = employee.EmployeeDocuments?.FirstOrDefault(d => d.Id == docId.Value);
                if (document == null)
                    return NotFound($"Document with ID {docId} not found for this employee.");
            }
            else
            {

                document = employee.EmployeeDocuments?.FirstOrDefault();
                if (document == null)
                    return NotFound("No document found for this employee.");
            }

            if (string.IsNullOrWhiteSpace(document.FilePath))
                return NotFound("Document file path is empty or invalid.");


            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound($"File not found on server. Path: {fullPath}");


            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };


            Response.Headers.Add("Content-Disposition", $"inline; filename={Path.GetFileName(fullPath)}");

            return PhysicalFile(fullPath, contentType);
        }
        [HttpGet("SearchFileMaster")]
        public async Task<IActionResult> SearchFileMaster([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { message = "Keyword is required." });

            keyword = keyword.Trim();

            try
            {
                // 1️⃣ Search FILE MASTER (Employees table)
                var matchedFiles = await _context.Employees
                    .Where(e =>
                        (e.EmployeeName ?? "").Contains(keyword) ||
                        (e.Department ?? "").Contains(keyword) ||
                        (e.Designation ?? "").Contains(keyword) ||
                        (e.Subject ?? "").Contains(keyword) ||
                        (e.Address ?? "").Contains(keyword) ||
                        (e.toYear ?? "").ToString().Contains(keyword)
                    )
                    .Select(e => new
                    {
                        e.FileUniqueId,
                        e.EmployeeName,
                        e.Department,
                        e.Designation,
                        e.Subject,
                        e.Address,
                        e.toYear
                    })
                    .ToListAsync();   // ✅ RETURNS LIST (duplicates allowed)

                if (!matchedFiles.Any())
                    return NotFound(new { message = "No matching files found." });

                var fileIds = matchedFiles
                    .Select(f => f.FileUniqueId)
                    .ToList();

                // 2️⃣ Fetch ALL documents for matched files
                var documents = await _context.EmployeeDocuments
                    .Where(d => fileIds.Contains(d.FileUniqueId))
                    .OrderBy(d => d.FileUniqueId)
                    .ThenBy(d => d.DocumentSequence)
                    .Select(d => new
                    {
                        d.FileUniqueId,
                        d.DocumentSequence,
                        d.FileName,
                        d.FilePath
                    })
                    .ToListAsync();

                // 3️⃣ Combine FILE + DOCUMENTS (NO GROUPING / NO DISTINCT)
                var result = matchedFiles.Select(file => new
                {
                    FileUniqueId = file.FileUniqueId,
                    FileDetails = new
                    {
                        file.EmployeeName,
                        file.Department,
                        file.Designation,
                        file.Subject,
                        file.Address,
                        file.toYear
                    },
                    Documents = documents
                        .Where(d => d.FileUniqueId == file.FileUniqueId)
                        .Select(d => new
                        {
                            d.DocumentSequence,
                            d.FileName,
                            d.FilePath
                        })
                        .ToList()
                })
                .ToList(); // ✅ ENSURE LIST OUTPUT

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while searching file master.",
                    error = ex.Message
                });
            }
        }


        [HttpPost("delete-requested/{employeeId}")]
        public async Task<IActionResult> DeleteRequested(int employeeId, [FromQuery] int userId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            if (employee.IsDeleteRequested)
                return BadRequest(new { message = "Delete already requested" });

            employee.IsDeleteRequested = true;
            employee.DeleteRequestedBy = userId;
            employee.DeleteRequestedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Delete request sent for approval",
                employeeId = employee.Id
            });
        }

        [HttpPost("delete-approved/{employeeId}")]
        public async Task<IActionResult> DeleteApproved(
    int employeeId,
    [FromQuery] int adminId,
    [FromQuery] bool approve)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeDocuments)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            if (!employee.IsDeleteRequested)
                return BadRequest(new { message = "No delete request found" });

            if (approve)
            {
                // ✅ Soft delete employee
                employee.IsDeleted = true;
                employee.DeleteApprovedBy = adminId;
                employee.DeleteApprovedOn = DateTime.Now;

                // ✅ Soft delete all documents
                foreach (var doc in employee.EmployeeDocuments)
                {
                    doc.IsDeleted = true;
                    doc.DeletedBy = adminId;
                    doc.DeletedOn = DateTime.Now;
                }
            }

            // Reset request flag whether approved or rejected
            employee.IsDeleteRequested = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = approve
                    ? "Employee deleted successfully"
                    : "Delete request rejected",
                employeeId = employee.Id
            });
        }


        private int GetPdfPageCount(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var document = PdfReader.Open(
                memoryStream,
                PdfDocumentOpenMode.ReadOnly
            );

            return document.PageCount;
        }

        [HttpPost("upload-documents/{id}")]
        public async Task<IActionResult> UploadDocuments(
    int id,
    List<IFormFile> documents)
        {
            var employee = await _context.Employees
                .Include(e => e.EmployeeDocuments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            string fileUniqueId = employee.FileUniqueId!;
            const long MAX_SIZE = 20 * 1024 * 1024; // 20 MB

            int documentSequence =
                employee.EmployeeDocuments.Any()
                    ? employee.EmployeeDocuments.Max(d => d.DocumentSequence) + 1
                    : 1;

            string uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                fileUniqueId
            );

            Directory.CreateDirectory(uploadsFolder);
            int totalPagesAdded = 0;

            foreach (var file in documents)
            {
                if (file.Length == 0) continue;
                if (file.Length > MAX_SIZE)
                    return BadRequest($"File {file.FileName} exceeds 20 MB.");

                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{fileUniqueId}-{documentSequence}{ext}";
                string path = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                int pageCount = 0;

                if (ext == ".pdf")
                {
                    pageCount = GetPdfPageCount(file);
                    totalPagesAdded += pageCount;
                }

                _context.EmployeeDocuments.Add(new EmployeeDocument
                {
                    EmployeeId = employee.Id,
                    FileUniqueId = fileUniqueId,
                    DocumentSequence = documentSequence,
                    FileName = fileName,
                    FilePath = $"/uploads/{fileUniqueId}/{fileName}",
                    pageCount = pageCount,
                    UploadedOn = DateTime.Now,
                    CreatedBy = employee.createdBy,
                    CreatedOn = DateTime.Now
                });

                documentSequence++;
            }
            employee.TotalpageCount += totalPagesAdded;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Documents uploaded" , addedPages = totalPagesAdded, totalPages = employee.TotalpageCount });
        }


    }
}
