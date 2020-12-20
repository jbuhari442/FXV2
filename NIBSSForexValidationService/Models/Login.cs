using System;
using System.ComponentModel.DataAnnotations;

namespace NIBSSForexValidationService.Models
{
    public class Login
    {
        public Login()
        {
        }
    }

    public class LoginRequest
    {
        [EmailAddress]
        public string RegisteredEmailAddress { get; set; }

        public string Password { get; set; }
    }

    public class LoginResponse:BasicResponse
    {
        public string Token { get; set; }
    }


    public class UserDetails
    {
        public int tid { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string flag { get; set; }
        public string last_name { get; set; }
        public string password { get; set; }
        public int institution_id { get; set; }
        public string code { get; set; }
        public string institution_code { get; set; }
        public string institution_name { get; set; }
        public string nip_institution_code { get; set; }
        public string aes_key { get; set; }
        public string iv_spec { get; set; }
    }


    public class Institution
    {
        public int TID { get; set; }
        public string code { get; set; }
        public string institution_code { get; set; }
        public string institution_name { get; set; }
        public string flag { get; set; }
        public string aes_key { get; set; }
        public string iv_spec { get; set; }
    }




}
