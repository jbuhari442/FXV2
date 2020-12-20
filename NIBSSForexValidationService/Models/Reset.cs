using System;
using System.ComponentModel.DataAnnotations;
namespace NIBSSForexValidationService.Models
{
    public class ResetModel
    {
        public ResetModel()
        {
        }

    }

    public class ResetModelRequest
    {
        [EmailAddress]
        public string RegisteredEmailAddress { get; set; }
    }

    public class ResetModelResponse:BasicResponse
    {
        //[Range(5,50,true,"must be between 5 and 50 cedes","",null,true)]
        [Range(5, 50)]
        [EmailAddress]
        public string RegisteredEmailAddress { get; set; }

        //Add Institution Code to response Email
    }
}
