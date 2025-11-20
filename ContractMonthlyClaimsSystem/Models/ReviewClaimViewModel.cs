using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class ReviewClaimViewModel
    {
        public Claim Claim { get; set; } = new Claim();

        [Display(Name = "Review Notes")]
        [StringLength(500, ErrorMessage = "Review notes cannot exceed 500 characters")]
        public string ReviewNotes { get; set; } = string.Empty;

    }
}
