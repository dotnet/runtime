// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Franklin Wise
// (C) 2003 Martin Willemoes Hansen
//
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
    public class ConstraintCollectionTest
    {
        private DataTable _table;
        private DataTable _table2;
        private Constraint _constraint1;
        private Constraint _constraint2;

        public ConstraintCollectionTest()
        {
            //Setup DataTable
            _table = new DataTable("TestTable");
            _table.Columns.Add("Col1", typeof(int));
            _table.Columns.Add("Col2", typeof(int));
            _table.Columns.Add("Col3", typeof(int));

            _table2 = new DataTable("TestTable");
            _table2.Columns.Add("Col1", typeof(int));
            _table2.Columns.Add("Col2", typeof(int));

            //Use UniqueConstraint to test Constraint Base Class
            _constraint1 = new UniqueConstraint(_table.Columns[0], false);
            _constraint2 = new UniqueConstraint(_table.Columns[1], false);

            // not sure why this is needed since a new _table was just created
            // for us, but this Clear() keeps the tests from throwing
            // an exception when the Add() is called.
            _table.Constraints.Clear();
        }

        [Fact]
        public void Add()
        {
            ConstraintCollection col = _table.Constraints;
            col.Add(_constraint1);
            col.Add(_constraint2);

            Assert.Equal(2, col.Count);
        }

        [Fact]
        public void AddExceptions()
        {
            ConstraintCollection col = _table.Constraints;
            Assert.Throws<ArgumentNullException>(() => col.Add(null));

            _constraint1.ConstraintName = "Dog";
            col.Add(_constraint1);
            Assert.Throws<DataException>(() => col.Add(_constraint1));
        }

        [Fact]
        public void Indexer()
        {
            var c1 = new UniqueConstraint(_table.Columns[0]) { ConstraintName = "first" };
            var c2 = new UniqueConstraint(_table.Columns[1]) { ConstraintName = "second" };

            _table.Constraints.Add(c1);
            _table.Constraints.Add(c2);

            Assert.Same(c1, _table.Constraints[0]);
            Assert.Same(c2, _table.Constraints[1]);

            Assert.Same(c1, _table.Constraints["first"]);
            Assert.Same(c2, _table.Constraints["sEcond"]); // case insensitive
        }

        [Fact]
        public void IndexOf()
        {
            var c1 = new UniqueConstraint(_table.Columns[0]) { ConstraintName = "first" };
            var c2 = new UniqueConstraint(_table.Columns[1]) { ConstraintName = "second" };

            _table.Constraints.Add(c1);
            _table.Constraints.Add(c2);

            Assert.Equal(0, _table.Constraints.IndexOf(c1));
            Assert.Equal(1, _table.Constraints.IndexOf(c2));
            Assert.Equal(0, _table.Constraints.IndexOf("first"));
            Assert.Equal(1, _table.Constraints.IndexOf("second"));
        }

        [Fact]
        public void Contains()
        {
            var c1 = new UniqueConstraint(_table.Columns[0]) { ConstraintName = "first" };
            var c2 = new UniqueConstraint(_table.Columns[1]) { ConstraintName = "second" };

            _table.Constraints.Add(c1);

            Assert.True(_table.Constraints.Contains(c1.ConstraintName));
            Assert.False(_table.Constraints.Contains(c2.ConstraintName));
        }

        [Fact]
        public void IndexerFailures()
        {
            _table.Constraints.Add(new UniqueConstraint(_table.Columns[0]));
            Assert.Null(_table.Constraints["notInCollection"]);
            Assert.Throws<IndexOutOfRangeException>(() => _table.Constraints[_table.Constraints.Count]);
            Assert.Throws<IndexOutOfRangeException>(() => _table.Constraints[-1]);
        }

        [Fact]
        public void AddFkException1()
        {
            var ds = new DataSet();
            ds.Tables.Add(_table);
            _table2.TableName = "TestTable2";
            ds.Tables.Add(_table2);

            _table.Rows.Add(new object[] { 1 });
            _table.Rows.Add(new object[] { 1 });

            //FKC: can't create unique constraint because duplicate values already exist
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                var fkc = new ForeignKeyConstraint(_table.Columns[0], _table2.Columns[0]);
                _table2.Constraints.Add(fkc);
            });
        }

        [Fact]
        public void AddFkException2()
        {
            //Foreign key rules only work when the tables
            //are apart of the dataset
            var ds = new DataSet();
            ds.Tables.Add(_table);
            _table2.TableName = "TestTable2";
            ds.Tables.Add(_table2);

            _table.Rows.Add(new object[] { 1 });
            _table2.Rows.Add(new object[] { 3 });

            var fkc = new ForeignKeyConstraint(_table.Columns[0], _table2.Columns[0]);
            AssertExtensions.Throws<ArgumentException>(null, () => _table2.Constraints.Add(fkc));
        }

        [Fact]
        public void AddUniqueExceptions()
        {
            //UC: can't create unique constraint because duplicate values already exist
            _table.Rows.Add(new object[] { 1 });
            _table.Rows.Add(new object[] { 1 });
            var uc = new UniqueConstraint(_table.Columns[0]);
            AssertExtensions.Throws<ArgumentException>(null, () => _table.Constraints.Add(uc));
        }

        [Fact]
        public void AddRange()
        {
            _constraint1.ConstraintName = "UK1";
            _constraint2.ConstraintName = "UK12";

            var _constraint3 = new ForeignKeyConstraint("FK2", _table.Columns[0], _table2.Columns[0]);
            var _constraint4 = new UniqueConstraint("UK2", _table2.Columns[1]);

            // Add the constraints.
            Constraint[] constraints = { _constraint1, _constraint2 };
            _table.Constraints.AddRange(constraints);

            Constraint[] constraints1 = { _constraint3, _constraint4 };
            _table2.Constraints.AddRange(constraints1);

            Assert.Equal("UK1", _table.Constraints[0].ConstraintName);
            Assert.Equal("UK12", _table.Constraints[1].ConstraintName);
            Assert.Equal("FK2", _table2.Constraints[0].ConstraintName);
            Assert.Equal("UK2", _table2.Constraints[1].ConstraintName);
        }

        [Fact] // "Even after EndInit(), .NET does not fill Table property on UniqueConstraint.")]
        public void TestAddRange2()
        {
            var table = new DataTable("Table");
            var column1 = new DataColumn("col1");
            var column2 = new DataColumn("col2");
            var column3 = new DataColumn("col3");
            table.Columns.Add(column1);
            table.Columns.Add(column2);
            table.Columns.Add(column3);
            string[] columnNames = { "col1", "col2", "col3" };

            var constraints = new Constraint[3];
            constraints[0] = new UniqueConstraint("Unique1", column1);
            constraints[1] = new UniqueConstraint("Unique2", column2);
            constraints[2] = new UniqueConstraint("Unique3", columnNames, true);

            table.BeginInit();
            table.Constraints.AddRange(constraints);

            //Check the table property of UniqueConstraint Object
            try
            {
                Assert.Null(constraints[2].Table);
            }
            catch (NullReferenceException) { }

            table.EndInit();
        }

        [Fact]
        public void Clear()
        {
            //try
            //{
            _table.Constraints.Clear(); //Clear all constraints
            Assert.Equal(0, _table.Constraints.Count);
            _table2.Constraints.Clear();
            Assert.Equal(0, _table2.Constraints.Count);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
        }

        [Fact]
        public void RemoveExceptions()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _table.Constraints.Remove(_table.Constraints[0]));
        }
    }
}
