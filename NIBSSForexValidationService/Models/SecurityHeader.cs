using System;
using System.ComponentModel.DataAnnotations;

namespace NIBSSForexValidationService.Models
{
    public class SecurityHeader
    {
        public SecurityHeader()
        {
        }

        [Required]
        [StringLength(maximumLength: 6, ErrorMessage ="Institution Code must be six digits",ErrorMessageResourceName = null,ErrorMessageResourceType = null,MinimumLength = 6)]
        public string InstitutionCode { get; set; }

        [Required]
        [StringLength(maximumLength: 14, ErrorMessage = "Institution Code must be fourteen digits", ErrorMessageResourceName = null, ErrorMessageResourceType = null, MinimumLength = 14)]
        public string Timestamp { get; set; }
    }
}
