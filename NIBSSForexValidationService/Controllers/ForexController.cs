using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NIBSSForexValidationService.Interfaces;
using NIBSSForexValidationService.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NIBSSForexValidationService.Controllers
{

    /// <summary>
    /// This houses all the methods for forex transactions
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    public class ForexController : Controller
    {
        private readonly IFXTransaction iFXTransaction;
        private readonly IAuthenticationAndAuthorisation iAuthenticationAndAuthorisation;
        
        public ForexController(IFXTransaction fXTransaction, IAuthenticationAndAuthorisation authenticationAndAuthorisation)
        {
            iFXTransaction = fXTransaction;
            iAuthenticationAndAuthorisation = authenticationAndAuthorisation;
        }
        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            //  var currentUser = HttpContext.User;
            //  22222222222
            //    bvnBeneficiary = "22222222491"

            //22222222488
           var result = await iFXTransaction.ValidateBVNAsync(new Models.ValidateBVNRequest { bvnApplicant = "22222222488", bvnBeneficiary = "22222222491" });
            // await iFXTransaction.AddFXPurchaseAsync(new Models.ForexDetailsRequest());

            if (result?.bvnApplicant.bvnStatus != "00")
            {



            }
            return Ok(result);
            //return StatusCode(456, new ValidateBVNResponse {  bvnApplicant = new BVNValidationDetails { bvn = "566777" } });
           // return new string[] { "value1", "value2" };
        }











        /// <summary>
        /// This is used to validate BVN sent in the request only successful when the applicant bvn is successful (accept text)
        /// </summary>
        /// <returns></returns>
        [HttpPost("validatebvn")]
        [Consumes("text/plain")]
        public async Task<IActionResult> ValidateBVN([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode)
        {
            string encryptedValidateBVNRequest;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                encryptedValidateBVNRequest = await reader.ReadToEndAsync();
            }

            return await ProcessValidateBVN(encryptedValidateBVNRequest, institutionCode);
        }

        /// <summary>
        /// This is used to validate BVN sent in the request only successful when the applicant bvn is successful (accepts json)
        /// </summary>
        /// <returns></returns>
        [HttpPost("validatebvn")]
        [Produces("application/json")]
        public async Task<IActionResult> ValidateBVN([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode, [FromBody] string encryptedValidateBVNRequest)
        {

            return await ProcessValidateBVN(encryptedValidateBVNRequest, institutionCode);

        }


        /// <summary>
        /// This is used to process the validation request, it decrypts, processes and encrypts
        /// </summary>
        /// <param name="encryptedValidateBVNRequest"></param>
        /// <returns></returns>
        private async Task<IActionResult> ProcessValidateBVN(string encryptedValidateBVNRequest, string institutionCode)
        {
            int statusCode = 200;
           // var institutionCode = HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value;
            string RequestID = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(institutionCode))
            {
                return BadRequest(new { message = "There is no institution Code in the header of your request" });
            }

            var validateBVNRequest = await iAuthenticationAndAuthorisation.PrepareRequest<ValidateBVNRequest>(encryptedValidateBVNRequest, institutionCode,RequestID);

            if (validateBVNRequest?.requestObject == null)
            {
                statusCode = 406;
            }


            //TryValidateModel(validateBVNRequest.requestObject);

            //if (!ModelState.IsValid)
            //{
            //    var encryptedResponsej = iAuthenticationAndAuthorisation.PrepareResponse(ModelState.Values.Select(a => a.Errors), institutionCode, validateBVNRequest.cryptography,RequestID);
            //    return BadRequest(encryptedResponsej);
            //}



            var result = await iFXTransaction.ValidateBVNAsync(validateBVNRequest.requestObject);
            
            if (result?.bvnApplicant.bvnStatus=="00")
            {
                
            }


            var encryptedResponse = iAuthenticationAndAuthorisation.PrepareResponse(result, institutionCode, validateBVNRequest.cryptography,RequestID);

            if (encryptedResponse == null)
                return BadRequest(new { message = "invalid" });


            return StatusCode(statusCode, encryptedResponse);
            //return Problem("bad", "", 456, "bad title", "");
            //return Ok(encryptedResponse);


        }






        [AllowAnonymous]
        [HttpPost("authenticate")]
        [Consumes("text/plain")]
        public async Task<IActionResult> Authenticate([FromHeader(Name = "InstitutionCode")][Required] string institutionCode)
        {
            string encryptedLoginRequest;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                encryptedLoginRequest = await reader.ReadToEndAsync();
            }

            return await ProcessLogin(encryptedLoginRequest,institutionCode);
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        [Produces("application/json")]
        public async Task<IActionResult> Authenticate([FromHeader(Name = "InstitutionCode")][Required] string institutionCode,[FromBody] string encryptedLoginRequest)
        {
           // Response.Headers.Add("jima","jima");
            return await ProcessLogin(encryptedLoginRequest, institutionCode);

        }


        /// <summary>
        /// This is used to process the login request, it decrypts, processes and encrypts
        /// </summary>
        /// <param name="encryptedLoginRequest"></param>
        /// <returns></returns>
        private async Task<IActionResult> ProcessLogin(string encryptedLoginRequest,string institutionCode)
        {
          //  var institutionCode = HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value;
            string RequestID = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(institutionCode))
            {
                return BadRequest(new { message = "There is no institution Code in the header of your request" });
            }

            var loginRequest = await iAuthenticationAndAuthorisation.PrepareRequest<LoginRequest>(encryptedLoginRequest, institutionCode, RequestID);

            TryValidateModel(loginRequest.requestObject);

            if (!ModelState.IsValid)
            {
                var encryptedResponsej = iAuthenticationAndAuthorisation.PrepareResponse(ModelState.Values.Select(a=>a.Errors), institutionCode, loginRequest.cryptography, RequestID);
                return BadRequest(encryptedResponsej);
            }


            var user = await iAuthenticationAndAuthorisation.LoginAsync(loginRequest.requestObject);

            var encryptedResponse = iAuthenticationAndAuthorisation.PrepareResponse(user, institutionCode,loginRequest.cryptography, RequestID);

            if (encryptedResponse == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(encryptedResponse);
        }









        /// <summary>
        /// Used to add log fx purchase  requests using plain text
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>

        [HttpPost("logFXPurchase")]
        [Consumes("text/plain")]
        public async Task<IActionResult> LogFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode)
        {
            string encryptedLogPurchaseRequest;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                encryptedLogPurchaseRequest = await reader.ReadToEndAsync();
            }

            return await ProcessLogFXPurchase(encryptedLogPurchaseRequest, institutionCode);
        }

        /// <summary>
        /// Used to add log fx purchase  requests using plain text
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        [HttpPost("logFXPurchase")]
        [Produces("application/json")]
        public async Task<IActionResult> LogFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode,[FromBody] string encryptedLogPurchaseRequest)
        {

            return await ProcessLogFXPurchase(encryptedLogPurchaseRequest, institutionCode);

        }
        /// <summary>
        /// method to process FX purchase
        /// </summary>
        /// <param name="encryptedLogPurchaseRequest"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        private async Task<IActionResult> ProcessLogFXPurchase(string encryptedLogPurchaseRequest, string institutionCode)
        {
          //  var institutionCode = HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value;
            string RequestID = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(institutionCode))
            {
                return BadRequest(new { message = "There is no institution Code in the header of your request" });
            }

            var logPurchaseResponse = await iAuthenticationAndAuthorisation.PrepareRequest<ForexDetailsRequest>(encryptedLogPurchaseRequest, institutionCode,RequestID);



            TryValidateModel(logPurchaseResponse.requestObject);

            if (!ModelState.IsValid)
            {
                var encryptedResponsej = iAuthenticationAndAuthorisation.PrepareResponse(ModelState.Values.Select(a => a.Errors), institutionCode, logPurchaseResponse.cryptography,RequestID);
                return BadRequest(encryptedResponsej);
            }


            var FxPurchaseResponse = await iFXTransaction.AddFXPurchaseAsync(logPurchaseResponse.requestObject, logPurchaseResponse.InstitutioID);

            var encryptedResponse = iAuthenticationAndAuthorisation.PrepareResponse(FxPurchaseResponse, institutionCode, logPurchaseResponse.cryptography,RequestID);

            if (encryptedResponse == null)
                return BadRequest(new { message = "" });

            return Ok(encryptedResponse);
        }




       /// <summary>
       /// Used to remove fx purchase accepts plain text
       /// </summary>
       /// <param name="bearerToken"></param>
       /// <param name="institutionCode"></param>
       /// <returns></returns>
        [HttpPost("removeFXPurchase")]
        [Consumes("text/plain")]
        public async Task<IActionResult> RemoveLogFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode)
        {
            string encryptedRemoveLogPurchaseRequest;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                encryptedRemoveLogPurchaseRequest = await reader.ReadToEndAsync();
            }

            return await ProcessRemoveFXPurchase(encryptedRemoveLogPurchaseRequest, institutionCode);
        }


        /// <summary>
        /// Used to remove fx purchase accepts json object
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        [HttpPost("removeFXPurchase")]
        [Produces("application/json")]
        public async Task<IActionResult> RemoveLogFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode,[FromBody] string encryptedLogPurchaseRequest)
        {

            return await ProcessRemoveFXPurchase(encryptedLogPurchaseRequest, institutionCode);

        }

        /// <summary>
        /// method to remove fx transfers
        /// </summary>
        /// <param name="encryptedLogPurchaseRequest"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        private async Task<IActionResult> ProcessRemoveFXPurchase(string encryptedLogPurchaseRequest, string institutionCode)
        {
            //var institutionCode = HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value;
            string RequestID = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(institutionCode))
            {
                return BadRequest(new { message = "There is no institution Code in the header of your request" });
            }

            var logPurchaseResponse = await iAuthenticationAndAuthorisation.PrepareRequest<DeleteForexRequest>(encryptedLogPurchaseRequest, institutionCode,RequestID);



            TryValidateModel(logPurchaseResponse.requestObject);

            if (!ModelState.IsValid)
            {
                var encryptedResponsej = iAuthenticationAndAuthorisation.PrepareResponse(ModelState.Values.Select(a => a.Errors), institutionCode, logPurchaseResponse.cryptography, RequestID);
                return BadRequest(encryptedResponsej);
            }


            var FxPurchaseResponse = await iFXTransaction.RemoveFXPurchaseAsync(logPurchaseResponse.requestObject);

            var encryptedResponse = iAuthenticationAndAuthorisation.PrepareResponse(FxPurchaseResponse, institutionCode, logPurchaseResponse.cryptography,RequestID);

            if (encryptedResponse == null)
                return BadRequest(new { message = "" });

            return Ok(encryptedResponse);
        }








        /// <summary>
        /// used to log fx purchase in bulk accepts plain text
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        [HttpPost("logBulkFXPurchase")]
        [Consumes("text/plain")]
        [Produces("text/plain")]
        public async Task<IActionResult> LogBulkFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode)
        {
            string encryptedLogPurchaseRequest;

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                encryptedLogPurchaseRequest = await reader.ReadToEndAsync();
            }

            return await ProcessLogBulkFXPurchase(encryptedLogPurchaseRequest, institutionCode);
        }


        /// <summary>
        /// used to log fx purchase in bulk accepts json object
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        [HttpPost("logBulkFXPurchase")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<IActionResult> LogBulkFXPurchase([FromHeader(Name = "Authorization")][Required] string bearerToken,[FromHeader(Name = "InstitutionCode")][Required] string institutionCode,[FromBody] string encryptedLogPurchaseRequest)
        {

            return await ProcessLogBulkFXPurchase(encryptedLogPurchaseRequest, institutionCode);

        }

        /// <summary>
        /// method to validate and process bulk fx purchase
        /// </summary>
        /// <param name="encryptedLogPurchaseRequest"></param>
        /// <param name="institutionCode"></param>
        /// <returns></returns>
        private async Task<IActionResult> ProcessLogBulkFXPurchase(string encryptedLogPurchaseRequest,string institutionCode)
        {
            //var institutionCode = HttpContext.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value;
            string RequestID = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(institutionCode))
            {
                return BadRequest(new { message = "There is no institution Code in the header of your request" });
            }

            var logPurchaseResponse = await iAuthenticationAndAuthorisation.PrepareRequest<IEnumerable<ForexDetailsRequest>>(encryptedLogPurchaseRequest, institutionCode,RequestID);


            foreach (var individuallogPurchaseResponseobject in logPurchaseResponse.requestObject)
            {
                TryValidateModel(individuallogPurchaseResponseobject);

                if (!ModelState.IsValid)
                {
                    var encryptedResponsej = iAuthenticationAndAuthorisation.PrepareResponse(ModelState.Values.Select(a => a.Errors), institutionCode, logPurchaseResponse.cryptography, RequestID);
                    return BadRequest(encryptedResponsej);
                }
            }



            var FxPurchaseResponse = await iFXTransaction.AddBulkFXPurchaseAsync(logPurchaseResponse.requestObject, logPurchaseResponse.InstitutioID);
            

            var encryptedResponse = iAuthenticationAndAuthorisation.PrepareResponse(FxPurchaseResponse, institutionCode, logPurchaseResponse.cryptography,RequestID);

            if (encryptedResponse == null)
                return BadRequest(new { message = "" });

            return Ok(encryptedResponse);
        }


    }
}
