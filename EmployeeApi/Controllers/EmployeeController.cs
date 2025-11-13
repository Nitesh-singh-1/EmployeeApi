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
            var query = _context.Employees.AsQueryable();

            // ✅ Apply WHERE condition only if userId != 0
            if (userId != 0)
            {
                query = query.Where(e => e.createdBy == userId);
            }

            var employees = query
                .Select(e => new
                {
                    id = e.Id,
                    employeeName = e.EmployeeName,
                    department = e.Department,
                    designation = e.Designation,
                    age = e.Age,
                    gender = e.Gender,
                    address = e.Address,
                    isApproved = e.IsApproved,
                    remarks = e.Remarks,
                    employeeDocuments = e.EmployeeDocuments.Select(doc => new
                    {
                        documentId = doc.Id,
                        employeeId = doc.EmployeeId,
                        documentName = doc.FileName,
                        filePath = doc.FilePath,
                    }).ToList()
                })
                .ToList();

            return Ok(employees);
        }

        [HttpGet("getAllEmployee")]
        public IActionResult GetAllEmployee([FromQuery] int userId,[FromQuery] int superVisorID)
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
                    age = e.Age,
                    gender = e.Gender,
                    address = e.Address,
                    isApproved = e.IsApproved,
                    remarks = e.Remarks,
                    employeeDocuments = e.EmployeeDocuments.Select(doc => new
                    {
                        documentId = doc.Id,
                        employeeId = doc.EmployeeId,
                        documentName = doc.FileName,
                        filePath = doc.FilePath,
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
                Age = Convert.ToInt32( emp.Age),
                Gender = emp.Gender,
                Address = emp.Address,
                isApproved = emp.IsApproved,
                Remarks = emp.Remarks,
                EmployeeDocuments = emp.EmployeeDocuments.Select(d => new EmployeeDocumentResponse
                {
                    Id = d.Id,
                    EmployeeId=d.EmployeeId,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedOn=Convert.ToDateTime( d.UploadedOn),
                    ImageData = d.ImageData


                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddEmployee([FromForm] EmployeeRequest employeeRequest, List<IFormFile>? documents)
        {
            if (employeeRequest == null)
                return BadRequest("Invalid employee data");

            var employee = new Employee
            {
                EmployeeName = employeeRequest.EmployeeName,
                Department = employeeRequest.Department,
                Designation = employeeRequest.Designation,
                Age = employeeRequest.Age,
                Gender = employeeRequest.Gender,
                Address = employeeRequest.Address,
                createdOn = DateTime.Now,
                createdBy = employeeRequest.createdBy,
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");
            Directory.CreateDirectory(uploadsFolder);

            if (documents != null && documents.Count > 0)
            {
                foreach (var file in documents)
                {
                    if (file.Length > 0)
                    {
                        
                        string uniqueId = Guid.NewGuid().ToString();
                        string pdfFileName = $"{uniqueId}_{Path.GetFileName(file.FileName)}";
                        string pdfPath = Path.Combine(uploadsFolder, pdfFileName);

                        using (var stream = new FileStream(pdfPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Extract text from PDF and save as .txt
                        string extractedText = ExtractTextFromPdf(pdfPath);
                        string textFilePath = Path.Combine(uploadsFolder, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.txt");
                        await System.IO.File.WriteAllTextAsync(textFilePath, extractedText);

                        // Save reference in DB
                        var document = new EmployeeDocument
                        {
                            EmployeeId = employee.Id,
                            FileName = pdfFileName,
                            FilePath = $"/uploads/resumes/{pdfFileName}",
                            UploadedOn = DateTime.Now,
                            CreatedBy=employee.createdBy,
                            CreatedOn = DateTime.Now,
                        };
                        _context.EmployeeDocuments.Add(document);
                    }
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Employee added successfully!",
                employeeId = employee.Id
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
        public async Task<IActionResult> EditEmployee(int id, [FromForm] EmployeeRequest model, List<IFormFile>? documents)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //  Update basic info
                employee.EmployeeName = model.EmployeeName;
                employee.Department = model.Department;
                employee.Designation = model.Designation;
                employee.Age = model.Age;
                employee.Gender = model.Gender;
                employee.Address = model.Address;
                employee.ModifiedBy = model.ModifiedBy;
                employee.ModifiedOn = DateTime.Now;
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                //  Save new uploaded documents
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");
                Directory.CreateDirectory(uploadsFolder);

                if (documents != null && documents.Count > 0)
                {
                    foreach (var file in documents)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueId = Guid.NewGuid().ToString();
                            string pdfFileName = $"{uniqueId}_{Path.GetFileName(file.FileName)}";
                            string pdfPath = Path.Combine(uploadsFolder, pdfFileName);

                            // Save file
                            using (var stream = new FileStream(pdfPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Extract text from PDF
                            string extractedText = ExtractTextFromPdf(pdfPath);
                            string textFilePath = Path.Combine(uploadsFolder, $"{Path.GetFileNameWithoutExtension(pdfFileName)}.txt");
                            await System.IO.File.WriteAllTextAsync(textFilePath, extractedText);

                            // Save reference in DB
                            var document = new EmployeeDocument
                            {
                                EmployeeId = employee.Id,
                                FileName = pdfFileName,
                                FilePath = $"/uploads/resumes/{pdfFileName}",
                                UploadedOn = DateTime.Now,
                                CreatedBy=employee.createdBy,
                                CreatedOn = DateTime.Now,

                            };

                            _context.EmployeeDocuments.Add(document);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Employee updated successfully!",
                    employeeId = employee.Id
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


        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var emp = _context.Employees.Find(id);


            if (emp == null) 
                return NotFound(new {messgae="Employee Not Found."});

            var docs = _context.EmployeeDocuments.Where(emp => emp.EmployeeId == id).ToList();

            _context.EmployeeDocuments.RemoveRange(docs);
            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message="Employee Deleted Successfully",
                employeeId = id

            });
        }

        [HttpDelete("deleteImg/{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {

            var doc = _context.EmployeeDocuments.Find(id);
            if (doc != null)
            {
                _context.EmployeeDocuments.Remove(doc);
                _context.SaveChanges();
            }
            return Ok(new
            {
                message = "Document Deleted Successfully",
                documentId=id,
                employeeId = doc.EmployeeId
            });

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

        [HttpGet("SearchResumes")]
        public IActionResult SearchResumes([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { message = "Keyword is required." });

            try
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");

                if (!Directory.Exists(uploadsFolder))
                    return NotFound(new { message = "Resume folder not found." });

                var txtFiles = Directory.GetFiles(uploadsFolder, "*.txt", SearchOption.TopDirectoryOnly);

                var matchingFiles = new List<object>();

                foreach (var txtFile in txtFiles)
                {
                    string content = System.IO.File.ReadAllText(txtFile);

                    if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        string baseName = Path.GetFileNameWithoutExtension(txtFile);
                        string pdfFile = Directory.GetFiles(uploadsFolder, baseName + ".pdf").FirstOrDefault();

                        if (pdfFile != null)
                        {
                            matchingFiles.Add(new
                            {
                                ResumeName = Path.GetFileName(pdfFile),
                                ResumeUrl = $"/uploads/resumes/{Path.GetFileName(pdfFile)}"
                            });
                        }
                    }
                }

                if (matchingFiles.Count == 0)
                    return NotFound(new { message = "No matching resumes found." });

                return Ok(matchingFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching resumes.", error = ex.Message });
            }
        }

    }
}
