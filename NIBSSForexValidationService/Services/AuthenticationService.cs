using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MySqlConnector;
using NIBSSForexValidationService.Interfaces;
using NIBSSForexValidationService.Models;
using NIBSSForexValidationService.Utilities;
using Serilog;

namespace NIBSSForexValidationService.Services
{
    public class AuthenticationService : IAuthenticationAndAuthorisation
    {


        public int? TIMEOUT { get; private set; } = 20;
        private readonly AppSettings _appSettings;
        private readonly ConnectionStrings _connectionStrings;

        public AuthenticationService(IOptions<AppSettings> appSettings, IOptions<ConnectionStrings> connectionstrings)
        {

            _appSettings = appSettings.Value;
            _connectionStrings = connectionstrings.Value;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            var LoginResponse = new LoginResponse { };
            string SelectDeviceSQL = "SELECT UI.tid,email,first_name,UI.flag,last_name, password,institution_id,code,institution_code,institution_name,nip_institution_code,aes_key,iv_spec FROM  forex_service.useridentity as UI INNER JOIN   forex_service.institution as I where UI.institution_id = I.TID and UI.email = @email";
            var updated = await QueryDatabase<UserDetails>(SelectDeviceSQL, new { @email = loginRequest.RegisteredEmailAddress }, IsStoredProcedure: false);
            if (updated.AsList().Count == 1)
            {
                var oneUser = updated.ToList().Single();




                var hashedPassWord = CalculatePasswordHash(oneUser.tid.ToString(), loginRequest.Password);

                if (hashedPassWord.Equals(oneUser.password, StringComparison.OrdinalIgnoreCase))
               // if (true)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

                    var claims = new Dictionary<string, object>();
                    //claims.Add("AESKEY", oneUser.aes_key);
                    //claims.Add("IVSPEC", oneUser.iv_spec);
                    claims.Add("INSTITUTIONCODE", oneUser.institution_code);

                    claims.Add("INSTITUTIONID", oneUser.institution_id);
                    claims.Add("INSTITUTIONNAME", oneUser.institution_name);

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                    new Claim(ClaimTypes.Name, oneUser.email.ToString())
                        }),
                        Expires = DateTime.UtcNow.AddYears(2),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                        Claims = claims
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    LoginResponse.Token = tokenHandler.WriteToken(token);

                    LoginResponse.ResponseCode = "00";
                    LoginResponse.ResponseDescription = "Successful";

                    return LoginResponse;
                }
                else
                {
                    LoginResponse.ResponseCode = "01";
                    LoginResponse.ResponseDescription = "Invalid Credentials";
                }




            }





            return LoginResponse;
        }

        public Task<ResetModelResponse> ResetAsync(ResetModelRequest loginRequest)
        {
            throw new NotImplementedException();
        }


        public string CalculatePasswordHash(string salt, string password)
        {
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            UTF8Encoding encoder = new UTF8Encoding();

            // Compute hash of the salt
            byte[] saltBytes = encoder.GetBytes(salt);
            byte[] saltHash = md5Provider.ComputeHash(saltBytes);

            // Get the password in bytes
            byte[] passwordBytes = encoder.GetBytes(password);

            // New byte array containing both password and salt hash
            byte[] saltedPassword = new byte[saltHash.Length + passwordBytes.Length];

            // Copy the contents of password byte array and saltHash byte array to saltedPassword
            passwordBytes.CopyTo(saltedPassword, 0);
            saltHash.CopyTo(saltedPassword, passwordBytes.Length);

            // Calculate the md5
            byte[] hashedBytes = md5Provider.ComputeHash(saltedPassword);
            return Convert.ToBase64String(hashedBytes);
        }









        //Stuff MSSQL

        public async Task<IEnumerable<T>> QueryDatabase<T>(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {

                using (var conn = new MySqlConnection(_connectionStrings.FXMySQLDBConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QueryAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    // Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

                    return list;
                }
            }
            catch (MySqlException ex)
            {

                // ReadSQLException(ex);
                Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Source, ex.Message, ex.Number);
                return default;
            }
            catch (Exception ex)
            {
                 Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return default;
            }
        }



        public async Task<T> QueryDatabaseSingle<T>(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {
                using (var conn = new MySqlConnection(_connectionStrings.FXMySQLDBConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QuerySingleAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                   

                    return list;
                }
            }
            catch (MySqlException ex)
            {
                // ReadSQLException(ex);

                Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Source, ex.Message, ex.Number);
                return default(T);
            }
            catch (Exception ex)
            {

                  Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return default(T);
            }
        }

        public async Task<int> ExecuteDatabaseSingle(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {
                using (var conn = new MySqlConnection(_connectionStrings.FXMySQLDBConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.ExecuteAsync(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

                    return list;
                }
            }
            catch (MySqlException ex)
            {
                //  ReadSQLException(ex);

                Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Source, ex.Message, ex.Number);
                return 0;
            }
            catch (Exception ex)
            {
               Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return 0;
            }
        }








        public async Task<IEnumerable<T>> MSQueryDatabase<T>(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {
                using (var conn = new SqlConnection(_connectionStrings.BVNMSSQLDbConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QueryAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    //    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

                    return list;
                }
            }
            catch (SqlException ex)
            {

                ReadSQLException(ex);
                  Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Server, ex.Message, ex.Procedure);
                return default;
            }
            catch (Exception ex)
            {
                 Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return default;
            }
        }



        public async Task<T> MSQueryDatabaseSingle<T>(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {
                using (var conn = new SqlConnection(_connectionStrings.BVNMSSQLDbConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QuerySingleAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    //    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

                    return list;
                }
            }
            catch (SqlException ex)
            {
                ReadSQLException(ex);

                  Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Server, ex.Message, ex.Procedure);
                return default(T);
            }
            catch (Exception ex)
            {

                Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return default(T);
            }
        }

        public async Task<int> MSExecuteDatabaseSingle(string SqlQuery, object objectParams, bool IsStoredProcedure)
        {

            try
            {
                using (var conn = new SqlConnection(_connectionStrings.BVNMSSQLDbConnectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.ExecuteAsync(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    //      Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

                    return list;
                }
            }
            catch (SqlException ex)
            {
                ReadSQLException(ex);

                   Log.Logger.Debug(ex, "{0} SQL SERVER threw an SQLEXCEPTION of  {1} in  {2} ", ex.Server, ex.Message, ex.Procedure);
                return 0;
            }
            catch (Exception ex)
            {
                   Log.Logger.Debug(ex, "{0} threw an EXCEPTION of  {1}  ", ex.Source, ex.Message);
                return 0;
            }
        }

        private void ReadSQLException(SqlException ex)
        {
            //   finalBasicResponse.Message = ex.Message;
            //  finalBasicResponse.ResponseCode = int.Parse(ex.Number.ToString());
        }

        public async Task<Institution> GetInstitutionByInstitutionCodeAsync(string institutionCode)
        {

            string SelectDeviceSQL = "SELECT TID,code,institution_code,institution_name,flag,aes_key,iv_spec FROM forex_service.institution where institution_code = @institution_code;";
            var updated = await QueryDatabase<Institution>(SelectDeviceSQL, new { @institution_code = institutionCode }, IsStoredProcedure: false);
            if (updated?.AsList()?.Count == 1)
            {
                return updated.ToList().Single();
            }
            else
            {
                return default;
            }
        }
        string AESKEYS = string.Empty;
        string IVSPEC = string.Empty;

        public async Task<ResponseObjectAndCryptyo<T>> PrepareRequest<T>(string EncryptedRequest, string InstitutionCode, string uniqueID)
        {
            var institutionResponse = await GetInstitutionByInstitutionCodeAsync(InstitutionCode);
            
            AESKEYS = institutionResponse?.aes_key;
            IVSPEC = institutionResponse?.iv_spec;

            if (!string.IsNullOrWhiteSpace(AESKEYS) && !string.IsNullOrWhiteSpace(IVSPEC))
            {
                AES cryptography = new AES(AESKEYS, IVSPEC);

                var clearRequest = cryptography.Decrypt(EncryptedRequest);
                Log.Logger.Debug($"{uniqueID} request is {clearRequest}");
                var request = new ResponseObjectAndCryptyo<T> { cryptography = cryptography, requestObject = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(clearRequest), InstitutioID = institutionResponse.TID };

                return request;
            }
            else
            {
                return default;
            }


        }

        public  string PrepareResponse<T>(T EncryptedRequest, string InstitutionCode, AES aes, string uniqueID)
        {
            
            var serialisedResponse = Newtonsoft.Json.JsonConvert.SerializeObject(EncryptedRequest);
            Log.Logger.Debug($"{uniqueID} response is {serialisedResponse}");
            AES cryptography = aes;
            var encryptedResponse = cryptography.Encrypt(serialisedResponse);
            return encryptedResponse;

        }

    }
}
