// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Data.Tests
{
    public class DataRowComparerTests
    {
        [Fact]
        public void Default_Get_ReturnsNotNull()
        {
            Assert.NotNull(DataRowComparer.Default);
            Assert.NotNull(DataRowComparer<DataRow>.Default);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var table1 = new DataTable("Table1");
            table1.Columns.Add("Column1");
            table1.Columns.Add("Column2");
            table1.Columns.Add("Column3");

            var table2 = new DataTable("Table2");
            table2.Columns.Add("Column1");

            DataRow row = table2.Rows.Add(1);

            // Basic
            yield return new object[]
            {
                row,
                row,
                true
            };

            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                table1.Rows.Add(1, 2, null),
                true
            };

            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                table1.Rows.Add(1, 3, null),
                false
            };

            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                table1.Rows.Add(1, 2, "abc"),
                false
            };

            // DBNull
            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                table1.Rows.Add(1, 2, DBNull.Value),
                true
            };

            yield return new object[]
            {
                table1.Rows.Add(1, 2, DBNull.Value),
                table1.Rows.Add(1, 2, "abc"),
                false
            };

            // Array
            var arrayTable = new DataTable("Table3");
            DataColumn arrayColumn = arrayTable.Columns.Add("Column1");
            arrayColumn.DataType = typeof(Array);

            DataRow ArrayRow(object array) => arrayTable.Rows.Add(array);

            int[] sameArray = new int[] { 1, 2, 3 };
            yield return new object[]
            {
                ArrayRow(sameArray),
                ArrayRow(sameArray),
                true
            };

            yield return new object[]
            {
                ArrayRow(new byte[] { 1, 2, 3 }),
                ArrayRow(new byte[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new byte[] { 1, 2, 3 }),
                ArrayRow(new byte[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new short[] { 1, 2, 3 }),
                ArrayRow(new short[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new short[] { 1, 2, 3 }),
                ArrayRow(new short[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new int[] { 1, 2, 3 }),
                ArrayRow(new int[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new int[] { 1, 2, 3 }),
                ArrayRow(new int[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new string[] { "a", "b" }),
                ArrayRow(new string[] { "a", "b" }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new string[] { "a", "b" }),
                ArrayRow(new string[] { "a", "c" }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new object[] { 1, "a", null }),
                ArrayRow(new object[] { 1, "a", null }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new object[] { 1, "a", null }),
                ArrayRow(new object[] { 1, "a", DBNull.Value }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new object[] { 1, "a", null }),
                ArrayRow(new object[] { 2, "a", null }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new object[] { 1, "a", null }),
                ArrayRow(new object[] { 1, "a", "b" }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new ushort[] { 1, 2, 3 }),
                ArrayRow(new ushort[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new ushort[] { 1, 2, 3 }),
                ArrayRow(new ushort[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new uint[] { 1, 2, 3 }),
                ArrayRow(new uint[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new uint[] { 1, 2, 3 }),
                ArrayRow(new uint[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new long[] { 1, 2, 3 }),
                ArrayRow(new long[] { 1, 2, 3 }),
                true
            };

            yield return new object[]
            {
                ArrayRow(new long[] { 1, 2, 3 }),
                ArrayRow(new long[] { 1, 2, 4 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new long[] { 1, 2, 3 }),
                ArrayRow(new int[] { 1, 2, 3 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new int[] { 1, 2, 3 }),
                ArrayRow(new int[] { 1, 2 }),
                false
            };

            yield return new object[]
            {
                ArrayRow(new int[] { 1, 2, 3 }),
                ArrayRow(new int[2,2]),
                false
            };

            yield return new object[]
            {
                ArrayRow(new int[2,2]),
                ArrayRow(new int[2,2]),
                false
            };

            yield return new object[]
            {
                ArrayRow(new int[2]),
                ArrayRow(null),
                false
            };

            // Different count.
            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                table2.Rows.Add(1),
                false
            };

            // Null argument.
            yield return new object[]
            {
                table1.Rows.Add(1, 2, null),
                null,
                false
            };

            yield return new object[]
            {
                null,
                table1.Rows.Add(1, 2, null),
                false
            };

            yield return new object[]
            {
                null,
                null,
                true
            };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Rows_ReturnsExpected(DataRow row1, DataRow row2, bool expected)
        {
            Assert.Equal(expected, DataRowComparer.Default.Equals(row1, row2));
            Assert.Equal(expected, DataRowComparer.Default.Equals(row2, row1));
        }

        [Fact]
        public void Equals_NullStringValueInStringArrayInRightColumn_ThrowsNullReferenceException()
        {
            var table = new DataTable("Table");
            DataColumn column = table.Columns.Add("Column");
            column.DataType = typeof(Array);

            DataRow row1 = table.Rows.Add(new object[] { new string[] { null } });
            DataRow row2 = table.Rows.Add(new object[] { new string[] { null } });
            DataRow row3 = table.Rows.Add(new object[] { new string[] { "abc" } });

            Assert.Throws<NullReferenceException>(() => DataRowComparer.Default.Equals(row1, row2));
            Assert.Throws<NullReferenceException>(() => DataRowComparer.Default.Equals(row2, row1));
            Assert.Throws<NullReferenceException>(() => DataRowComparer.Default.Equals(row1, row3));

            Assert.False(DataRowComparer.Default.Equals(row3, row1));
        }

        [Fact]
        public void Equals_DeletedRow_ThrowsInvalidOperationException()
        {
            var table = new DataTable("Table");
            table.Columns.Add("Column");

            DataRow row1 = table.Rows.Add(1);
            DataRow row2 = table.Rows.Add(2);

            table.AcceptChanges();
            row1.Delete();

            Assert.Throws<InvalidOperationException>(() => DataRowComparer<DataRow>.Default.Equals(row1, row2));
            Assert.Throws<InvalidOperationException>(() => DataRowComparer<DataRow>.Default.Equals(row2, row1));
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            yield return new object[] { 123, 123 };
            yield return new object[] { null, DBNull.Value.GetHashCode() };

            yield return new object[] { new int[0], 0 };
            yield return new object[] { new int[] { 1, 2 }, 1 };

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                Array nonZeroBoundArray = Array.CreateInstance(typeof(int), new int[] { 2 }, new int[] { 2 });
                nonZeroBoundArray.SetValue(10, 2);
                yield return new object[] { nonZeroBoundArray, 10 };
            }

            Array multidimensionalArray = new int[,] { { 1, 2 }, { 3, 4 } };
            yield return new object[] { multidimensionalArray, multidimensionalArray.GetHashCode() };
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public void GetHashCode_HasColumns_ReturnsExpected(object value, int expected)
        {
            var table = new DataTable();
            DataColumn column = table.Columns.Add("Column1");
            table.Columns.Add("Column2");
            column.DataType = value?.GetType() ?? typeof(object);

            DataRow row = table.Rows.Add(value, 2);
            Assert.Equal(expected, DataRowComparer.Default.GetHashCode(row));
        }

        [Fact]
        public void GetHashCode_NoColumns_ReturnsZero()
        {
            var table = new DataTable();
            Assert.Equal(0, DataRowComparer.Default.GetHashCode(table.NewRow()));
        }

        [Fact]
        public void GetHashCode_OneColumn_DoesNotReturnZero()
        {
            var comparer = DataRowComparer<DataRow>.Default;
            DataTable table = new DataTable();
            DataRow row = table.NewRow();
            table.Columns.Add();

            Assert.NotEqual(0, comparer.GetHashCode(row));
        }

        [Fact]
        public void GetHashCode_NullRow_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("row", () => DataRowComparer<DataRow>.Default.GetHashCode(null));
        }

        [Fact]
        public void GetHashCode_DeletedRow_ThrowsInvalidOperationException()
        {
            var table = new DataTable("Table");
            table.Columns.Add("Column");

            DataRow row = table.Rows.Add(1);
            table.AcceptChanges();
            row.Delete();

            Assert.Throws<InvalidOperationException>(() => DataRowComparer<DataRow>.Default.GetHashCode(row));
        }

    }
}