// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class BeginExecReaderAsyncTest
    {
        private static string GenerateCommandText()
        {
            int suffix = (new Random()).Next(5000);
            string companyName = "M Inc.";
            string phone = "777-1111";

            string commandText =
                $"CREATE TABLE #Shippers{suffix}(" +
                $"[ShipperID][int] NULL," +
                $"[CompanyName] [nvarchar] (40) NOT NULL," +
                $"[Phone] [nvarchar] (24) NULL )" +
                $"INSERT INTO #Shippers{suffix}" +
                $"([CompanyName]  " +
                $",[Phone])" +
                $"VALUES " +
                $"('{companyName}' " +
                $",'{phone}'); " +
                $"WAITFOR DELAY '0:0:3'; " +
                $"select s.ShipperID, s.CompanyName, s.Phone " +
                $"from #Shippers{suffix} s; ";
            
            return commandText;
        }

        [CheckConnStrSetupFact]
        public static void ExecuteTest()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                SqlCommand command = new SqlCommand(GenerateCommandText(), connection);
                connection.Open();

                IAsyncResult result = command.BeginExecuteReader();
                while (!result.IsCompleted)
                {
                    System.Threading.Thread.Sleep(100);
                }
                SqlDataReader reader = command.EndExecuteReader(result);
                Assert.True(reader.HasRows, $"FAILED: Reader has no rows");
            }
        }
    }
}
