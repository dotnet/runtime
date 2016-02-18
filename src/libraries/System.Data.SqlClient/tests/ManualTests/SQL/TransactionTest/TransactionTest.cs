// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class TransactionTest
    {
        [Fact]
        public void TestYukon()
        {
            new TransactionTestWorker(DataTestClass.SQL2005_Northwind + ";multipleactiveresultsets=true;").StartTest();
        }

        [Fact]
        public void TestKatmai()
        {
            new TransactionTestWorker(DataTestClass.SQL2008_Northwind + ";multipleactiveresultsets=true;").StartTest();
        }
    }


    internal class TransactionTestWorker
    {
        private static string s_tempTableName1 = string.Format("TEST_{0}{1}{2}", Environment.GetEnvironmentVariable("ComputerName"), Environment.TickCount, Guid.NewGuid()).Replace('-', '_');
        private static string s_tempTableName2 = s_tempTableName1 + "_2";
        private string _connectionString;

        public TransactionTestWorker(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void StartTest()
        {
            try
            {
                PrepareTables();

                CommitTransactionTest();
                ResetTables();

                RollbackTransactionTest();
                ResetTables();

                ScopedTransactionTest();
                ResetTables();

                ExceptionTest();
                ResetTables();

                ReadUncommitedIsolationLevel_ShouldReturnUncommitedData();
                ResetTables();

                ReadCommitedIsolationLevel_ShouldReceiveTimeoutExceptionBecauseItWaitsForUncommitedTransaction();
                ResetTables();
            }
            finally
            {
                //make sure to clean up
                DropTempTables();
            }
        }

        private void PrepareTables()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand(string.Format("CREATE TABLE [{0}]([CustomerID] [nchar](5) NOT NULL PRIMARY KEY, [CompanyName] [nvarchar](40) NOT NULL, [ContactName] [nvarchar](30) NULL)", s_tempTableName1), conn);
                command.ExecuteNonQuery();
                command.CommandText = "create table " + s_tempTableName2 + "(col1 int, col2 varchar(32))";
                command.ExecuteNonQuery();
            }
        }

        private void DropTempTables()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(
                        string.Format("DROP TABLE [{0}]; DROP TABLE [{1}]", s_tempTableName1, s_tempTableName2), conn);
                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public void ResetTables()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(string.Format("TRUNCATE TABLE [{0}]; TRUNCATE TABLE [{1}]", s_tempTableName1, s_tempTableName2), connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void CommitTransactionTest()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from " + s_tempTableName1 + " where CustomerID='ZYXWV'", connection);

                connection.Open();

                SqlTransaction tx = connection.BeginTransaction();
                command.Transaction = tx;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.False(reader.HasRows, "Error: table is in incorrect state for test.");
                }

                using (SqlCommand command2 = connection.CreateCommand())
                {
                    command2.Transaction = tx;

                    command2.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXWV', 'XYZ', 'John' );";
                    command2.ExecuteNonQuery();
                }

                tx.Commit();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read()) { count++; }
                    Assert.True(count == 1, "Error: incorrect number of rows in table after update.");
                    Assert.Equal(count, 1);
                }
            }
        }

        private void RollbackTransactionTest()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from " + s_tempTableName1 + " where CustomerID='ZYXWV'",
                    connection);
                connection.Open();

                SqlTransaction tx = connection.BeginTransaction();
                command.Transaction = tx;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.False(reader.HasRows, "Error: table is in incorrect state for test.");
                }

                using (SqlCommand command2 = connection.CreateCommand())
                {
                    command2.Transaction = tx;

                    command2.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXWV', 'XYZ', 'John' );";
                    command2.ExecuteNonQuery();
                }

                tx.Rollback();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.False(reader.HasRows, "Error Rollback Test : incorrect number of rows in table after rollback.");
                    int count = 0;
                    while (reader.Read()) count++;
                    Assert.Equal(count, 0);
                }

                connection.Close();
            }
        }


        private void ScopedTransactionTest()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("select * from " + s_tempTableName1 + " where CustomerID='ZYXWV'",
                    connection);

                connection.Open();

                SqlTransaction tx = connection.BeginTransaction("transName");
                command.Transaction = tx;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.False(reader.HasRows, "Error: table is in incorrect state for test.");
                }
                using (SqlCommand command2 = connection.CreateCommand())
                {
                    command2.Transaction = tx;

                    command2.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXWV', 'XYZ', 'John' );";
                    command2.ExecuteNonQuery();
                }
                tx.Save("saveName");

                //insert another one
                using (SqlCommand command2 = connection.CreateCommand())
                {
                    command2.Transaction = tx;

                    command2.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXW2', 'XY2', 'KK' );";
                    command2.ExecuteNonQuery();
                }

                tx.Rollback("saveName");

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.HasRows, "Error Scoped Transaction Test : incorrect number of rows in table after rollback to save state one.");
                    int count = 0;
                    while (reader.Read()) count++;
                    Assert.Equal(count, 1);
                }

                tx.Rollback();

                connection.Close();
            }
        }


        private void ExceptionTest()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlTransaction tx = connection.BeginTransaction();

                string invalidSaveStateMessage = SystemDataResourceManager.Instance.SQL_NullEmptyTransactionName;
                string executeCommandWithoutTransactionMessage = SystemDataResourceManager.Instance.ADP_TransactionRequired("ExecuteNonQuery");
                string transactionConflictErrorMessage = SystemDataResourceManager.Instance.ADP_TransactionConnectionMismatch;
                string parallelTransactionErrorMessage = SystemDataResourceManager.Instance.ADP_ParallelTransactionsNotSupported("SqlConnection");

                AssertException<InvalidOperationException>(() =>
                {
                    SqlCommand command = new SqlCommand("sql", connection);
                    command.ExecuteNonQuery();
                }, executeCommandWithoutTransactionMessage);

                AssertException<InvalidOperationException>(() =>
                {
                    SqlConnection con1 = new SqlConnection(_connectionString);
                    con1.Open();

                    SqlCommand command = new SqlCommand("sql", con1);
                    command.Transaction = tx;
                    command.ExecuteNonQuery();
                }, transactionConflictErrorMessage);

                AssertException<InvalidOperationException>(() =>
                {
                    connection.BeginTransaction(null);
                }, parallelTransactionErrorMessage);

                AssertException<InvalidOperationException>(() =>
                {
                    connection.BeginTransaction("");
                }, parallelTransactionErrorMessage);

                AssertException<ArgumentException>(() =>
                {
                    tx.Rollback(null);
                }, invalidSaveStateMessage);

                AssertException<ArgumentException>(() =>
                {
                    tx.Rollback("");
                }, invalidSaveStateMessage);

                AssertException<ArgumentException>(() =>
                {
                    tx.Save(null);
                }, invalidSaveStateMessage);

                AssertException<ArgumentException>(() =>
                {
                    tx.Save("");
                }, invalidSaveStateMessage);
            }
        }

        public static void AssertException<T>(Action action, string expectedErrorMessage) where T : Exception
        {
            var exception = Assert.Throws<T>(action);
            Assert.Equal(exception.Message, expectedErrorMessage);
        }

        private void ReadUncommitedIsolationLevel_ShouldReturnUncommitedData()
        {
            using (SqlConnection connection1 = new SqlConnection(_connectionString))
            {
                connection1.Open();
                SqlTransaction tx1 = connection1.BeginTransaction();

                using (SqlCommand command1 = connection1.CreateCommand())
                {
                    command1.Transaction = tx1;

                    command1.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXWV', 'XYZ', 'John' );";
                    command1.ExecuteNonQuery();
                }
                using (SqlConnection connection2 = new SqlConnection(_connectionString))
                {
                    SqlCommand command2 =
                        new SqlCommand("select * from " + s_tempTableName1 + " where CustomerID='ZYXWV'",
                            connection2);
                    connection2.Open();
                    SqlTransaction tx2 = connection2.BeginTransaction(IsolationLevel.ReadUncommitted);
                    command2.Transaction = tx2;

                    using (SqlDataReader reader = command2.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read()) count++;
                        Assert.True(count == 1, "Should Expected 1 row because Isolation Level is read uncommited which should return uncommited data.");
                    }

                    tx2.Rollback();
                    connection2.Close();
                }

                tx1.Rollback();
                connection1.Close();
            }
        }

        private void ReadCommitedIsolationLevel_ShouldReceiveTimeoutExceptionBecauseItWaitsForUncommitedTransaction()
        {
            using (SqlConnection connection1 = new SqlConnection(_connectionString))
            {
                connection1.Open();
                SqlTransaction tx1 = connection1.BeginTransaction();

                using (SqlCommand command1 = connection1.CreateCommand())
                {
                    command1.Transaction = tx1;
                    command1.CommandText = "INSERT INTO " + s_tempTableName1 + " VALUES ( 'ZYXWV', 'XYZ', 'John' );";
                    command1.ExecuteNonQuery();
                }

                using (SqlConnection connection2 = new SqlConnection(_connectionString))
                {
                    SqlCommand command2 =
                        new SqlCommand("select * from " + s_tempTableName1 + " where CustomerID='ZYXWV'",
                            connection2);

                    connection2.Open();
                    SqlTransaction tx2 = connection2.BeginTransaction(IsolationLevel.ReadCommitted);
                    command2.Transaction = tx2;

                    AssertException<SqlException>(() => command2.ExecuteReader(), SystemDataResourceManager.Instance.SQL_Timeout as string);

                    tx2.Rollback();
                    connection2.Close();
                }

                tx1.Rollback();
                connection1.Close();
            }
        }
    }
}
