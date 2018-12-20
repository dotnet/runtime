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

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup))]
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

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup))]
        public static void BeginExecuteReaderWithCallback()
        {
            object state = new object();
            bool callbackExecutedFlag = false;

            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand command = new SqlCommand(GenerateCommandText(), connection))
            {
                connection.Open();

                Tuple<object, SqlCommand> stateAndCommand = new Tuple<object, SqlCommand>(state, command);

                IAsyncResult result = command.BeginExecuteReader(ar =>
                {
                    Tuple<object, SqlCommand> asyncArgs = ar.AsyncState as Tuple<object, SqlCommand>;
                    Assert.NotNull(asyncArgs);

                    SqlDataReader reader = asyncArgs.Item2.EndExecuteReader(ar);
                    callbackExecutedFlag = true;
                    Assert.True(reader.HasRows, $"FAILED: Reader has no rows");
                    Assert.Equal(state, asyncArgs.Item1);
                }, stateAndCommand);
                
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

                Assert.True(callbackExecutedFlag, $"FAILED: Callback did not executed");
            }
        }
    }
}
