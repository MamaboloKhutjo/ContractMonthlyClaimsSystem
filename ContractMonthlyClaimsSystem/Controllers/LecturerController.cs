using ContractMonthlyClaimsSystem.Models;
using ContractMonthlyClaimsSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;

        public LecturerController(IClaimRepository claimRepository, IUserRepository userRepository)
        {
            _claimRepository = claimRepository;
            _userRepository = userRepository;
        }

        private bool IsLecturerLoggedIn()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole?.ToLower() == "lecturer";
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

            if (!IsLecturerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Lecturer role required.";
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName");
            var allClaims = await _claimRepository.GetClaimsAsync();
            var myClaims = allClaims.Where(c => c.LecturerName == userName).ToList();

            var viewModel = new LecturerDashboardViewModel
            {
                CurrentUserName = userName,
                UserRole = "Lecturer",
                ClaimsSummary = new ClaimsSummary(),
                RecentClaims = myClaims
                    .OrderByDescending(c => c.SubmittedDate)
                    .Take(5)
                    .ToList(),
                MyPendingClaims = myClaims
                    .Where(c => c.Status == "Pending")
                    .ToList(),
                TotalSubmittedClaims = myClaims.Count,
                TotalApprovedAmount = myClaims
                    .Where(c => c.Status == "Approved")
                    .Sum(c => c.Amount)
            };

            viewModel.ClaimsSummary.UpdateSummary(myClaims, userName);

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult SubmitClaim()
        {
            var userName = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new SubmitClaimViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitNewClaim(SubmitClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SubmitClaim", model);
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName");
                if (string.IsNullOrEmpty(userName))
                {
                    TempData["ErrorMessage"] = "You must be logged in to submit a claim.";
                    return RedirectToAction("Login", "Account");
                }

                // Generate a unique claim ID
                var allClaims = await _claimRepository.GetClaimsAsync();
                var claimNumber = allClaims.Count + 1;
                var claimId = $"CLM{claimNumber:000}";

                
                var claim = new Claim
                {
                    ClaimId = claimId,
                    ContractName = model.ContractName,
                    Amount = model.Amount,
                    Description = model.Description ?? string.Empty,
                    LecturerName = userName,
                    LecturerEmail = $"{userName.Replace(" ", ".").ToLower()}@university.com", // Simulated email
                    SubmittedDate = DateTime.Now,
                    Status = "Pending"
                };

                await _claimRepository.AddClaimAsync(claim);

                TempData["SuccessMessage"] = $"Claim {claim.ClaimId} submitted successfully! It is now pending approval.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
                return View("SubmitClaim", model);
            }
        }

        public async Task<IActionResult> MyClaims()
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsLecturerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Lecturer role required.";
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName");
            var allClaims = await _claimRepository.GetClaimsAsync();
            var myClaims = allClaims.Where(c => c.LecturerName == userName)
                                   .OrderByDescending(c => c.SubmittedDate)
                                   .ToList();

            return View(myClaims);
        }

        [HttpGet]
        public async Task<IActionResult> ClaimDetails(string id)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsLecturerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Lecturer role required.";
                return RedirectToAction("Login", "Account");
            }

            var userName = HttpContext.Session.GetString("UserName");
            var claim = await _claimRepository.GetClaimByIdAsync(id);

            if (claim == null || claim.LecturerName != userName)
            {
                TempData["ErrorMessage"] = "Claim not found or access denied.";
                return RedirectToAction("MyClaims");
            }

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetractClaim(string claimId)
        {
            var authCheck = RedirectToLoginIfNotAuthenticated();
            if (authCheck != null) return authCheck;

            if (!IsLecturerLoggedIn())
            {
                TempData["ErrorMessage"] = "Access denied. Lecturer role required.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userName = HttpContext.Session.GetString("UserName");
                var claim = await _claimRepository.GetClaimByIdAsync(claimId);

                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction("MyClaims");
                }

                // Check if the claim belongs to the current user
                if (claim.LecturerName != userName)
                {
                    TempData["ErrorMessage"] = "You can only retract your own claims.";
                    return RedirectToAction("MyClaims");
                }

                // Check if claim can be retracted (only pending, unreviewed claims)
                if (claim.Status != "Pending" || !string.IsNullOrEmpty(claim.ReviewedBy))
                {
                    TempData["ErrorMessage"] = "This claim cannot be retracted. It may have already been reviewed or processed.";
                    return RedirectToAction("ClaimDetails", new { id = claimId });
                }

                await _claimRepository.DeleteClaimAsync(claimId);

                TempData["SuccessMessage"] = $"Claim {claimId} has been retracted successfully.";
                return RedirectToAction("MyClaims");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while retracting the claim.";
                return RedirectToAction("MyClaims");
            }
        }
    }
}