using System;
using System.Threading.Tasks;
using NIBSSForexValidationService.Models;
using NIBSSForexValidationService.Utilities;

namespace NIBSSForexValidationService.Interfaces
{
    public interface IAuthenticationAndAuthorisation
    {
        Task<LoginResponse> LoginAsync(LoginRequest loginRequest);

        Task<ResetModelResponse> ResetAsync(ResetModelRequest loginRequest);

        Task<Institution> GetInstitutionByInstitutionCodeAsync(string institutionCode);



        Task<ResponseObjectAndCryptyo<T>> PrepareRequest<T>(string EncryptedRequest, string InstitutionCode,string uniqueID);

        string PrepareResponse<T>(T clearResponse, string InstitutionCode, AES aes, string uniqueID);
    }

    public class ResponseObjectAndCryptyo<T>
    {
        public T requestObject { get; set; }
        public AES cryptography { get; set; }
        public long InstitutioID { get; set; }
    }
}
