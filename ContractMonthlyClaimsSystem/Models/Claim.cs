using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimsSystem.Models
{
    public class Claim
    {
        public string ClaimId { get; set; } = string.Empty;

        [Required]
        public string ContractName { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; }

        public decimal Amount { get; set; }

        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }

        public string LecturerName { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
        public string Description { get; set; } = string.Empty;

        public string ReviewedBy { get; set; } = string.Empty;
        public DateTime? ReviewedDate { get; set; }
        public string ReviewNotes { get; set; } = string.Empty;

        public string FormattedAmount => $"R {Amount:N2}";
        public string FormattedHours => $"{HoursWorked:N1} hours";
        public string FormattedHourlyRate => $"R {HourlyRate:N2}/hour";
        public string FormattedDate => SubmittedDate.ToString("dd/MM/yyyy");
        public string FormattedReviewedDate => ReviewedDate?.ToString("dd/MM/yyyy HH:mm") ?? "Not reviewed";

        public string StatusBadgeClass => Status.ToLower() switch
        {
            "pending" => "bg-warning",
            "approved" => "bg-success",
            "rejected" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}