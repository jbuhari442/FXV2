using System;
using System.ComponentModel.DataAnnotations;
using NIBSSForexValidationService.Utilities.ValidationUtilities;

namespace NIBSSForexValidationService.Models.CustomValidators
{
    public class BVNValidAttribute : ValidationAttribute
    {

        //public override bool IsValid(object value)
        //{
        //    var bvn = value as string;
        //    return BVNVerifier.VerifyBVN(bvn);
        //}

        //public override string FormatErrorMessage(string name)
        //{
        //    return base.FormatErrorMessage(name);
        //}

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var bvn = value as string;
            if (BVNVerifier.VerifyBVN(bvn))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult($"{bvn} is invalid.");
            }
            
        }
    }
}
