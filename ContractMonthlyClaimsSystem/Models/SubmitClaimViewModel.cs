using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Please select a contract")]
        [Display(Name = "Contract Name")]
        public string ContractName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.1, 744, ErrorMessage = "Hours must be between 0.1 and 744 (monthly maximum)")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(50, 1000, ErrorMessage = "Hourly rate must be between R50 and R1000")]
        [Display(Name = "Hourly Rate (R)")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total Amount (R)")]
        public decimal CalculatedAmount { get; set; }

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        // Validation method for business rules
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var amount = HoursWorked * HourlyRate;

            if (amount > 50000)
            {
                yield return new ValidationResult(
                    "Claim amount cannot exceed R50,000 per submission. Please split large claims into multiple submissions.",
                    new[] { nameof(HoursWorked), nameof(HourlyRate) });
            }

            if (amount < 10)
            {
                yield return new ValidationResult(
                    "Claim amount must be at least R10.",
                    new[] { nameof(HoursWorked), nameof(HourlyRate) });
            }
        }
    }
}