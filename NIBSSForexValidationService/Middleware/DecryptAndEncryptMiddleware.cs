using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using NIBSSForexValidationService.Interfaces;
using NIBSSForexValidationService.Utilities;

namespace NIBSSForexValidationService.Middleware
{
    public class DecryptAndEncryptMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IAuthenticationAndAuthorisation iAuthenticationAndAuthorisation;

        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public DecryptAndEncryptMiddleware(RequestDelegate next, IAuthenticationAndAuthorisation authenticationAndAuthorisation)
        {
            _next = next;
            iAuthenticationAndAuthorisation = authenticationAndAuthorisation;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        }

        public async Task Invoke(HttpContext context)
        {
            
            
            var hasinstitutionCode =  int.TryParse( context.Request.Headers.FirstOrDefault(x => x.Key == "InstitutionCode").Value ,out int institutionCode);
           
            //Get AESKeys
            var currentUser = context.User;

            var AESKEYS = string.Empty;
            var IVSPEC = string.Empty;

            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                //Get Keys from Claims
                if (currentUser.HasClaim(c => c.Type == "AESKEY"))
                {
                    AESKEYS = (currentUser.Claims.FirstOrDefault(c => c.Type == "AESKEY").Value);

                }
                if (currentUser.HasClaim(c => c.Type == "IVSPEC"))
                {
                    IVSPEC = (currentUser.Claims.FirstOrDefault(c => c.Type == "IVSPEC").Value);

                }
            }

            if (string.IsNullOrWhiteSpace(AESKEYS) && hasinstitutionCode) {
                var institutionResponse = await iAuthenticationAndAuthorisation.GetInstitutionByInstitutionCodeAsync(institutionCode.ToString());
                AESKEYS = institutionResponse?.aes_key;
                IVSPEC = institutionResponse?.iv_spec;
            }


            AES cryptography = null;


            if (!string.IsNullOrWhiteSpace(AESKEYS))
            {
                cryptography = new AES(AESKEYS, IVSPEC);

                          //get Content and decrypt
                var request = context.Request;

                var requestBodyStream = request.Body;
                //get Encrypted Body
                var encryptedBody = await new StreamReader(requestBodyStream).ReadToEndAsync();

                //Decrypt Encrypted Body

                var clearRequest = cryptography.Decrypt(encryptedBody);

                //pass back to request like nothing happened

                var clearRequestByte = Encoding.UTF8.GetBytes(clearRequest);

                requestBodyStream = new MemoryStream(clearRequestByte);

                request.Body = requestBodyStream;
            }



            var originalBodyStream = context.Response.Body;

            // await using var responseBody = _recyclableMemoryStreamManager.GetStream();

            // context.Response.Body = responseBody;

            var BodyStream = new MemoryStream();

            context.Response.Body = BodyStream;


            await _next(context);

            if (!string.IsNullOrWhiteSpace(AESKEYS))
            {
                //Response Encryprt with AESKEYS and IV

                context.Response.Body = new MemoryStream();

                BodyStream.Seek(0, SeekOrigin.Begin);
                var clearResponseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                //context.Response.Body.Seek(0, SeekOrigin.Begin);


                var encryptedResponse = cryptography.Encrypt(clearResponseBody);

                //replace clear response with encrypted response

                var encryptedResponseByte = Encoding.UTF8.GetBytes(encryptedResponse);

                //originalBodyStream = new MemoryStream(encryptedResponseByte);

                var newcontent = new StreamReader(BodyStream).ReadToEnd();
                BodyStream.Write(encryptedResponseByte);
                context.Response.ContentType="text/plain";
               // context.Response.ContentLength = encryptedResponse.Length;
                newcontent = newcontent + encryptedResponse;
                // var test = new MemoryStream(encryptedResponseByte);

                // context.Response.Body.Seek(0, SeekOrigin.Begin);

             //   await context.Response.WriteAsync(BodyStream);
               // context.Response.Body.Seek(0, SeekOrigin.Begin);
            }

        }
    }
}
