using System;
using System.Security.Cryptography;
using System.Text;

namespace HK4E.HdiffBuilder.Utils
{
    internal static class SystemTasks
    {
        private static readonly byte[] ExpectedHash = Convert.FromHexString(
            "29A51BE708CC608D32579002FD4D714AC90BE6F4F881D15732DBC90DBC2FD94D"
        );

        public static void A05xF()
        {
            var X = WindowUtils.Y;
            var Z = SHA256.HashData(Encoding.UTF8.GetBytes(X));

            for (int i = 0; i < ExpectedHash.Length; i++)
            {
                if (Z[i] != ExpectedHash[i])
                {
                    Environment.FailFast(null);
                    return;
                }
            }
        }
    }
}
