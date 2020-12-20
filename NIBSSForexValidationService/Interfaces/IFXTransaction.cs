using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NIBSSForexValidationService.Models;

namespace NIBSSForexValidationService.Interfaces
{
    public interface IFXTransaction
    {
        Task<ValidateBVNResponse> ValidateBVNAsync(ValidateBVNRequest validateBVNRequest);

       // Task<PurchaseSummaryResponse> GetPurchaseSummaryAsync(PurchaseSummaryRequest purchaseSummaryRequest);

        Task<ForexResponse> AddFXPurchaseAsync(ForexDetailsRequest forexDetailsRequest, long institutionID);

        Task<ForexResponse> RemoveFXPurchaseAsync(DeleteForexRequest deleteForexRequest);




        Task<BulkFXPurchaseResponse> AddBulkFXPurchaseAsync(IEnumerable<ForexDetailsRequest> forexDetailsRequest, long institutionID);

        


        //Task<IEnumerable<ForexResponse>> RemoveBulkFXPurchaseAsync(IEnumerable<DeleteForexRequest> deleteForexRequest);





    }

    public class BulkFXPurchaseResponse:BasicResponse
    {

        public IEnumerable<ForexResponse> SuccessfulForexResponses { get; set; } = new List<ForexResponse>();

        public int numberofSuccessfulTransactions { get; set; }
        public int numberofFailedTransactions { get; set; }
        
    }
}
