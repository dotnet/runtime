// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class UdtBulkCopyTest
    {
        private string _connStr;

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.IsUdtTestDatabasePresent), nameof(DataTestUtility.AreConnStringsSetup))]
        public void RunCopyTest()
        {
            _connStr = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { InitialCatalog = DataTestUtility.UdtTestDbName }).ConnectionString;
            SqlConnection conn = new SqlConnection(_connStr);

            string cities = DataTestUtility.GetUniqueNameForSqlServer("UdtBulkCopy_cities");
            string customers = DataTestUtility.GetUniqueNameForSqlServer("UdtBulkCopy_customers");
            string circles = DataTestUtility.GetUniqueNameForSqlServer("UdtBulkCopy_circles");

            conn.Open();
            try
            {
                ExecuteNonQueryCommand($"create table {cities} (name sysname, location Point)", _connStr);
                ExecuteNonQueryCommand($"create table {customers} (name nvarchar(30), address Address)", _connStr);
                ExecuteNonQueryCommand($"create table {circles} (num int, def Circle)", _connStr);

                string expectedResults =
                    "ColumnName[0] = name" + Environment.NewLine +
                    "DataType[0] = nvarchar" + Environment.NewLine +
                    "FieldType[0] = System.String" + Environment.NewLine +
                    "ColumnName[1] = location" + Environment.NewLine +
                    "DataType[1] = UdtTestDb.dbo.Point" + Environment.NewLine +
                    "FieldType[1] = Point" + Environment.NewLine +
                    "   redmond, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                    "  bellevue, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                    "   seattle, p.X =  10, p.Y =  10, p.Distance() = 14.866068747318506" + Environment.NewLine +
                    "  portland, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                    "        LA, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                    "       SFO, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                    " beaverton, p.X =  10, p.Y =  10, p.Distance() = 14.866068747318506" + Environment.NewLine +
                    "  new york, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                    "     yukon, p.X =  20, p.Y =  20, p.Distance() = 32.01562118716424" + Environment.NewLine;

                CopyTableTest(_connStr, "cities", cities, expectedResults);

                expectedResults =
                    "ColumnName[0] = name" + Environment.NewLine +
                    "DataType[0] = nvarchar" + Environment.NewLine +
                    "FieldType[0] = System.String" + Environment.NewLine +
                    "ColumnName[1] = address" + Environment.NewLine +
                    "DataType[1] = UdtTestDb.dbo.Address" + Environment.NewLine +
                    "FieldType[1] = Address" + Environment.NewLine +
                    "     first, Address 1  Address 2" + Environment.NewLine +
                    "    second, 123 Park Lane  New York" + Environment.NewLine +
                    "     third, 21 Forest grove  Portland" + Environment.NewLine +
                    "    fourth, 34 Lake Blvd  Seattle" + Environment.NewLine +
                    "     fifth, A2 Meadows  Bellevue" + Environment.NewLine;

                CopyTableTest(_connStr, "customers", customers, expectedResults);

                expectedResults =
                    "ColumnName[0] = num" + Environment.NewLine +
                    "DataType[0] = int" + Environment.NewLine +
                    "FieldType[0] = System.Int32" + Environment.NewLine +
                    "ColumnName[1] = def" + Environment.NewLine +
                    "DataType[1] = UdtTestDb.dbo.Circle" + Environment.NewLine +
                    "FieldType[1] = Circle" + Environment.NewLine +
                    "         1, Center = 1,2" + Environment.NewLine +
                    "         2, Center = 3,4" + Environment.NewLine +
                    "         3, Center = 11,23" + Environment.NewLine +
                    "         4, Center = 444,555" + Environment.NewLine +
                    "         5, Center = 1,2" + Environment.NewLine +
                    "         6, Center = 3,4" + Environment.NewLine +
                    "         7, Center = 11,23" + Environment.NewLine +
                    "         8, Center = 444,245" + Environment.NewLine;

                CopyTableTest(_connStr, "circles", circles, expectedResults);
            }
            finally
            {
                ExecuteNonQueryCommand($"drop table {cities}", _connStr);
                ExecuteNonQueryCommand($"drop table {customers}", _connStr);
                ExecuteNonQueryCommand($"drop table {circles}", _connStr);
            }
        }

        private void ExecuteNonQueryCommand(string cmdText, string connStr)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = cmdText;
                cmd.ExecuteNonQuery();
            }
        }

        private void CopyTableTest(string connStr, string sourceTable, string targetTable, string expectedResults)
        {
            using (SqlConnection srcConn = new SqlConnection(connStr))
            {
                srcConn.Open();

                SqlCommand cmd = srcConn.CreateCommand();

                cmd.CommandText = "select * from " + sourceTable;
                using (SqlDataReader reader = cmd.ExecuteReader())
                using (SqlBulkCopy bc = new SqlBulkCopy(connStr))
                {
                    bc.DestinationTableName = targetTable;
                    bc.WriteToServer(reader);
                }
                cmd.CommandText = "select * from " + targetTable;

                DataTestUtility.AssertEqualsWithDescription(
                    expectedResults, UdtTestHelpers.DumpReaderString(cmd.ExecuteReader()),
                    "Unexpected bulk copy results.");
            }
        }
    }
}

