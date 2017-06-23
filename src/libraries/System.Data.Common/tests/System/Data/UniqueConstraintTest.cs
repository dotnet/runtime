// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2002 Franklin Wise
// (C) 2003 Martin Willemoes Hansen

// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using Xunit;

namespace System.Data.Tests
{
    public class UniqueConstraintTest
    {
        private DataTable _table;

        public UniqueConstraintTest()
        {
            //Setup DataTable
            _table = new DataTable("TestTable");
            _table.Columns.Add("Col1", typeof(int));
            _table.Columns.Add("Col2", typeof(int));
            _table.Columns.Add("Col3", typeof(int));
        }

        [Fact]
        public void CtorExceptions()
        {
            //UniqueConstraint(string name, DataColumn column, bool isPrimaryKey)

            UniqueConstraint cst;

            //must have DataTable exception
            AssertExtensions.Throws<ArgumentException>(null, () => new UniqueConstraint(new DataColumn("")));

            //Null exception
            Assert.Throws<NullReferenceException>(() => new UniqueConstraint((DataColumn)null));

            Assert.Throws<InvalidConstraintException>(() => new UniqueConstraint(new DataColumn[] { }));

            DataTable dt = new DataTable("Table1");
            dt.Columns.Add("Col1", typeof(int));
            DataTable dt2 = new DataTable("Table2");
            dt2.Columns.Add("Col1", typeof(int));

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt2);

            //columns from two different tables.
            Assert.Throws<InvalidConstraintException>(() => new UniqueConstraint(new DataColumn[] { dt.Columns[0], dt2.Columns[0] }));
        }

        [Fact]
        public void Ctor()
        {
            UniqueConstraint cst;

            //Success case
            cst = new UniqueConstraint(_table.Columns[0]);

            cst = new UniqueConstraint(new DataColumn[] {
                        _table.Columns[0], _table.Columns[1]});

            //table is set on ctor
            cst = new UniqueConstraint(_table.Columns[0]);

            Assert.Same(_table, cst.Table);

            //table is set on ctor
            cst = new UniqueConstraint(new DataColumn[] {
                      _table.Columns[0], _table.Columns[1]});
            Assert.Same(_table, cst.Table);

            cst = new UniqueConstraint("MyName", _table.Columns[0], true);

            //Test ctor parm set for ConstraintName & IsPrimaryKey
            Assert.Equal("MyName", cst.ConstraintName);
            Assert.False(cst.IsPrimaryKey);

            _table.Constraints.Add(cst);

            Assert.True(cst.IsPrimaryKey);

            Assert.Equal(1, _table.PrimaryKey.Length);
            Assert.True(_table.PrimaryKey[0].Unique);
        }

        [Fact]
        public void Unique()
        {
            UniqueConstraint U = new UniqueConstraint(_table.Columns[0]);
            Assert.False(_table.Columns[0].Unique);

            U = new UniqueConstraint(new DataColumn[] { _table.Columns[0], _table.Columns[1] });
            Assert.False(_table.Columns[0].Unique);
            Assert.False(_table.Columns[1].Unique);
            Assert.False(_table.Columns[2].Unique);

            _table.Constraints.Add(U);
            Assert.False(_table.Columns[0].Unique);
            Assert.False(_table.Columns[1].Unique);
            Assert.False(_table.Columns[2].Unique);
        }

        [Fact]
        public void EqualsAndHashCode()
        {
            UniqueConstraint cst = new UniqueConstraint(new DataColumn[] {
                    _table.Columns[0], _table.Columns[1]});
            UniqueConstraint cst2 = new UniqueConstraint(new DataColumn[] {
                     _table.Columns[1], _table.Columns[0]});

            UniqueConstraint cst3 = new UniqueConstraint(_table.Columns[0]);
            UniqueConstraint cst4 = new UniqueConstraint(_table.Columns[2]);

            //true
            Assert.True(cst.Equals(cst2));

            //false
            Assert.False(cst.Equals(23));
            Assert.False(cst.Equals(cst3));
            Assert.False(cst3.Equals(cst));
            Assert.False(cst.Equals(cst4));

            Assert.NotEqual(cst3.GetHashCode(), cst.GetHashCode());
        }

        [Fact]
        public void DBNullAllowed()
        {
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            dt.Constraints.Add(new UniqueConstraint(dt.Columns[0]));
            dt.Rows.Add(new object[] { 1, 3 });
            dt.Rows.Add(new object[] { DBNull.Value, 3 });
        }
    }
}
