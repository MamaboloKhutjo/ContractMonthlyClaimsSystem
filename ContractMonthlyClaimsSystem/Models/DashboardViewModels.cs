using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class DashboardViewModel
    {
        public string CurrentUserName { get; set; }
        public string UserRole { get; set; }
        public ClaimsSummary ClaimsSummary { get; set; }
        public List<Claim> RecentClaims { get; set; } = new List<Claim>();
        public List<Claim> PendingApprovalClaims { get; set; } = new List<Claim>();
    }

    public class LecturerDashboardViewModel : DashboardViewModel
    {
        public int TotalSubmittedClaims { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public List<Claim> MyPendingClaims { get; set; } = new List<Claim>();
    }

    public class ManagerDashboardViewModel : DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int PendingApprovalsCount { get; set; }
        public decimal MonthlyApprovedAmount { get; set; }
        public List<UserStats> UserStatistics { get; set; } = new List<UserStats>();
    }

    public class UserStats
    {
        public string UserName { get; set; }
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ClaimsSummary
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal PendingAmount { get; set; }

        public string FormattedApprovedAmount => $"R {ApprovedAmount:N2}";
        public string FormattedPendingAmount => $"R {PendingAmount:N2}";

        public void UpdateSummary(List<Claim> claims, string lecturerName = null)
        {
            var filteredClaims = claims;

            if (!string.IsNullOrEmpty(lecturerName))
            {
                filteredClaims = claims.Where(c => c.LecturerName == lecturerName).ToList();
            }

            TotalClaims = filteredClaims.Count;
            PendingClaims = filteredClaims.Count(c => c.Status == "Pending");
            ApprovedClaims = filteredClaims.Count(c => c.Status == "Approved");
            RejectedClaims = filteredClaims.Count(c => c.Status == "Rejected");
            ApprovedAmount = filteredClaims.Where(c => c.Status == "Approved").Sum(c => c.Amount);
            PendingAmount = filteredClaims.Where(c => c.Status == "Pending").Sum(c => c.Amount);
        }
    }
}