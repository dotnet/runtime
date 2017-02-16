// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Data;
using System.Data.SqlClient;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class ColumnCollation
    {
        public static void Test(string dstConstr, string dstTable)
        {
            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();

                Helpers.Execute(dstCmd, "create table " + dstTable + " (name_jp varchar(20) collate Japanese_CI_AS, " +
                    "name_ru varchar(20) collate Cyrillic_General_CI_AS)");

                string s_jp = "江戸糸あやつり人形";
                string s_ru = "проверка";

                DataTable table = new DataTable();
                table.Columns.Add("name_jp", typeof(string));
                table.Columns.Add("name_ru", typeof(string));
                DataRow row = table.NewRow();
                row["name_jp"] = s_jp;
                row["name_ru"] = s_ru;
                table.Rows.Add(row);

                using (SqlBulkCopy bcp = new SqlBulkCopy(dstConn))
                {
                    bcp.DestinationTableName = dstTable;
                    bcp.WriteToServer(table);
                }

                using (SqlDataReader reader = (new SqlCommand("select * from  " + dstTable, dstConn)).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DataTestUtility.AssertEqualsWithDescription(
                            0, string.CompareOrdinal(s_jp, reader["name_jp"] as string),
                            "Unexpected value: " + reader["name_jp"]);

                        DataTestUtility.AssertEqualsWithDescription(
                            0, string.CompareOrdinal(s_ru, reader["name_ru"] as string),
                            "Unexpected value: " + reader["name_ru"]);
                    }
                }

            }

            using (SqlConnection dstConn = new SqlConnection(dstConstr))
            using (SqlCommand dstCmd = dstConn.CreateCommand())
            {
                dstConn.Open();
                Helpers.TryExecute(dstCmd, "drop table " + dstTable);
            }
        }

    }
}
