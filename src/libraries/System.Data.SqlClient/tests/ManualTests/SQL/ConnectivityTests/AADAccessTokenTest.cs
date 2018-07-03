// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class AADAccessTokenTest
    {
        private static bool IsAccessTokenSetup() => DataTestUtility.IsAccessTokenSetup();
        private static bool IsAzureServer() => DataTestUtility.IsAzureSqlServer(GetDataSource());

        [ConditionalFact(nameof(IsAccessTokenSetup), nameof(IsAzureServer))]
        public static void AccessTokenTest()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.AccessToken = DataTestUtility.getAccessToken();
                connection.Open();
            }
        }

        private static string GetDataSource()
        {
            // Obtain Data source from connection string
            string tcpConnStr = DataTestUtility.TcpConnStr.Replace(" ", string.Empty);
            Regex regex = new Regex("DataSource=(.*?);");
            Match match = regex.Match(tcpConnStr);
            return match.Groups[1].ToString();
        }
    }
}
