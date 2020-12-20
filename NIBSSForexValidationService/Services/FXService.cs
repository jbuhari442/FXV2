using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using NIBSSForexValidationService.Interfaces;
using NIBSSForexValidationService.Models;
using NIBSSForexValidationService.Utilities.ValidationUtilities;
using Serilog;

namespace NIBSSForexValidationService.Services
{
    public class FXTransactionService : IFXTransaction
    {
        private const string SUCCESSFULCODE = "00";
        private const string FAILURECODE = "96";
        private const string SUCCESSFULMESSAGE = "Successful";
        string ResponseCode = FAILURECODE;
        string ResponseMessage = "ERROR PROCESSING REQUEST";
        public BasicResponse basicResponse = new BasicResponse();
        private readonly ConnectionStrings _connectionStrings;
        public int? TIMEOUT { get; private set; } = 20;

        public FXTransactionService(IOptions<ConnectionStrings> connectionstrings)
        {
            _connectionStrings = connectionstrings.Value;
        }

        public async Task<ValidateBVNResponse> ValidateBVNAsync(ValidateBVNRequest validateBVNRequest)
        {

            var bvnDetails = GetBVNDetails(validateBVNRequest);
            var FXDetails = GetFXDetails(validateBVNRequest);

            Task[] taskDetailsList = { bvnDetails, FXDetails };

            var getDetailsTasks = Task.WhenAll(taskDetailsList);

            await getDetailsTasks;

            if (getDetailsTasks.IsCompletedSuccessfully)
            {
                var bvnresult = bvnDetails.Result;
                var fxresult = FXDetails.Result;


                var bvnapplicant = bvnresult?.FirstOrDefault(a => a.bvn == validateBVNRequest.bvnApplicant);
                var bvnbeneficiary = bvnresult?.FirstOrDefault(a => a.bvn == validateBVNRequest.bvnBeneficiary);

                if (bvnapplicant?.bvnStatus == SUCCESSFULCODE)
                {
                    ResponseCode = SUCCESSFULCODE;
                    ResponseMessage = SUCCESSFULMESSAGE;
                }

                return new ValidateBVNResponse { ResponseCode = ResponseCode, ResponseDescription = ResponseMessage, bvnApplicant = bvnapplicant, bvnApplicantFXDetails = fxresult, bvnBeneficiary = bvnbeneficiary };
            }

            return null;

        }

        private async Task<IEnumerable<PurchaseSummary>> GetFXDetails(ValidateBVNRequest validateBVNRequest)
        {
            string SelectSQL = "GetCurrentFXBalanceDetails";
            var updated = await MSQueryDatabaseMultipleReturnSet<PurchaseSummary>(SelectSQL, new { @bvn_applicant = validateBVNRequest.bvnApplicant }, true, _connectionStrings.FXMSSQLDbConnectionString);

            if (updated?.Count() > 0)
            {
                return updated;
                //await AddEventAsync(userID, enable ? "APPROVE DEVICE" : "UNAPPROVE DEVICE", new { deviceID, userID, enable }, new { deviceID, userID, enable = !enable });
            }

            return default;
        }

        private async Task<IEnumerable<BVNValidationDetails>> GetBVNDetails(ValidateBVNRequest validateBVNRequest)
        {
            //    static enum ResponseCodes
            //{
            //    SUCCESS("00", "BVN record was found"),
            //NO_EXISTENT_BVN("01", "BVN does not exist"),
            //INVALID_BVN("02", "Invalid BVN"),
            //MAX_NUMBER_OF_BVN_EXCEEDED("03", "Maximum number of BVN's exceeded. Maximumm number is 200."),
            //INVALID_IP("04", "Invalid IP Address"),
            //INVALID_REQUEST("05", "Invalid Request"),
            //SERVER_ERROR("06", "Server Error. Please contact your System Administrator"),
            //WATCHLISTED_CUSTOMER("07", "Watchlisted Customer. Please contact your Risk Personnel");

            string SelectDeviceSQL = "select a.bvn bvn, a.first_name FirstName, a.middle_name MiddleName, a.surname LastName,a.watchlisted bvnStatus from demography a where a.bvn in (@bvnApplicant,@bvnBeneficiary)";
            var result = await MSQueryDatabase<BVNValidationDetails>(SelectDeviceSQL, new { @bvnApplicant = validateBVNRequest.bvnApplicant, @bvnBeneficiary = validateBVNRequest.bvnBeneficiary }, IsStoredProcedure: false, _connectionStrings.BVNMSSQLDbConnectionString);

            if (result == default)
            {
                result?.DefaultIfEmpty()
                .ToList()
                .ForEach(a => { a.bvnStatus = "01"; a.bvnStatusDetails = "BVN does not exist"; });
            }

            if (result?.Count() > 0)
            {
                var Goodbvn =
                    result
                        .Where(a => a.bvnStatus != "902" && a.bvnStatus != "1");

                Goodbvn
                    .ToList()
                    .ForEach(a => { a.bvnStatus = SUCCESSFULCODE; a.bvnStatusDetails = "BVN record was found"; });

                result.Except(Goodbvn)
                    .ToList()
                    .ForEach(a => { a.bvnStatus = "05"; a.bvnStatusDetails = "Watchlisted Customer. Please contact your Risk Personnel"; });
            }


            return result;
        }


        public async Task<ForexResponse> AddFXPurchaseAsync(ForexDetailsRequest forexDetailsRequest, long institutionId)
        {

            string UpdateDeviceSQL = "InsertSingleFXPurchase";
            var updated = await MSExecuteDatabaseSingle(UpdateDeviceSQL, new
            {
                @amount = forexDetailsRequest.Amount,
                @bvn_applicant = forexDetailsRequest.bvnApplicant,
                @bvn_applicant_acct = forexDetailsRequest.bvnApplicantAccount,
                @bvn_beneficiary = forexDetailsRequest.bvnBeneficiary,
                @passport_no = forexDetailsRequest.PassportNumber,
                @purpose = forexDetailsRequest.Purpose,
                @rate = forexDetailsRequest.Rate,
                @request_date = forexDetailsRequest.RequestDate,
                @request_id = forexDetailsRequest.RequestID,
                @tax_clearance_cert_no = forexDetailsRequest.TaxCertificationNumber,
                @institution = institutionId,
                @transaction_type = forexDetailsRequest.TransactionType,
                @Purchase_type = forexDetailsRequest.PurchaseType,
                @DateofBirth = forexDetailsRequest.DateOfBirth,
                @OperationType = "ADD"
            }, true, _connectionStrings.FXMSSQLDbConnectionString);

            if (updated != null && long.Parse(updated.ToString()) > 0)
            {

                return new ForexResponse { ResponseCode = SUCCESSFULCODE, ResponseDescription= SUCCESSFULMESSAGE, bvnApplicant = forexDetailsRequest.bvnApplicant,  RequestID = forexDetailsRequest.RequestID, ResponseFXID = long.Parse(updated.ToString()) };

            }

            return default;


        }

        public async Task<ForexResponse> RemoveFXPurchaseAsync(DeleteForexRequest deleteForexRequest)
        {

            string UpdateDeviceSQL = "RemoveSingleFXPurchase";
            var updated = await MSExecuteDatabaseSingle(UpdateDeviceSQL, new
            {
                @RequestID = deleteForexRequest.RequestID,
                @ResponseFXID = deleteForexRequest.ResponseFXID
            }, true, _connectionStrings.FXMSSQLDbConnectionString);

            if (updated != null && long.Parse(updated.ToString()) > 0)
            {

                return new ForexResponse {  ResponseCode = SUCCESSFULCODE, ResponseDescription = SUCCESSFULMESSAGE,bvnApplicant = deleteForexRequest.bvnApplicant,  RequestID = deleteForexRequest.RequestID, ResponseFXID = long.Parse(updated.ToString()) };

            }

            return default;
        }

        public async Task<BulkFXPurchaseResponse> AddBulkFXPurchaseAsync(IEnumerable<ForexDetailsRequest> forexDetailsRequest, long institutionId)
        {
            int count = forexDetailsRequest.Count(); //10
            int iterator = 0;
            while (count!=0)
            {
                var item = forexDetailsRequest.ToList().ElementAt(iterator);
                var result = BVNVerifier.VerifyBVN(item.bvnApplicant);
                if (!result)
                {
                    forexDetailsRequest.ToList().Remove(item);
                }

                count--;
                iterator++;
            }


            BulkFXPurchaseResponse bulkFXPurchaseResponse = new BulkFXPurchaseResponse();
            IList<ForexResponse> forexResponses = new List<ForexResponse>();
            var response = forexDetailsRequest.AsDataTable();
            response.Columns["Amount"].ColumnName = "amount";
            response.Columns["bvnApplicant"].ColumnName = "bvn_applicant";
            response.Columns["bvnApplicantAccount"].ColumnName = "bvn_applicant_acct";
            response.Columns["bvnBeneficiary"].ColumnName = "bvn_beneficiary";
            response.Columns["PassportNumber"].ColumnName = "passport_no";
            response.Columns["DateOfBirth"].ColumnName = "DateofBirth";
            response.Columns["PurchaseType"].ColumnName = "Purchase_type";
            response.Columns["Purpose"].ColumnName = "purpose";
            response.Columns["Rate"].ColumnName = "rate";
            response.Columns["RequestDate"].ColumnName = "request_date";
            response.Columns["RequestID"].ColumnName = "request_id";
            response.Columns["TransactionType"].ColumnName = "transaction_type";

            response.Columns.Add(new DataColumn { ColumnName = "institution", DefaultValue = 2 });
            response.Columns.Add(new DataColumn { ColumnName = "OperationType", DefaultValue = "ADD" });
            response.Columns.Add(new DataColumn { ColumnName = "tax_clearance_cert_no" });


            var dv = response.DefaultView;
            response = dv.ToTable(true, "amount", "bvn_applicant", "bvn_applicant_acct", "bvn_beneficiary", "passport_no", "purpose", "rate", "request_date", "request_id", "tax_clearance_cert_no", "institution", "transaction_type", "Purchase_type", "DateofBirth", "OperationType");


             using (SqlConnection connection = new SqlConnection(_connectionStrings.FXMSSQLDbConnectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("InsertBulkFXPurchase", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlParameter sqlParam = cmd.Parameters.AddWithValue("@FXBULKINSERT", response);
                    sqlParam.SqlDbType = SqlDbType.Structured;
                    using (var BulkInsertResponse = await cmd.ExecuteReaderAsync())
                    {
                        while (BulkInsertResponse.Read())
                        {

                            ForexResponse forexResponse = new ForexResponse();
                            forexResponse.ResponseCode = SUCCESSFULCODE;
                            forexResponse.ResponseDescription = SUCCESSFULMESSAGE;

                            forexResponse.RequestID = BulkInsertResponse.GetString(BulkInsertResponse.GetOrdinal("RequestID"));
                            forexResponse.ResponseFXID = BulkInsertResponse.GetInt64(BulkInsertResponse.GetOrdinal("id"));
                            forexResponse.bvnApplicant = BulkInsertResponse.GetString(BulkInsertResponse.GetOrdinal("bvn_applicant"));

                            forexResponses.Add(forexResponse);

                        }
                    }

                }
            }
            response.Clear();
            bulkFXPurchaseResponse.SuccessfulForexResponses = forexResponses;
            bulkFXPurchaseResponse.numberofSuccessfulTransactions = forexResponses.Count;
            bulkFXPurchaseResponse.numberofFailedTransactions = forexDetailsRequest.Count() - forexResponses.Count;
            bulkFXPurchaseResponse.ResponseCode = SUCCESSFULCODE;
            bulkFXPurchaseResponse.ResponseDescription = SUCCESSFULMESSAGE;
            //var allfxRequest = forexDetailsRequest.Select(allfx => new ForexResponse { bvnApplicant = allfx.bvnApplicant, RequestID = allfx.RequestID });

            //allfxRequest.ToList().RemoveAll(forexResponses.Select(a => new ForexResponse { });
            //    //.Except(forexResponses, new ForexResponseComparer())
            //    .ToList()
            //    //.ForEach(a=> { a.ResponseCode = "01";a.ResponseDescription = "Failed To Log Forex Purchase"; });
            //bulkFXPurchaseResponse.FailedForexResponses = allfxRequest;
            return bulkFXPurchaseResponse;
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

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

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

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

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








        public async Task<IEnumerable<T>> MSQueryDatabase<T>(string SqlQuery, object objectParams, bool IsStoredProcedure, string connectionString)
        {

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QueryAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                  
                  
                    
                    conn.Close();

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);
                  
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


        public async Task<IEnumerable<T>> MSQueryDatabaseMultipleReturnSet<T>(string SqlQuery, object objectParams, bool IsStoredProcedure, string connectionString)
        {

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    //var list = await conn.QueryAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    var list = await conn.QueryMultipleAsync(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);

                    var result1 = await list.ReadAsync<T>();
                    var result2 = await list.ReadAsync<T>();

                    conn.Close();

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);
                    if (result1.Count() > 0)
                    {
                        return result1;
                    }
                    if (result2.Count() > 0)
                    {
                        return result2;
                    }
                    return null;
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



        public async Task<T> MSQueryDatabaseSingle<T>(string SqlQuery, object objectParams, bool IsStoredProcedure, string connectionString)
        {

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.QuerySingleAsync<T>(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

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

        public async Task<object> MSExecuteDatabaseSingle(string SqlQuery, object objectParams, bool IsStoredProcedure, string connectionString)
        {

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    CommandType commandType = CommandType.StoredProcedure;
                    if (!IsStoredProcedure)
                    {
                        commandType = CommandType.Text;
                    }

                    await conn.OpenAsync();
                    var list = await conn.ExecuteScalarAsync(SqlQuery, objectParams, null, TIMEOUT, commandType: commandType);
                    conn.Close();

                    Log.Logger.Debug("{0} rows were returned from  {1}  Parameters {2} ", list, SqlQuery, objectParams);

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
            basicResponse.ResponseCode = ex.Number.ToString();
            basicResponse.ResponseDescription = ex.Message;
        }

    }


    public static class IEnumerableExtensions
    {
        public static DataTable AsDataTable<T>(this IEnumerable<T> data)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }
    }

    public class ForexResponseComparer : IEqualityComparer<ForexResponse>
    {
        public bool Equals([AllowNull] ForexResponse x, [AllowNull] ForexResponse y)
        {
            if (x != null && y != null && x.RequestID.Equals(y.RequestID, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetHashCode([DisallowNull] ForexResponse obj) => HashCode.Combine(obj.RequestID);



    }
}
