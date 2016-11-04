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
    public class RowNotInTableExceptionTest
    {
        [Fact]
        public void Generate()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Relations.Add(new DataRelation("myRelation", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]));

            DataRow drParent = ds.Tables[0].Rows[0];
            DataRow drChild = ds.Tables[1].Rows[0];
            drParent.Delete();
            drChild.Delete();
            ds.AcceptChanges();

            // RowNotInTableException - AcceptChanges
            Assert.Throws<RowNotInTableException>(() =>
            {
                drParent.AcceptChanges();
            });

            // RowNotInTableException - GetChildRows
            Assert.Throws<RowNotInTableException>(() =>
            {
                drParent.GetChildRows("myRelation");
            });

            // RowNotInTableException - ItemArray
            Assert.Throws<RowNotInTableException>(() => drParent.ItemArray);

            // RowNotInTableException - GetParentRows
            Assert.Throws<RowNotInTableException>(() => drChild.GetParentRows("myRelation"));

            // RowNotInTableException - RejectChanges
            Assert.Throws<RowNotInTableException>(() => drParent.RejectChanges());

            // RowNotInTableException - SetParentRow
            Assert.Throws<RowNotInTableException>(() => drChild.SetParentRow(ds.Tables[0].Rows[1]));
        }
    }
}
