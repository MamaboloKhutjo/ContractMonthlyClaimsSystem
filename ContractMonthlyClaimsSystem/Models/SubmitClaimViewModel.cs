using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Please select a contract")]
        [Display(Name = "Contract Name")]
        public string ContractName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [Display(Name = "Amount (R)")]
        public decimal Amount { get; set; }

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
    }
}