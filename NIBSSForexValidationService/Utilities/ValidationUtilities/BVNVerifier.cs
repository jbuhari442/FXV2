using System;
namespace NIBSSForexValidationService.Utilities.ValidationUtilities
{
    public class BVNVerifier
    {
        public BVNVerifier()
        {
        }
        public static bool VerifyBVN(string bvn)
        {

            int[] ArrayOfBVNMAGICNUMBERS = new int[] { 3, 1, 7, 3, 1, 7, 3, 1, 7, 3 };

            int STANDARDLENGTHOFBVN = 11;

            var bvnSpan = bvn.Trim().AsSpan();

            if (STANDARDLENGTHOFBVN != bvnSpan.Length)
            {
                throw new ArgumentException($"{bvn} length is less than {STANDARDLENGTHOFBVN}");
            }

            int sumofBVNWeight = 0;

            for (int positionHolder = 0; positionHolder < ArrayOfBVNMAGICNUMBERS.Length; positionHolder++)
            {
                int BVNWeight = ArrayOfBVNMAGICNUMBERS[positionHolder];
                var bvnSlice = bvnSpan.Slice(positionHolder, 1);

                int.TryParse(bvnSlice, out int numericValue);
                sumofBVNWeight = sumofBVNWeight + (BVNWeight * numericValue);
            }

            var ModulusOfWeight = sumofBVNWeight % 10;

            var lastDigitBVN = bvnSpan.Slice(10, 1);

            if (lastDigitBVN.ToString().Equals(ModulusOfWeight.ToString()))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
