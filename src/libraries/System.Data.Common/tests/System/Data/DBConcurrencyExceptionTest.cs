// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
    public class DBConcurrencyExceptionTest
    {
        [Fact] // .ctor ()
        public void Constructor1()
        {
            DBConcurrencyException dbce = new DBConcurrencyException();
            Assert.Null(dbce.InnerException);
            Assert.NotNull(dbce.Message);
            Assert.NotNull(dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);
        }

        [Fact] // .ctor (String)
        public void Constructor2()
        {
            DBConcurrencyException dbce;
            string msg = "MONO";

            dbce = new DBConcurrencyException(msg);
            Assert.Null(dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(null);
            Assert.Null(dbce.InnerException);
            Assert.NotNull(dbce.Message);
            Assert.True(dbce.Message.IndexOf(typeof(DBConcurrencyException).FullName) != -1);
            Assert.Null(dbce.Row);

            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(string.Empty);
            Assert.Null(dbce.InnerException);
            Assert.Equal(string.Empty, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);
        }

        [Fact] // .ctor (String, Exception)
        public void Constructor3()
        {
            Exception inner = new Exception();
            DBConcurrencyException dbce;
            string msg = "MONO";

            dbce = new DBConcurrencyException(msg, inner);
            Assert.Same(inner, dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(null, inner);
            Assert.Same(inner, dbce.InnerException);
            Assert.True(dbce.Message.IndexOf(typeof(DBConcurrencyException).FullName) != -1);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(string.Empty, inner);
            Assert.Same(inner, dbce.InnerException);
            Assert.Equal(string.Empty, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(msg, null);
            Assert.Null(dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            dbce = new DBConcurrencyException(null, null);
            Assert.Null(dbce.InnerException);
            Assert.True(dbce.Message.IndexOf(typeof(DBConcurrencyException).FullName) != -1);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);
        }

        [Fact] // .ctor (String, Exception, DataRow [])
        public void Constructor4()
        {
            DataTable dt = new DataTable();
            DataRow rowA = dt.NewRow();
            DataRow rowB = dt.NewRow();
            DataRow[] rows;
            Exception inner = new Exception();
            DBConcurrencyException dbce;
            string msg = "MONO";

            rows = new DataRow[] { rowA, null, rowB };
            dbce = new DBConcurrencyException(msg, inner, rows);
            Assert.Same(inner, dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Same(rowA, dbce.Row);
            Assert.Equal(3, dbce.RowCount);

            rows = new DataRow[] { rowB, rowA, null };
            dbce = new DBConcurrencyException(null, inner, rows);
            Assert.Same(inner, dbce.InnerException);
            Assert.True(dbce.Message.IndexOf(typeof(DBConcurrencyException).FullName) != -1);
            Assert.Same(rowB, dbce.Row);
            Assert.Equal(3, dbce.RowCount);

            rows = new DataRow[] { null, rowA };
            dbce = new DBConcurrencyException(string.Empty, inner, rows);
            Assert.Same(inner, dbce.InnerException);
            Assert.Equal(string.Empty, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(2, dbce.RowCount);

            rows = new DataRow[] { rowA };
            dbce = new DBConcurrencyException(msg, null, rows);
            Assert.Null(dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Same(rowA, dbce.Row);
            Assert.Equal(1, dbce.RowCount);

            rows = null;
            dbce = new DBConcurrencyException(msg, null, rows);
            Assert.Null(dbce.InnerException);
            Assert.Same(msg, dbce.Message);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);

            rows = null;
            dbce = new DBConcurrencyException(null, null, rows);
            Assert.Null(dbce.InnerException);
            Assert.True(dbce.Message.IndexOf(typeof(DBConcurrencyException).FullName) != -1);
            Assert.Null(dbce.Row);
            Assert.Equal(0, dbce.RowCount);
        }

        [Fact]
        public void Row()
        {
            DataTable dt = new DataTable();
            DataRow rowA = dt.NewRow();
            DataRow rowB = dt.NewRow();

            DBConcurrencyException dbce = new DBConcurrencyException();
            dbce.Row = rowA;
            Assert.Same(rowA, dbce.Row);
            Assert.Equal(1, dbce.RowCount);
            dbce.Row = rowB;
            Assert.Same(rowB, dbce.Row);
            Assert.Equal(1, dbce.RowCount);
            dbce.Row = null;
            Assert.Null(dbce.Row);
            Assert.Equal(1, dbce.RowCount);
        }
    }
}
