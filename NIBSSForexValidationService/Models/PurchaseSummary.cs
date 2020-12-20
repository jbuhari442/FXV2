using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NIBSSForexValidationService.Models
{
    public class PurchaseSummary
    {
        public PurchaseSummary()
        {
        }
        public int TransactionType { get; set; }
        public string TransactionTypeName { get; set; }
        public decimal TotalPurchase { get; set; }
        public decimal ApplicantsBalance { get; set; }
        public decimal cbnLimit { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal LastPurchaseAmount { get; set; }
        public int Quarter { get; set; }
        public int Year { get; set; }
    }

    public class PurchaseSummaryRequest: BVNApplicant
    {
        [Required]
        [StringLength(10)]
        public string bvnApplicantAccountNumber { get; set; }
    }

    public class PurchaseSummaryResponse : BVNValidationDetails
    {

        public IEnumerable<PurchaseSummary> PurchaseSummaries { get; set; } = new List<PurchaseSummary>();
    }

}
