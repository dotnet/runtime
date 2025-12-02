// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// DataRowExtensionsTest.cs
//
// Author:
//   Marek Habersack (mhabersack@novell.com)
//
// Copyright (C) 2008 Novell, Inc. http://www.novell.com
//

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

using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace MonoTests.System.Data
{
    public class DataRowExtensionsTest
    {
        DataRow SetupRow()
        {
            DataTable dt = new DataTable("TestTable");
            DataColumn dc = new DataColumn("Column1", typeof(string));
            dc.AllowDBNull = true;
            dt.Columns.Add(dc);

            dc = new DataColumn("Column2", typeof(int));
            dc.AllowDBNull = true;
            dt.Columns.Add(dc);

            DataRow row = dt.NewRow();
            dt.Rows.Add(row);
            return row;
        }

        [Fact]
        public void Field_T_DBNullFieldValue()
        {
            DataRow row = SetupRow();
            row["Column1"] = null;
            row["Column2"] = DBNull.Value;

            string s = row.Field<string>("Column1");
            Assert.Null(s);

            int? i = row.Field<int?>("Column2");
            Assert.Null(i);
        }

        [Fact]
        public void Field_T_DBNullFieldValue_ValueType()
        {
            DataRow row = SetupRow();
            row["Column1"] = null;
            row["Column2"] = DBNull.Value;

            Assert.Throws<InvalidCastException>(() => row.Field<int>("Column2"));
        }
    }
}
