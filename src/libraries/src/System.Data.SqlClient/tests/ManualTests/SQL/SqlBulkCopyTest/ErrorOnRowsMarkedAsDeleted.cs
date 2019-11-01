// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// Test case for SqlBulkEdit raising exception on rows marked as deleted
    /// </summary>
    public class ErrorOnRowsMarkedAsDeleted
    {
        // This char is used to mark the row as 'to be deleted'.
        // See RunCase lines below
        const char DeletedRowMark = 'D';

        /// <summary>
        /// specifies which overload of WriteToServer we want to test
        /// </summary>
        enum SqlBulkCopyInputType
        {
            DataTable,
            RowsList
        }

        public static void Test(string dstConstr, string targettable)
        {
            using (SqlConnection destConn = new SqlConnection(dstConstr))
            {
                destConn.Open();

                string tablePrefix = targettable.Replace(' ', '_') + "_Case";
                int tableIdx = 1;

                // run several cases, using DataTable or RowsList as an input method
                // no need to duplicate tests since from dev point they are sharing the same code
                RunCase(destConn, "empty table", SqlBulkCopyInputType.DataTable, tablePrefix + (tableIdx++), "D");
                RunCase(destConn, "single deleted row", SqlBulkCopyInputType.RowsList, tablePrefix + (tableIdx++), "D");
                RunCase(destConn, "deleted row is first", SqlBulkCopyInputType.DataTable, tablePrefix + (tableIdx++), "DRR");
                RunCase(destConn, "deleted row is last", SqlBulkCopyInputType.RowsList, tablePrefix + (tableIdx++), "RRD");
                RunCase(destConn, "continues deleted rows", SqlBulkCopyInputType.DataTable, tablePrefix + (tableIdx++), "RDDDDR");
                RunCase(destConn, "deleted row is in the middle", SqlBulkCopyInputType.RowsList, tablePrefix + (tableIdx++), "RDR");
                RunCase(destConn, "empty table", SqlBulkCopyInputType.DataTable, tablePrefix + (tableIdx++), "");
                RunCase(destConn, "no deleted rows", SqlBulkCopyInputType.RowsList, tablePrefix + (tableIdx++), "RR");
                RunCase(destConn, "big bang", SqlBulkCopyInputType.DataTable, tablePrefix + (tableIdx++), "DRRDRRRDDDDDDDDRRDRDDRDRDDDRRD");
            }
        }

        /// <summary>
        /// runs single case: creates DataTable with some of the rows deleted, calls SqlBulkCopy on it and
        /// validates the results
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="caseName"></param>
        /// <param name="tableName"></param>
        /// <param name="rows">rows map: 'N' means value = row index; 'D' means value is Deleted</param>
        /// <returns>true on failure; false - otherwise</returns>
        private static void RunCase(SqlConnection conn, string caseName, SqlBulkCopyInputType inputType, string tableName, string rowsMap)
        {
            // create simple table
            DataTable table = new DataTable();
            table.Columns.Add("IntVal", typeof(int));

            // create the rows, each one contains one field:
            // * the index of the row, if row stays
            // * -1 if row is marked to be deleted

            // row values has no meaning for the SqlBulkCopy, they are used for test output verification only
            // at the same loop, create list of rows that expected to appear in the target table after bulk copy

            StringBuilder expectedRows = new StringBuilder();
            for (int i = 0; i < rowsMap.Length; i++)
            {
                if (rowsMap[i] == DeletedRowMark)
                {
                    // this row is to be deleted, mark with -1
                    table.Rows.Add(-1);
                }
                else
                {
                    // this row should stay, use row index as value and add it to expected list
                    table.Rows.Add(i);
                    expectedRows.AppendFormat("{0} ", i);
                }
            }

            table.AcceptChanges();

            // mark the rows relevant for this case as deleted
            DataRow[] rowsToDelete = table.Select("IntVal=-1");
            for (int i = 0; i < rowsToDelete.Length; i++)
            {
                rowsToDelete[i].Delete();
            }

            // create SQL table with one int field, similar to the above DataTable
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE [" + tableName + "] (IntVal int)";
            cmd.ExecuteNonQuery();

            try
            {
                using (SqlBulkCopy bcp = new SqlBulkCopy(conn))
                {
                    bcp.DestinationTableName = tableName;

                    // run the test
                    switch (inputType)
                    {
                        case SqlBulkCopyInputType.DataTable:
                            bcp.WriteToServer(table);
                            break;
                        case SqlBulkCopyInputType.RowsList:
                            DataRow[] rowsList = new DataRow[table.Rows.Count];
                            table.Rows.CopyTo(rowsList, 0);
                            bcp.WriteToServer(rowsList);
                            break;
                        default:
                            throw new InvalidOperationException("How did we reach here?");
                    }
                }

                // now, read the Actual Rows from SQL
                StringBuilder actualRows = new StringBuilder();
                cmd.CommandText = "SELECT * FROM [" + tableName + "]";
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int val = Convert.ToInt32(r[0]);
                        actualRows.AppendFormat("{0} ", val);
                    }
                }

                // compare the Expected and the Actual values
                DataTestUtility.AssertEqualsWithDescription(expectedRows.ToString(), actualRows.ToString(), "Unexpected rows data for test case " + caseName);
            }
            finally
            {
                // delete the table
                cmd = conn.CreateCommand();
                cmd.CommandText = "DROP TABLE [" + tableName + "]";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
