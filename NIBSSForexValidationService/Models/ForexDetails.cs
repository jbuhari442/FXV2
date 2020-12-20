using System;
using System.ComponentModel.DataAnnotations;

namespace NIBSSForexValidationService.Models
{
    public class ForexDetails
    {
        public ForexDetails()
        {
        }
    }


    public class ForexDetailsRequest : ValidateBVNRequest
    {
        [Required]
        [StringLength(10)]
        public string bvnApplicantAccount { get; set; }

        [Required]
        [Range(1, 5)]
        public int TransactionType { get; set; }

        [Required]
        [Range(1, 2)]
        public int PurchaseType { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(100)]
        public string Purpose { get; set; }

        [Required]
        public decimal Amount { get; set; }
        [Required]
        public decimal Rate { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }
        [Required]
        [StringLength(30)]
        public string RequestID { get; set; }
        [Required]
        [StringLength(20)]
        public string PassportNumber { get; set; }

        public string TaxCertificationNumber { get; set; }

    }

    public class DeleteForexRequest : BVNApplicant
    {
        [Required]
        [StringLength(30)]
        public string RequestID { get; set; }
        [Required]
        public string ResponseFXID { get; set; }
    }

    public class ForexResponse : BasicResponse
    {
        [Required]
        [StringLength(30)]
        public string RequestID { get; set; }

        
        public long ResponseFXID { get; set; }

        public string bvnApplicant { get; set; }
    }

}
