// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Common.Tests
{
    public class DataRecordInternalTest
    {
        [Fact]
        public void GetBytes_NegativeDataIndex_ThrowsIndexOutOfRangeException()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Bytes", typeof(byte[]));
            table.Rows.Add(new byte[] { 1, 2, 3 });

            using DataTableReader reader = table.CreateDataReader();
            reader.Read();

            byte[] buffer = new byte[3];

            Assert.Throws<IndexOutOfRangeException>(() => reader.GetBytes(0, Int64.MinValue, buffer, 0, buffer.Length));
        }

        [Fact]
        public void GetChars_NegativeDataIndex_ThrowsIndexOutOfRangeException()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Chars", typeof(char[]));
            table.Rows.Add(new char[] { 'a', 'b', 'c' });

            using DataTableReader reader = table.CreateDataReader();
            reader.Read();

            char[] buffer = new char[3];

            Assert.Throws<IndexOutOfRangeException>(() => reader.GetChars(0, Int64.MinValue, buffer, 0, buffer.Length));
        }
    }
}
