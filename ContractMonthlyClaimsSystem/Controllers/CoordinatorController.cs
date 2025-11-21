using ContractMonthlyClaimsSystem.Models;
using ContractMonthlyClaimsSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class CoordinatorController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;

        public CoordinatorController(IClaimRepository claimRepository, IUserRepository userRepository)
        {
            _claimRepository = claimRepository;
            _userRepository = userRepository;
        }

        private bool IsCoordinatorLoggedIn()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole?.ToLower() == "program coordinator" || userRole?.ToLower() == "manager";
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

        public async Task<IActionResult> ReviewClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsCoordinatorLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Coordinator role required.";
                return RedirectToAction("Login", "Account");
            }

            var allClaims = await _claimRepository.GetClaimsAsync();
            var claimsNeedingReview = allClaims
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            return View(claimsNeedingReview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoVerifyClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsCoordinatorLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Coordinator role required.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var pendingClaims = (await _claimRepository.GetClaimsAsync())
                    .Where(c => c.Status == "Pending")
                    .ToList();

                var autoApprovedCount = 0;
                var needsReviewCount = 0;
                var rejectedCount = 0;

                foreach (var claim in pendingClaims)
                {
                    var verificationResult = VerifyClaimAutomatically(claim);

                    if (verificationResult.AutoApprove)
                    {
                        claim.Status = "Approved";
                        claim.ReviewedBy = "System (Auto)";
                        claim.ReviewedDate = DateTime.Now;
                        claim.ReviewNotes = "Automatically approved by system verification";
                        autoApprovedCount++;
                    }
                    else if (verificationResult.NeedsManualReview)
                    {
                        claim.ReviewNotes = "Requires manual review: " + string.Join(", ", verificationResult.VerificationFlags);
                        needsReviewCount++;
                    }
                    else if (!verificationResult.IsValid)
                    {
                        claim.Status = "Rejected";
                        claim.ReviewedBy = "System (Auto)";
                        claim.ReviewedDate = DateTime.Now;
                        claim.ReviewNotes = "Automatically rejected: " + string.Join(", ", verificationResult.VerificationFlags);
                        rejectedCount++;
                    }

                    await _claimRepository.UpdateClaimAsync(claim);
                }

                TempData["SuccessMessage"] = $"Auto-verification completed: {autoApprovedCount} auto-approved, {needsReviewCount} need review, {rejectedCount} rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error during auto-verification: {ex.Message}";
            }

            return RedirectToAction("ReviewClaims");
        }

        private VerificationResult VerifyClaimAutomatically(Claim claim)
        {
            var result = new VerificationResult();
            var flags = new List<string>();

            // Check hourly rate limits
            if (claim.HourlyRate > 500)
            {
                flags.Add($"Hourly rate R{claim.HourlyRate} exceeds typical limit");
                result.NeedsManualReview = true;
            }

            if (claim.HoursWorked > 160)
            {
                flags.Add($"Hours worked ({claim.HoursWorked}) exceeds typical monthly maximum");
                result.NeedsManualReview = true;
            }

            if (claim.Amount > 50000)
            {
                flags.Add($"Claim amount R{claim.Amount} exceeds R50,000 limit");
                result.IsValid = false;
            }

            if (claim.Amount < 10)
            {
                flags.Add($"Claim amount R{claim.Amount} below R10 minimum");
                result.IsValid = false;
            }

            if (claim.HourlyRate > 800)
            {
                flags.Add("Very high hourly rate detected");
                result.NeedsManualReview = true;
            }

            if (claim.HoursWorked > 744) 
            {
                flags.Add("Hours worked exceeds maximum possible");
                result.IsValid = false;
            }

            result.VerificationFlags = flags;
            result.AutoApprove = result.IsValid && !result.NeedsManualReview;

            return result;
        }
    }

    public class VerificationResult
    {
        public bool IsValid { get; set; } = true;
        public bool NeedsManualReview { get; set; }
        public bool AutoApprove { get; set; }
        public List<string> VerificationFlags { get; set; } = new List<string>();
    }
}