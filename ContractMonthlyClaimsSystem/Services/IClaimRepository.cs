using ContractMonthlyClaimsSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Services
{
    public interface IClaimRepository
    {
        Task<List<Claim>> GetClaimsAsync();
        Task<Claim> GetClaimByIdAsync(string claimId);
        Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerName);
        Task AddClaimAsync(Claim claim);
        Task UpdateClaimAsync(Claim claim);
        Task DeleteClaimAsync(string claimId);
        Task<List<Claim>> GetPendingClaimsAsync();
    }
}