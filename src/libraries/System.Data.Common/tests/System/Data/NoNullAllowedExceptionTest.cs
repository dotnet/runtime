// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Xunit;


namespace System.Data.Tests
{
    public class NoNullAllowedExceptionTest
    {
        [Fact]
        public void Generate()
        {
            DataTable tbl = DataProvider.CreateParentDataTable();

            // ----------- check with columnn type int -----------------------
            tbl.Columns[0].AllowDBNull = false;

            //add new row with null value
            // NoNullAllowedException - Add Row
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows.Add(new object[] { null, "value", "value", new DateTime(0), 0.5, true });
            });

            //add new row with DBNull value
            // NoNullAllowedException - Add Row
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows.Add(new object[] { DBNull.Value, "value", "value", new DateTime(0), 0.5, true });
            });

            // NoNullAllowedException - ItemArray
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows[0].ItemArray = new object[] { DBNull.Value, "value", "value", new DateTime(0), 0.5, true };
            });

            // NoNullAllowedException - Add Row - LoadDataRow
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.LoadDataRow(new object[] { DBNull.Value, "value", "value", new DateTime(0), 0.5, true }, true);
            });

            // NoNullAllowedException - EndEdit
            tbl.Rows[0].BeginEdit();
            tbl.Rows[0][0] = DBNull.Value;
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows[0].EndEdit();
            });

            // ----------- add new column -----------------------
            tbl.Columns[0].AllowDBNull = true;
            tbl.Columns.Add(new DataColumn("bolCol", typeof(bool)));

            // add new column
            Assert.Throws<DataException>(() =>
            {
                tbl.Columns[tbl.Columns.Count - 1].AllowDBNull = false;
            });

            //clear table data in order to add the new column
            tbl.Rows.Clear();
            tbl.Columns[tbl.Columns.Count - 1].AllowDBNull = false;
            tbl.Rows.Add(new object[] { 99, "value", "value", new DateTime(0), 0.5, true, false }); //missing last value - will be null

            //add new row with null value
            // NoNullAllowedException - Add Row
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows.Add(new object[] { 99, "value", "value", new DateTime(0), 0.5, true }); //missing last value - will be null
            });

            //add new row with DBNull value
            // NoNullAllowedException - Add Row
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows.Add(new object[] { 1, "value", "value", new DateTime(0), 0.5, true, DBNull.Value });
            });

            // NoNullAllowedException - ItemArray
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows[0].ItemArray = new object[] { 77, "value", "value", new DateTime(0), 0.5, true, DBNull.Value };
            });

            // NoNullAllowedException - Add Row - LoadDataRow
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.LoadDataRow(new object[] { 66, "value", "value", new DateTime(0), 0.5, true }, true);
            });

            // NoNullAllowedException - EndEdit
            tbl.Rows[0].BeginEdit();
            tbl.Rows[0][tbl.Columns.Count - 1] = DBNull.Value;
            Assert.Throws<NoNullAllowedException>(() =>
            {
                tbl.Rows[0].EndEdit();
            });
        }
    }
}
