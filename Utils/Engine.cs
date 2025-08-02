using System;
using System.Security.Cryptography;
using System.Text;

namespace HK4E.HdiffBuilder.Utils
{
    internal static class SystemTasks
    {
        private static readonly byte[] ExpectedHash = Convert.FromHexString(
            "EE96692E649413B6F8E2EB91563DE2251190CE3D5F09666300EB05050C33F321"
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
