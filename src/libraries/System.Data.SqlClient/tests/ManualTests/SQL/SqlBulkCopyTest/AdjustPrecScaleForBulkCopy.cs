// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class AdjustPrecScaleForBulkCopy
    {
        [CheckConnStrSetupFact]
        public static void RunTest()
        {
            SqlDecimal value = BulkCopySqlDecimalToTable(new SqlDecimal(0), 1, 0, 2, 2);
            Assert.Equal("0.00", value.ToString());

            value = BulkCopySqlDecimalToTable(new SqlDecimal(10.0), 10, 1, 4, 2);
            Assert.Equal("10.00", value.ToString());

            value = BulkCopySqlDecimalToTable(new SqlDecimal(10.00), 7, 2, 2, 0);
            Assert.Equal("10", value.ToString());

            value = BulkCopySqlDecimalToTable(new SqlDecimal(12.345), 5, 3, 3, 1);
            Assert.Equal("12.3", value.ToString());

            value = BulkCopySqlDecimalToTable(new SqlDecimal(123.45), 10, 2, 4, 1);
            Assert.Equal("123.4", value.ToString());

            Assert.Throws<InvalidOperationException>(() => BulkCopySqlDecimalToTable(new SqlDecimal(111.00), 7, 2, 2, 0));
        }

        private static SqlDecimal BulkCopySqlDecimalToTable(SqlDecimal decimalValue, int sourcePrecision, int sourceScale, int targetPrecision, int targetScale)
        {
            string tableName = DataTestUtility.GenerateTableName();
            string connectionString = DataTestUtility.TcpConnStr;

            SqlDecimal resultValue;
            try
            {
                DataTestUtility.RunNonQuery(connectionString, $"create table {tableName} (target_column decimal({targetPrecision}, {targetScale}))");

                SqlDecimal inputValue = SqlDecimal.ConvertToPrecScale(decimalValue, sourcePrecision, sourceScale);

                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("source_column", typeof(SqlDecimal));
                DataRow row = dt.NewRow();
                row["source_column"] = inputValue;
                dt.Rows.Add(row);

                using (SqlBulkCopy sbc = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity))
                {
                    sbc.DestinationTableName = tableName;
                    sbc.ColumnMappings.Add("source_column", "target_column");
                    sbc.WriteToServer(dt);
                }

                DataTable resultTable = DataTestUtility.RunQuery(connectionString, $"select * from {tableName}");
                resultValue = new SqlDecimal((Decimal)resultTable.Rows[0][0]);
            }
            finally
            {
                DataTestUtility.RunNonQuery(connectionString, $"drop table {tableName}");
            }

            return resultValue;
        }
    }
}
