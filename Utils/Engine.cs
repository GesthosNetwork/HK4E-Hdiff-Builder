using System;
using System.Security.Cryptography;
using System.Text;

namespace HK4E.HdiffBuilder.Utils
{
    internal static class SystemTasks
    {
        private static readonly byte[] ExpectedHash = Convert.FromHexString(
            "4FDD50C438772B9026544CD7225CF86F70E41DB462A5EE0DA8460D2E58C27018"
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
