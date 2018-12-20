// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class BeginExecAsyncTest
    {
        private static string GenerateCommandText()
        {
            int suffix = (new Random()).Next(5000);

            string commandText = 
                $"CREATE TABLE #Shippers{suffix}(" +
                    $"[ShipperID][int] NULL," +
                    $"[CompanyName] [nvarchar] (40) NOT NULL," +
                    $"[Phone] [nvarchar] (24) NULL )" +
                $"INSERT INTO #Shippers{suffix}" +
                        $"([CompanyName]  " +
                        $",[Phone])" +
                    $"VALUES " +
                        $"('Acme Inc.' " +
                        $",'555-1212'); " +
                $"WAITFOR DELAY '0:0:3'; " +
                $"DELETE FROM #Shippers{suffix} WHERE ShipperID > 3;";

            return commandText;
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void ExecuteTest()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                try
                {
                    SqlCommand command = new SqlCommand(GenerateCommandText(), connection);
                    connection.Open();

                    IAsyncResult result = command.BeginExecuteNonQuery();
                    while (!result.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    Assert.True(command.EndExecuteNonQuery(result) > 0, "FAILED: BeginExecuteNonQuery did not complete successfully.");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("Error ({0}): {1}", ex.Number, ex.Message);
                    Assert.Null(ex);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                    Assert.Null(ex);
                }
                catch (Exception ex)
                {
                    // You might want to pass these errors
                    // back out to the caller.
                    Console.WriteLine("Error: {0}", ex.Message);
                    Assert.Null(ex);
                }
            }
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void FailureTest()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                bool caughtException = false;
                SqlCommand command = new SqlCommand(GenerateCommandText(), connection);
                connection.Open();

                //Try to execute a synchronous query on same command
                IAsyncResult result = command.BeginExecuteNonQuery();
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Assert.True(ex is InvalidOperationException, "FAILED: Thrown exception for BeginExecuteNonQuery was not an InvalidOperationException");
                    caughtException = true;
                }

                Assert.True(caughtException, "FAILED: No exception thrown after trying second BeginExecuteNonQuery.");
                caughtException = false;

                while (!result.IsCompleted)
                {
                    System.Threading.Thread.Sleep(100);
                }

                Assert.True(result.IsCompleted, "FAILED: ExecuteNonQueryAsync Task did not complete successfully.");
                Assert.True(command.EndExecuteNonQuery(result) > 0, "FAILED: No rows affected");
            }
        }
    }
}
