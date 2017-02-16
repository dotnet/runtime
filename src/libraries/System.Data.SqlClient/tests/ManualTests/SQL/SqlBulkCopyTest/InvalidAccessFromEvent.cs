// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class InvalidAccessFromEvent
    {
        private static string _dstConstr;
        private static string _dstTable;
        private static SqlConnection _dstConn;
        private static SqlTransaction _tx;
        private static SqlCommand _dstcmd;
        private static DataTable _dataTable;
        private static string expectedErrorMsg;

        protected static void OnRowCopiedClose(object sender, SqlRowsCopiedEventArgs e)
        {
            _dstConn.Close();
        }

        protected static void OnRowCopiedExecute(object sender, SqlRowsCopiedEventArgs e)
        {
            _dstcmd.ExecuteNonQuery();
        }

        protected static void OnRowCopiedRollback(object sender, SqlRowsCopiedEventArgs e)
        {
            _tx.Rollback();
        }

        protected static void OnRowCopiedCommit(object sender, SqlRowsCopiedEventArgs e)
        {
            _tx.Commit();
        }

        protected static void OnRowCopiedChangeDatabase(object sender, SqlRowsCopiedEventArgs e)
        {
            _dstConn.ChangeDatabase("msdb");
        }

        protected static void OnRowCopiedBulkCopy(object sender, SqlRowsCopiedEventArgs e)
        {
            InnerTest(null);
        }

        public static void Test(string srcConstr, string dstConstr, string dstTable)
        {
            _dstConstr = dstConstr;
            _dstTable = dstTable;

            DataSet dataset;
            SqlDataAdapter adapter;

            using (SqlConnection srcConn = new SqlConnection(srcConstr))
            using (SqlCommand srcCmd = new SqlCommand("select top 20 * from orders", srcConn))
            {
                srcConn.Open();

                dataset = new DataSet("MyDataSet");
                adapter = new SqlDataAdapter(srcCmd);
                adapter.Fill(dataset);
                _dataTable = dataset.Tables[0];
            }

            using (_dstConn = new SqlConnection(dstConstr))
            {
                _dstConn.Open();
                _dstcmd = _dstConn.CreateCommand();

                try
                {
                    Helpers.Execute(_dstcmd, "create table " + dstTable + " (orderid int, customerid nchar(5), rdate datetime, freight money, shipname nvarchar(40))");
                    _dstcmd.CommandText = "truncate table " + dstTable;

                    expectedErrorMsg = SystemDataResourceManager.Instance.SQL_ConnectionLockedForBcpEvent;
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedRollback));
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedCommit));
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedChangeDatabase));
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedExecute));
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedBulkCopy));

                    // this will close the connect which is valid so it must be the last test!
                    expectedErrorMsg = string.Format(
                        SystemDataResourceManager.Instance.ADP_OpenConnectionRequired,
                        "WriteToServer",
                        SystemDataResourceManager.Instance.ADP_ConnectionStateMsg_Closed);
                    InnerTest(new SqlRowsCopiedEventHandler(OnRowCopiedClose));
                }
                finally
                {
                    // the original connection is probably trashed
                    Helpers.TryDropTable(dstConstr, dstTable);
                }
            }
        }

        internal static void InnerTest(SqlRowsCopiedEventHandler eventHandler)
        {
            bool hitException = false;
            try
            {
                if (null == _tx || null == _tx.Connection)
                {
                    _tx = _dstConn.BeginTransaction();
                    _dstcmd.Transaction = _tx;
                    _dstcmd.ExecuteNonQuery();
                }
                SqlBulkCopy bulkcopy;
                using (bulkcopy = new SqlBulkCopy(_dstConn, SqlBulkCopyOptions.Default, _tx))
                {
                    bulkcopy.SqlRowsCopied += eventHandler;

                    bulkcopy.DestinationTableName = _dstTable;
                    bulkcopy.NotifyAfter = 10;

                    SqlBulkCopyColumnMappingCollection ColumnMappings = bulkcopy.ColumnMappings;

                    ColumnMappings.Add(0, "orderid");
                    ColumnMappings.Add(1, "customerid");
                    ColumnMappings.Add(4, "rdate");
                    ColumnMappings.Add(7, "freight");
                    ColumnMappings.Add(8, "shipname");

                    bulkcopy.WriteToServer(_dataTable);
                    bulkcopy.SqlRowsCopied -= eventHandler;

                    _tx.Commit();
                }
            }
            catch (Exception e)
            {
                while (null != e.InnerException)
                {
                    e = e.InnerException;
                }

                Assert.True(e is InvalidOperationException, "Unexpected exception type: " + e.GetType());
                Assert.True(e.Message.Contains(expectedErrorMsg), string.Format("Incorrect error message. Expected: {0}. Actual: {1}.", expectedErrorMsg, e.Message));
                _tx.Dispose();

                hitException = true;
            }
            Assert.True(hitException, "Did not encounter expected exception.");
        }
    }
}

