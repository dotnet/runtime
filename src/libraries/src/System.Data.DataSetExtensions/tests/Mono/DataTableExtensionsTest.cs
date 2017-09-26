// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// DataTableExtensionsTest.cs
//
// Author:
//   Atsushi Enomoto  <atsushi@ximian.com>
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
using System.Linq;
using Xunit;

namespace MonoTests.System.Data
{
    public class DataTableExtensionsTest
    {
        private string _testDataSet = "testdataset1.xml";

        [Fact]
        public void CopyToDataTableNoArgNoRows()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CID", typeof(int));
            dt.Columns.Add("CName", typeof(string));

            // for no rows
            Assert.Throws<InvalidOperationException>(() => dt.AsEnumerable().CopyToDataTable());
        }

        [Fact]
        public void CopyToDataTableNoArg()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CID", typeof(int));
            dt.Columns.Add("CName", typeof(string));
            dt.Rows.Add(new object[] { 1, "foo" });
            DataTable dst = dt.AsEnumerable().CopyToDataTable();
            Assert.Equal(1, dst.Rows.Count);
            Assert.Equal("foo", dst.Rows[0]["CName"]);
        }

        [Fact]
        public void CopyToDataTableTableArgNoRows()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("CID", typeof(int));
            dt.Columns.Add("CName", typeof(string));
            DataTable dst = new DataTable();
            dt.AsEnumerable().CopyToDataTable(dst, LoadOption.PreserveChanges);
        }

        [Fact]
        public void AsEnumerable()
        {
            DataSet ds = new DataSet();
            ds.ReadXml(_testDataSet);
            DataTable dt = ds.Tables[0];
            Assert.Equal("ScoreList", dt.TableName);
            var dv = dt.AsEnumerable();
            Assert.Equal(4, dv.Count());
            var i = dv.GetEnumerator();
            Assert.True(i.MoveNext(), "#1");
            Assert.Equal(1, i.Current["ID"]);
            Assert.True(i.MoveNext(), "#3");
            Assert.Equal(2, i.Current["ID"]);
        }
    }
}