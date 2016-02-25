// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class ParallelTransactionsTest
    {
        #region <<Basic Parallel Test>>
        [Fact]
        public void BasicParallelTest_ShouldThrowsUnsupported_Yukon()
        {
            BasicParallelTest_shouldThrowsUnsupported(DataTestClass.SQL2005_Pubs);
        }

        [Fact]
        public void BasicParallelTest_ShouldThrowsUnsupported_Katmai()
        {
            BasicParallelTest_shouldThrowsUnsupported(DataTestClass.SQL2008_Pubs);
        }

        private void BasicParallelTest_shouldThrowsUnsupported(string connectionString)
        {
            string expectedErrorMessage = SystemDataResourceManager.Instance.ADP_ParallelTransactionsNotSupported(typeof(SqlConnection).Name);
            string tempTableName = "";
            try
            {
                tempTableName = CreateTempTable(connectionString);
                DataTestClass.AssertThrowsWrapper<InvalidOperationException>(
                    actionThatFails: () => { BasicParallelTest(connectionString, tempTableName); },
                    exceptionMessage: expectedErrorMessage);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    DropTempTable(connectionString, tempTableName);
                }
            }
        }

        private void BasicParallelTest(string connectionString, string tempTableName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction trans1 = connection.BeginTransaction();
                SqlTransaction trans2 = connection.BeginTransaction();
                SqlTransaction trans3 = connection.BeginTransaction();

                SqlCommand com1 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com1.Transaction = trans1;
                com1.ExecuteNonQuery();

                SqlCommand com2 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com2.Transaction = trans2;
                com2.ExecuteNonQuery();

                SqlCommand com3 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com3.Transaction = trans3;
                com3.ExecuteNonQuery();

                trans1.Rollback();
                trans2.Rollback();
                trans3.Rollback();

                com1.Dispose();
                com2.Dispose();
                com3.Dispose();
            }
        }

        #endregion

        #region <<MultipleExecutesInSameTransactionTest>>
        [Fact]
        public void MultipleExecutesInSameTransactionTest_ShouldThrowsUnsupported_Yukon()
        {
            MultipleExecutesInSameTransactionTest_shouldThrowsUnsupported(DataTestClass.SQL2005_Pubs);
        }

        [Fact]
        public void MultipleExecutesInSameTransactionTest_ShouldThrowsUnsupported_Katmai()
        {
            MultipleExecutesInSameTransactionTest_shouldThrowsUnsupported(DataTestClass.SQL2008_Northwind);
        }

        private void MultipleExecutesInSameTransactionTest_shouldThrowsUnsupported(string connectionString)
        {
            string expectedErrorMessage = SystemDataResourceManager.Instance.ADP_ParallelTransactionsNotSupported(typeof(SqlConnection).Name);
            string tempTableName = "";
            try
            {
                tempTableName = CreateTempTable(connectionString);
                DataTestClass.AssertThrowsWrapper<InvalidOperationException>(
                    actionThatFails: () => { MultipleExecutesInSameTransactionTest(connectionString, tempTableName); },
                    exceptionMessage: expectedErrorMessage);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    DropTempTable(connectionString, tempTableName);
                }
            }
        }

        private void MultipleExecutesInSameTransactionTest(string connectionString, string tempTableName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction trans1 = connection.BeginTransaction();
                SqlTransaction trans2 = connection.BeginTransaction();
                SqlTransaction trans3 = connection.BeginTransaction();

                SqlCommand com1 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com1.Transaction = trans1;
                com1.ExecuteNonQuery();

                SqlCommand com2 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com2.Transaction = trans2;
                com2.ExecuteNonQuery();

                SqlCommand com3 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com3.Transaction = trans3;
                com3.ExecuteNonQuery();

                trans1.Rollback();
                trans2.Rollback();
                trans3.Rollback();

                com1.Dispose();
                com2.Dispose();
                com3.Dispose();

                SqlCommand com4 = new SqlCommand("select top 1 au_id from " + tempTableName, connection);
                com4.Transaction = trans1;
                SqlDataReader reader4 = com4.ExecuteReader();
                reader4.Dispose();
                com4.Dispose();

                trans1.Rollback();
            }
        }
        #endregion

        private string CreateTempTable(string connectionString)
        {
            var uniqueKey = string.Format("{0}_{1}_{2}", Environment.GetEnvironmentVariable("ComputerName"), Environment.TickCount, Guid.NewGuid()).Replace("-", "_");
            var tempTableName = "TEMP_" + uniqueKey;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(string.Format("SELECT au_id, au_lname, au_fname, phone, address, city, state, zip, contract into {0} from pubs.dbo.authors", tempTableName), conn);
                cmd.ExecuteNonQuery();
                cmd.CommandText = string.Format("alter table {0} add constraint au_id_{1} primary key (au_id)", tempTableName, uniqueKey);
                cmd.ExecuteNonQuery();
            }

            return tempTableName;
        }

        private void DropTempTable(string connectionString, string tempTableName)
        {
            using (SqlConnection con1 = new SqlConnection(connectionString))
            {
                con1.Open();
                SqlCommand cmd = new SqlCommand("Drop table " + tempTableName, con1);
                cmd.ExecuteNonQuery();
            }
        }
    }
}


