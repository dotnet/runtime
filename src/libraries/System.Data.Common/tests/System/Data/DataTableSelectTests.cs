// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.SqlTypes;
using Xunit;

namespace System.Data.Tests
{
    public class DataTableSelectTests
    {
        /// <summary>
        /// Regression test: BinaryNode.EvalBinaryOp OR operator performed an unconditional
        /// (bool)vLeft unbox when vLeft could be SqlBoolean сausing InvalidCastException
        /// Covers the full truth table to exercise both the short-circuit path left=true
        /// and the fall-through path left=false, evaluate right.
        /// </summary>
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void OrOperator_WithSqlBooleanColumns(bool left, bool right, bool expectedMatch)
        {
            DataTable table = new DataTable("TestTable");
            table.Columns.Add("Col1", typeof(SqlBoolean));
            table.Columns.Add("Col2", typeof(SqlBoolean));

            DataRow row = table.NewRow();
            row["Col1"] = new SqlBoolean(left);
            row["Col2"] = new SqlBoolean(right);
            table.Rows.Add(row);

            DataRow[] result = table.Select("Col1 OR Col2");

            Assert.Equal(expectedMatch ? 1 : 0, result.Length);
        }

        [Fact]
        public void AndOperator_WithSqlBooleanColumn_WorksCorrectly()
        {
            DataTable table = new DataTable("TestTable");
            table.Columns.Add("Col1", typeof(SqlBoolean));
            table.Columns.Add("Col2", typeof(SqlBoolean));

            // true AND true => true
            DataRow row1 = table.NewRow();
            row1["Col1"] = SqlBoolean.True;
            row1["Col2"] = SqlBoolean.True;
            table.Rows.Add(row1);

            // true AND false => false
            DataRow row2 = table.NewRow();
            row2["Col1"] = SqlBoolean.True;
            row2["Col2"] = SqlBoolean.False;
            table.Rows.Add(row2);

            //false AND True => False short-circuit on left=false
            DataRow row3 = table.NewRow();
            row3["Col1"] = SqlBoolean.False;
            row3["Col2"] = SqlBoolean.True;
            table.Rows.Add(row3);

            // AND operator correctly handles SqlBoolean should not throw
            DataRow[] result = table.Select("Col1 AND Col2");

            // Only row 1 should match
            Assert.Equal(1, result.Length);
        }
    }
}
