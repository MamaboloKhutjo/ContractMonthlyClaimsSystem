using ContractMonthlyClaimsSystem.Models;
using ContractMonthlyClaimsSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class HRController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;
        private static List<PaymentBatch> _paymentBatches = new List<PaymentBatch>();
        private static List<Report> _generatedReports = new List<Report>();

        public HRController(IClaimRepository claimRepository, IUserRepository userRepository, IWebHostEnvironment environment)
        {
            _claimRepository = claimRepository;
            _userRepository = userRepository;
            _environment = environment;
        }

        private bool IsHRLoggedIn()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole?.ToLower() == "hr";
        }

        private IActionResult RedirectToLoginIfNotAuthenticated()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                TempData["ErrorMessage"] = "Please log in to access this page.";
                return RedirectToAction("Login", "Account");
            }
            return null;
        }

        public async Task<IActionResult> Dashboard()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsHRLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. HR role required.";
                return RedirectToAction("Login", "Account");
            }

            var allClaims = await _claimRepository.GetClaimsAsync();
            var allUsers = await _userRepository.GetUsersAsync();
            var lecturers = allUsers.Where(u => u.Role.ToLower() == "lecturer").ToList();

            var model = new HRDashboardViewModel
            {
                TotalLecturers = lecturers.Count,
                ActiveClaims = allClaims.Count(c => c.Status == "Pending" || c.Status == "Approved"),
                ClaimsThisMonth = allClaims.Count(c => c.SubmittedDate.Month == DateTime.Now.Month),
                TotalAmountPending = allClaims.Where(c => c.Status == "Approved").Sum(c => c.Amount),
                RecentApprovedClaims = allClaims
                    .Where(c => c.Status == "Approved")
                    .OrderByDescending(c => c.ReviewedDate)
                    .Take(5)
                    .ToList(),
                RecentUpdatedLecturers = lecturers
                    .OrderByDescending(u => u.Id)
                    .Take(5)
                    .ToList(),
                RecentPaymentBatches = _paymentBatches
                    .OrderByDescending(b => b.CreatedDate)
                    .Take(5)
                    .ToList(),
                GeneratedReports = _generatedReports
                    .OrderByDescending(r => r.GeneratedDate)
                    .Take(5)
                    .ToList(),
                AllClaims = allClaims,
                AllLecturers = lecturers
            };

            return View(model);
        }

        // CLAIMS MANAGEMENT
        public async Task<IActionResult> ViewClaims(string status = "all")
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            var allClaims = await _claimRepository.GetClaimsAsync();

            var filteredClaims = status.ToLower() switch
            {
                "pending" => allClaims.Where(c => c.Status == "Pending").ToList(),
                "approved" => allClaims.Where(c => c.Status == "Approved").ToList(),
                "rejected" => allClaims.Where(c => c.Status == "Rejected").ToList(),
                "paid" => allClaims.Where(c => c.Status == "Paid").ToList(),
                _ => allClaims
            };

            ViewBag.CurrentFilter = status;
            return View(filteredClaims.OrderByDescending(c => c.SubmittedDate).ToList());
        }

        public async Task<IActionResult> ClaimDetails(string id)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("ViewClaims");
            }

            return View(claim);
        }

        // LECTURER MANAGEMENT
        public async Task<IActionResult> ManageLecturers()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            var lecturers = (await _userRepository.GetUsersAsync())
                .Where(u => u.Role.ToLower() == "lecturer")
                .ToList();

            return View(lecturers);
        }

        [HttpGet]
        public async Task<IActionResult> EditLecturer(int id)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            var lecturer = await _userRepository.GetUserByIdAsync(id);
            if (lecturer == null || lecturer.Role.ToLower() != "lecturer")
            {
                TempData["ErrorMessage"] = "Lecturer not found.";
                return RedirectToAction("ManageLecturers");
            }

            var viewModel = new LecturerEditViewModel
            {
                Id = lecturer.Id,
                Name = lecturer.Name,
                Surname = lecturer.Surname,
                Email = $"{lecturer.Name.ToLower()}.{lecturer.Surname.ToLower()}@university.com",
                PhoneNumber = "+27 12 345 6789", 
                Department = "Computer Science", 
                EmployeeId = $"EMP{lecturer.Id:000}",
                BankAccountNumber = "123456789",
                BankName = "Standard Bank",
                BranchCode = "051001",
                Status = "Active"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(LecturerEditViewModel model)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var lecturer = await _userRepository.GetUserByIdAsync(model.Id);
                if (lecturer == null)
                {
                    TempData["ErrorMessage"] = "Lecturer not found.";
                    return RedirectToAction("ManageLecturers");
                }

                // Update lecturer information
                lecturer.Name = model.Name;
                lecturer.Surname = model.Surname;

                await _userRepository.UpdateUserAsync(lecturer);

                TempData["SuccessMessage"] = $"Lecturer {model.Name} {model.Surname} updated successfully.";
                return RedirectToAction("ManageLecturers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating lecturer: {ex.Message}";
                return View(model);
            }
        }

        // PAYMENT PROCESSING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePaymentBatch()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var approvedClaims = (await _claimRepository.GetClaimsAsync())
                    .Where(c => c.Status == "Approved")
                    .ToList();

                if (!approvedClaims.Any())
                {
                    TempData["ErrorMessage"] = "No approved claims available for payment processing.";
                    return RedirectToAction("Dashboard");
                }

                // Generate payment batch
                var batch = new PaymentBatch
                {
                    BatchId = _paymentBatches.Count + 1,
                    BatchNumber = $"BATCH-{DateTime.Now:yyyyMMdd-HHmmss}",
                    CreatedBy = HttpContext.Session.GetString("UserName") ?? "HR_User",
                    TotalAmount = approvedClaims.Sum(c => c.Amount),
                    ClaimCount = approvedClaims.Count,
                    Status = "Generated"
                };

                // Generate payment CSV file
                var csvContent = GeneratePaymentCSV(approvedClaims);
                var fileName = $"{batch.BatchNumber}.csv";
                var reportsPath = Path.Combine(_environment.WebRootPath, "reports");
                Directory.CreateDirectory(reportsPath);
                var filePath = Path.Combine(reportsPath, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, csvContent);
                batch.FilePath = $"/reports/{fileName}";

                // Update claims status to "Paid"
                foreach (var claim in approvedClaims)
                {
                    claim.Status = "Paid";
                    claim.ReviewNotes += $"\nIncluded in payment batch {batch.BatchNumber} on {DateTime.Now:yyyy-MM-dd}";
                    await _claimRepository.UpdateClaimAsync(claim);
                }

                _paymentBatches.Add(batch);

                TempData["SuccessMessage"] = $"Payment batch {batch.BatchNumber} generated successfully with {batch.ClaimCount} claims totaling R{batch.TotalAmount:N2}.";
                TempData["DownloadPath"] = batch.FilePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating payment batch: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> PaymentBatches()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            return View(_paymentBatches.OrderByDescending(b => b.CreatedDate).ToList());
        }

        // REPORT GENERATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(string reportType, DateTime fromDate, DateTime toDate)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var allClaims = await _claimRepository.GetClaimsAsync();
                var filteredClaims = allClaims
                    .Where(c => c.SubmittedDate >= fromDate && c.SubmittedDate <= toDate.AddDays(1))
                    .ToList();

                var reportContent = GenerateReportContent(reportType, filteredClaims, fromDate, toDate);
                var report = new Report
                {
                    ReportId = _generatedReports.Count + 1,
                    ReportName = $"{reportType}_Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                    ReportType = reportType,
                    GeneratedBy = HttpContext.Session.GetString("UserName") ?? "HR_User",
                    GeneratedDate = DateTime.Now
                };

                var fileName = $"{report.ReportName}.txt";
                var reportsPath = Path.Combine(_environment.WebRootPath, "reports");
                Directory.CreateDirectory(reportsPath);
                var filePath = Path.Combine(reportsPath, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, reportContent);
                report.FilePath = $"/reports/{fileName}";

                _generatedReports.Add(report);

                TempData["SuccessMessage"] = $"{reportType} report generated successfully.";
                TempData["DownloadPath"] = report.FilePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating report: {ex.Message}";
            }

            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Reports()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            return View(_generatedReports.OrderByDescending(r => r.GeneratedDate).ToList());
        }

        // DATA EXPORT
        public async Task<IActionResult> DownloadLecturerData()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var lecturers = (await _userRepository.GetUsersAsync())
                    .Where(u => u.Role.ToLower() == "lecturer")
                    .ToList();

                var csvContent = GenerateLecturerCSV(lecturers);
                var fileName = $"Lecturer_Data_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting lecturer data: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> PrintLecturers()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            var lecturers = (await _userRepository.GetUsersAsync())
                .Where(u => u.Role.ToLower() == "lecturer")
                .ToList();

            return View(lecturers);
        }

        public async Task<IActionResult> GenerateInvoice(string claimId)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var claim = await _claimRepository.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("ViewClaims");
                }

                var invoiceContent = GenerateInvoiceContent(claim);
                var fileName = $"Invoice_{claimId}_{DateTime.Now:yyyyMMdd}.txt";
                var reportsPath = Path.Combine(_environment.WebRootPath, "reports");
                Directory.CreateDirectory(reportsPath);
                var filePath = Path.Combine(reportsPath, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, invoiceContent);

                TempData["SuccessMessage"] = $"Invoice for claim {claimId} generated successfully.";
                TempData["DownloadPath"] = $"/reports/{fileName}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating invoice: {ex.Message}";
            }

            return RedirectToAction("ViewClaims");
        }

        // PRIVATE HELPER METHODS
        private string GeneratePaymentCSV(List<Claim> claims)
        {
            var csv = new StringBuilder();
            csv.AppendLine("EmployeeID,FirstName,LastName,BankAccount,BankName,BranchCode,Amount,Description,ClaimID");

            foreach (var claim in claims)
            {
                csv.AppendLine($"\"EMP001\",\"{claim.LecturerName.Split(' ')[0]}\",\"{claim.LecturerName.Split(' ').Last()}\",\"123456789\",\"Standard Bank\",\"051001\",\"{claim.Amount}\",\"Payment for {claim.ContractName}\",\"{claim.ClaimId}\"");
            }

            return csv.ToString();
        }

        private string GenerateLecturerCSV(List<User> lecturers)
        {
            var csv = new StringBuilder();
            csv.AppendLine("EmployeeID,FirstName,LastName,Email,Department,Status,PhoneNumber");

            foreach (var lecturer in lecturers)
            {
                csv.AppendLine($"\"EMP{lecturer.Id:000}\",\"{lecturer.Name}\",\"{lecturer.Surname}\",\"{lecturer.Name.ToLower()}.{lecturer.Surname.ToLower()}@university.com\",\"Computer Science\",\"Active\",\"+27 12 345 6789\"");
            }

            return csv.ToString();
        }

        private string GenerateReportContent(string reportType, List<Claim> claims, DateTime fromDate, DateTime toDate)
        {
            var report = new StringBuilder();

            switch (reportType.ToLower())
            {
                case "monthly":
                    report.AppendLine($"MONTHLY CLAIMS REPORT: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
                    report.AppendLine("==============================================");
                    report.AppendLine($"Total Claims: {claims.Count}");
                    report.AppendLine($"Total Amount: R{claims.Sum(c => c.Amount)}");
                    report.AppendLine($"Approved Claims: {claims.Count(c => c.Status == "Approved" || c.Status == "Paid")}");
                    report.AppendLine($"Pending Claims: {claims.Count(c => c.Status == "Pending")}");
                    report.AppendLine($"Rejected Claims: {claims.Count(c => c.Status == "Rejected")}");
                    break;

                case "department":
                    report.AppendLine("DEPARTMENT CLAIMS SUMMARY");
                    report.AppendLine("=========================");
                    var departmentGroups = claims.GroupBy(c => c.ContractName.Split(' ').First())
                                               .OrderByDescending(g => g.Sum(c => c.Amount));
                    foreach (var group in departmentGroups)
                    {
                        report.AppendLine($"\n{group.Key}:");
                        report.AppendLine($"  Total Claims: {group.Count()}");
                        report.AppendLine($"  Total Amount: R{group.Sum(c => c.Amount)}");
                        report.AppendLine($"  Average Claim: R{group.Average(c => c.Amount)}");
                    }
                    break;

                case "contract":
                    report.AppendLine("CONTRACT CLAIMS ANALYSIS");
                    report.AppendLine("========================");
                    var contractGroups = claims.GroupBy(c => c.ContractName)
                                             .OrderByDescending(g => g.Sum(c => c.Amount));
                    foreach (var group in contractGroups)
                    {
                        report.AppendLine($"\n{group.Key}:");
                        report.AppendLine($"  Claims: {group.Count()}");
                        report.AppendLine($"  Total Hours: {group.Sum(c => c.HoursWorked)}");
                        report.AppendLine($"  Total Amount: R{group.Sum(c => c.Amount)}");
                        report.AppendLine($"  Average Rate: R{group.Average(c => c.HourlyRate)}/hour");
                    }
                    break;

                default:
                    report.AppendLine($"CLAIMS SUMMARY REPORT: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
                    report.AppendLine("==================================================");
                    report.AppendLine($"Total Claims Submitted: {claims.Count}");
                    report.AppendLine($"Total Amount Claimed: R{claims.Sum(c => c.Amount)}");
                    report.AppendLine($"Average Claim Amount: R{claims.Average(c => c.Amount)}");
                    break;
            }

            return report.ToString();
        }

        private string GenerateInvoiceContent(Claim claim)
        {
            var invoice = new StringBuilder();
            invoice.AppendLine("INVOICE");
            invoice.AppendLine("=======");
            invoice.AppendLine($"Invoice Date: {DateTime.Now:yyyy-MM-dd}");
            invoice.AppendLine($"Claim ID: {claim.ClaimId}");
            invoice.AppendLine($"Lecturer: {claim.LecturerName}");
            invoice.AppendLine($"Contract: {claim.ContractName}");
            invoice.AppendLine($"Hours Worked: {claim.HoursWorked}");
            invoice.AppendLine($"Hourly Rate: R{claim.HourlyRate}");
            invoice.AppendLine($"Total Amount: R{claim.Amount}");
            invoice.AppendLine($"Status: {claim.Status}");
            invoice.AppendLine($"Submitted: {claim.SubmittedDate:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(claim.Description))
            {
                invoice.AppendLine($"Description: {claim.Description}");
            }

            return invoice.ToString();
        }

        public async Task<IActionResult> DownloadAllApprovedClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var approvedClaims = (await _claimRepository.GetClaimsAsync())
                    .Where(c => c.Status == "Approved")
                    .ToList();

                var csvContent = GenerateApprovedClaimsCSV(approvedClaims);
                var fileName = $"Approved_Claims_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading approved claims: {ex.Message}";
                return RedirectToAction("ViewClaims");
            }
        }

        public async Task<IActionResult> DownloadClaimsByStatus(string status)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var claims = (await _claimRepository.GetClaimsAsync())
                    .Where(c => c.Status.ToLower() == status.ToLower())
                    .ToList();

                var csvContent = GenerateClaimsCSV(claims, status);
                var fileName = $"{status}_Claims_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading {status} claims: {ex.Message}";
                return RedirectToAction("ViewClaims");
            }
        }

        public async Task<IActionResult> DownloadMonthlyReport(DateTime fromDate, DateTime toDate)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var allClaims = await _claimRepository.GetClaimsAsync();
                var filteredClaims = allClaims
                    .Where(c => c.SubmittedDate >= fromDate && c.SubmittedDate <= toDate.AddDays(1))
                    .ToList();

                var reportContent = GenerateDetailedMonthlyReport(filteredClaims, fromDate, toDate);
                var fileName = $"Detailed_Monthly_Report_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.csv";

                return File(Encoding.UTF8.GetBytes(reportContent), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading monthly report: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult DownloadReport(string reportName)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "reports", reportName);
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Report file not found.";
                    return RedirectToAction("Reports");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "text/plain", reportName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading report: {ex.Message}";
                return RedirectToAction("Reports");
            }
        }

        public IActionResult DownloadPaymentBatch(string batchNumber)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            try
            {
                var batch = _paymentBatches.FirstOrDefault(b => b.BatchNumber == batchNumber);
                if (batch == null)
                {
                    TempData["ErrorMessage"] = "Payment batch not found.";
                    return RedirectToAction("PaymentBatches");
                }

                var filePath = Path.Combine(_environment.WebRootPath, "reports", $"{batchNumber}.csv");
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Payment batch file not found.";
                    return RedirectToAction("PaymentBatches");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "text/csv", $"{batchNumber}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading payment batch: {ex.Message}";
                return RedirectToAction("PaymentBatches");
            }
        }

        // ENHANCED CSV GENERATION METHODS
        private string GenerateApprovedClaimsCSV(List<Claim> claims)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ClaimID,LecturerName,ContractName,HoursWorked,HourlyRate,Amount,SubmittedDate,Description");

            foreach (var claim in claims)
            {
                csv.AppendLine($"\"{claim.ClaimId}\",\"{claim.LecturerName}\",\"{claim.ContractName}\",\"{claim.HoursWorked}\",\"{claim.HourlyRate}\",\"{claim.Amount}\",\"{claim.SubmittedDate:yyyy-MM-dd}\",\"{claim.Description}\"");
            }

            return csv.ToString();
        }

        private string GenerateClaimsCSV(List<Claim> claims, string status)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ClaimID,LecturerName,ContractName,HoursWorked,HourlyRate,Amount,Status,SubmittedDate,ReviewedBy,ReviewDate,Description");

            foreach (var claim in claims)
            {
                csv.AppendLine($"\"{claim.ClaimId}\",\"{claim.LecturerName}\",\"{claim.ContractName}\",\"{claim.HoursWorked}\",\"{claim.HourlyRate}\",\"{claim.Amount}\",\"{claim.Status}\",\"{claim.SubmittedDate:yyyy-MM-dd}\",\"{claim.ReviewedBy}\",\"{claim.ReviewedDate}\",\"{claim.Description}\"");
            }

            return csv.ToString();
        }

        private string GenerateDetailedMonthlyReport(List<Claim> claims, DateTime fromDate, DateTime toDate)
        {
            var csv = new StringBuilder();
            csv.AppendLine($"Monthly Claims Report: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
            csv.AppendLine("ClaimID,Lecturer,Contract,Hours,Rate,Amount,Status,Submitted,ReviewedBy");

            foreach (var claim in claims)
            {
                csv.AppendLine($"\"{claim.ClaimId}\",\"{claim.LecturerName}\",\"{claim.ContractName}\",\"{claim.HoursWorked}\",\"{claim.HourlyRate}\",\"{claim.Amount}\",\"{claim.Status}\",\"{claim.SubmittedDate:yyyy-MM-dd}\",\"{claim.ReviewedBy}\"");
            }

            csv.AppendLine();
            csv.AppendLine("SUMMARY");
            csv.AppendLine($"Total Claims,{claims.Count}");
            csv.AppendLine($"Total Amount,R{claims.Sum(c => c.Amount):N2}");
            csv.AppendLine($"Approved Claims,{claims.Count(c => c.Status == "Approved")}");
            csv.AppendLine($"Pending Claims,{claims.Count(c => c.Status == "Pending")}");
            csv.AppendLine($"Rejected Claims,{claims.Count(c => c.Status == "Rejected")}");
            csv.AppendLine($"Paid Claims,{claims.Count(c => c.Status == "Paid")}");

            return csv.ToString();
        }

        private void LogError(string method, Exception ex)
        {
            Console.WriteLine($"Error in {method}: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Error in {method}: {ex.Message}";
            System.IO.File.AppendAllText("hr_errors.log", logMessage + Environment.NewLine);
        }
    }
}