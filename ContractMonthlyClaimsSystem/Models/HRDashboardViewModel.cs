using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class HRDashboardViewModel
    {
        public int TotalLecturers { get; set; }
        public int ActiveClaims { get; set; }
        public int ClaimsThisMonth { get; set; }
        public decimal TotalAmountPending { get; set; }
        public List<Claim> RecentApprovedClaims { get; set; } = new List<Claim>();
        public List<User> RecentUpdatedLecturers { get; set; } = new List<User>();
        public List<PaymentBatch> RecentPaymentBatches { get; set; } = new List<PaymentBatch>();
        public List<Report> GeneratedReports { get; set; } = new List<Report>();
        public List<Claim> AllClaims { get; set; } = new List<Claim>();
        public List<User> AllLecturers { get; set; } = new List<User>();
    }

    public class PaymentBatch
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ClaimCount { get; set; }
        public string Status { get; set; } = "Generated";
        public string FilePath { get; set; } = string.Empty;
    }

    public class Report
    {
        public int ReportId { get; set; }
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public class LecturerEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "Employee ID")]
        public string EmployeeId { get; set; } = string.Empty;

        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = string.Empty;

        [Display(Name = "Branch Code")]
        public string BranchCode { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";
    }
}