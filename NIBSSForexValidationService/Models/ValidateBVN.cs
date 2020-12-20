using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NIBSSForexValidationService.Models.CustomValidators;

namespace NIBSSForexValidationService.Models
{
    public class ValidateBVN
    {
        public ValidateBVN()
        {
        }
    }

    public class ValidateBVNRequest : BVNApplicant
    {
        public string bvnBeneficiary { get; set; }

    }

    public class ValidateBVNResponse : BasicResponse
    {
        public BVNValidationDetails bvnBeneficiary { get; set; }

        public BVNValidationDetails bvnApplicant { get; set; }

        //public ForexLimitDetails PersonalTravelAllowance { get; set; }
        //public ForexLimitDetails BusinessTravelAllowance { get; set; }

        //public ForexLimitDetails SchoolFeesAllowance { get; set; }
        //public ForexLimitDetails MedicalFeesAllowance { get; set; }

        public IEnumerable<PurchaseSummary> bvnApplicantFXDetails { get; set; }

    }

    public class BVNValidationDetails 
    {
        public string bvn { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string bvnStatus { get; set; }
        public string bvnStatusDetails { get; set; }


    }
    //public class ForexLimitDetails
    //{
    //    public string LimitType { get; set; }
    //    public decimal ApplicantsBalance { get; set; }
    //    public decimal ApplicantsTotal { get; set; }
    //    public DateTime StartLimit { get; set; }
    //    public DateTime EndLimit { get; set; }
    //}

}
