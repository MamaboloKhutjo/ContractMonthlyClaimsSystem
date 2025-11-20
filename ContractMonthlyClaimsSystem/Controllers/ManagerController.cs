using ContractMonthlyClaimsSystem.Models;
using ContractMonthlyClaimsSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class ManagerController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;

        public ManagerController(IClaimRepository claimRepository, IUserRepository userRepository)
        {
            _claimRepository = claimRepository;
            _userRepository = userRepository;
        }

        private bool IsManagerLoggedIn()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole?.ToLower() == "manager" || userRole?.ToLower() == "program coordinator";
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

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");
            var allClaims = await _claimRepository.GetClaimsAsync();
            var allUsers = await _userRepository.GetUsersAsync();

            var viewModel = new ManagerDashboardViewModel
            {
                CurrentUserName = userName,
                UserRole = userRole ?? "Manager",
                ClaimsSummary = new ClaimsSummary(),
                RecentClaims = allClaims
                    .OrderByDescending(c => c.SubmittedDate)
                    .Take(5)
                    .ToList(),
                PendingApprovalClaims = allClaims
                    .Where(c => c.Status == "Pending")
                    .OrderByDescending(c => c.SubmittedDate)
                    .Take(5)
                    .ToList(),
                TotalUsers = allUsers.Count,
                PendingApprovalsCount = allClaims.Count(c => c.Status == "Pending"),
                MonthlyApprovedAmount = allClaims
                    .Where(c => c.Status == "Approved" && c.SubmittedDate.Month == DateTime.Now.Month)
                    .Sum(c => c.Amount),
                UserStatistics = allUsers.Select(u => new UserStats
                {
                    UserName = u.FullName,
                    TotalClaims = allClaims.Count(c => c.LecturerName == u.FullName),
                    ApprovedClaims = allClaims.Count(c => c.LecturerName == u.FullName && c.Status == "Approved"),
                    TotalAmount = allClaims
                        .Where(c => c.LecturerName == u.FullName && c.Status == "Approved")
                        .Sum(c => c.Amount)
                })
                .Where(u => u.TotalClaims > 0)
                .OrderByDescending(u => u.TotalClaims)
                .Take(5)
                .ToList()
            };

            viewModel.ClaimsSummary.UpdateSummary(allClaims);

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> ReviewClaim(string id)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            var claim = await _claimRepository.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["ErrorMessage"] = "Claim not found.";
                return RedirectToAction("Dashboard");
            }

            var viewModel = new ReviewClaimViewModel
            {
                Claim = claim,
                ReviewNotes = ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(string claimId, string reviewNotes)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var claim = await _claimRepository.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("PendingClaims");
                }

                var reviewerName = HttpContext.Session.GetString("UserName");

                claim.Status = "Approved";
                claim.ReviewedBy = reviewerName;
                claim.ReviewedDate = DateTime.Now;
                claim.ReviewNotes = reviewNotes ?? string.Empty;

                await _claimRepository.UpdateClaimAsync(claim);

                TempData["SuccessMessage"] = $"Claim {claimId} has been approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
            }

            return RedirectToAction("PendingClaims");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(string claimId, string reviewNotes)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var claim = await _claimRepository.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("PendingClaims");
                }

                var reviewerName = HttpContext.Session.GetString("UserName");

                claim.Status = "Rejected";
                claim.ReviewedBy = reviewerName;
                claim.ReviewedDate = DateTime.Now;
                claim.ReviewNotes = reviewNotes ?? string.Empty;

                await _claimRepository.UpdateClaimAsync(claim);

                TempData["SuccessMessage"] = $"Claim {claimId} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
            }

            return RedirectToAction("PendingClaims");
        }

        public async Task<IActionResult> PendingClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            var pendingClaims = await _claimRepository.GetPendingClaimsAsync();
            return View(pendingClaims.OrderByDescending(c => c.SubmittedDate).ToList());
        }

        public async Task<IActionResult> AllClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsManagerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Manager role required.";
                return RedirectToAction("Login", "Account");
            }

            var allClaims = await _claimRepository.GetClaimsAsync();
            return View(allClaims.OrderByDescending(c => c.SubmittedDate).ToList());
        }

    }
}
