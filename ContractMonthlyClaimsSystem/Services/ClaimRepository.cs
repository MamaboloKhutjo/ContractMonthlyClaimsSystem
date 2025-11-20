using ContractMonthlyClaimsSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Services
{
    public class ClaimRepository : IClaimRepository
    {
        private static List<Claim> _claims = new List<Claim>();
        private static int _claimCounter = 4; // Starting after the sample claims

        public ClaimRepository()
        {
            // Add some sample claims for testing if empty
            if (!_claims.Any())
            {
                _claims.Add(new Claim
                {
                    ClaimId = "CLM001",
                    ContractName = "Software Development 101",
                    SubmittedDate = DateTime.Now.AddDays(-5),
                    Amount = 2500.00m,
                    LecturerName = "John Doe",
                    Status = "Pending",
                    Description = "Monthly contract claim for software development course"
                });

                _claims.Add(new Claim
                {
                    ClaimId = "CLM002",
                    ContractName = "Mathematics 202",
                    SubmittedDate = DateTime.Now.AddDays(-3),
                    Amount = 1800.00m,
                    LecturerName = "John Doe",
                    Status = "Approved",
                    Description = "Advanced mathematics course claims"
                });

                _claims.Add(new Claim
                {
                    ClaimId = "CLM003",
                    ContractName = "Physics 301",
                    SubmittedDate = DateTime.Now.AddDays(-1),
                    Amount = 3200.00m,
                    LecturerName = "Jane Smith",
                    Status = "Pending",
                    Description = "Physics laboratory equipment and materials"
                });

                _claims.Add(new Claim
                {
                    ClaimId = "CLM004",
                    ContractName = "Engineering 401",
                    SubmittedDate = DateTime.Now.AddDays(-7),
                    Amount = 2750.00m,
                    LecturerName = "Bob Johnson",
                    Status = "Rejected",
                    Description = "Engineering workshop materials claim"
                });
            }
        }

        public Task<List<Claim>> GetClaimsAsync()
        {
            return Task.FromResult(_claims);
        }

        public Task<Claim> GetClaimByIdAsync(string claimId)
        {
            var claim = _claims.FirstOrDefault(c => c.ClaimId == claimId);
            return Task.FromResult(claim);
        }

        public Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerName)
        {
            var claims = _claims.Where(c => c.LecturerName == lecturerName).ToList();
            return Task.FromResult(claims);
        }

        public Task<List<Claim>> GetPendingClaimsAsync()
        {
            var claims = _claims.Where(c => c.Status == "Pending").ToList();
            return Task.FromResult(claims);
        }

        public Task AddClaimAsync(Claim claim)
        {
            try
            {
                // Generate a unique claim ID
                _claimCounter++;
                claim.ClaimId = $"CLM{_claimCounter:000}";

                _claims.Add(claim);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error adding claim: {ex.Message}");
                throw; // Re-throw the exception
            }
        }

        public Task UpdateClaimAsync(Claim claim)
        {
            var existingClaim = _claims.FirstOrDefault(c => c.ClaimId == claim.ClaimId);
            if (existingClaim != null)
            {
                existingClaim.ContractName = claim.ContractName;
                existingClaim.Amount = claim.Amount;
                existingClaim.Status = claim.Status;
                existingClaim.Description = claim.Description;
            }
            return Task.CompletedTask;
        }

        public Task DeleteClaimAsync(string claimId)
        {
            var claim = _claims.FirstOrDefault(c => c.ClaimId == claimId);
            if (claim != null)
            {
                _claims.Remove(claim);
            }
            return Task.CompletedTask;
        }

        public Task<bool> CanRetractClaimAsync(string claimId, string lecturerName)
        {
            var claim = _claims.FirstOrDefault(c => c.ClaimId == claimId && c.LecturerName == lecturerName);
            if (claim == null) return Task.FromResult(false);

            // Can only retract if status is Pending and not yet reviewed
            var canRetract = claim.Status == "Pending" && string.IsNullOrEmpty(claim.ReviewedBy);
            return Task.FromResult(canRetract);
        }
    }
}