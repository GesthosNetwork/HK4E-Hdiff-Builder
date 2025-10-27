using System;
using System.Security.Cryptography;
using System.Text;

namespace HK4E.HdiffBuilder.Utils
{
    internal static class SystemTasks
    {
        private static readonly byte[] ExpectedHash = Convert.FromHexString(
            "C0B503B265B85F0940C809D04B20313E72D2188FE10037426B8A89EAE6C29E59"
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
