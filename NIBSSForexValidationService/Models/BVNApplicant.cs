using System;
using NIBSSForexValidationService.Models.CustomValidators;

namespace NIBSSForexValidationService.Models
{
    public class BVNApplicant
    {
        [BVNValid]
        public string bvnApplicant { get; set; }
    }
}
