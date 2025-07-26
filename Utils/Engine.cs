using System;
using System.Security.Cryptography;
using System.Text;

namespace HK4E.HdiffBuilder.Utils
{
    internal static class SystemTasks
    {
        private static readonly byte[] ExpectedHash = Convert.FromHexString(
            "2EE2ADDD5D596BA179A18AD304CBEB9DB269B6F80AD81734B991EB757BA6033E"
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
