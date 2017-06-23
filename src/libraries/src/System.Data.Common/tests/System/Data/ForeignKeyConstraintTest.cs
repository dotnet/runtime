// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Franklin Wise
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
    public class ForeignKeyConstraintTest
    {
        private DataSet _ds;

        //NOTE: fk constraints only work when the table is part of a DataSet

        public ForeignKeyConstraintTest()
        {
            _ds = new DataSet();

            //Setup DataTable
            DataTable table;
            table = new DataTable("TestTable");
            table.Columns.Add("Col1", typeof(int));
            table.Columns.Add("Col2", typeof(int));
            table.Columns.Add("Col3", typeof(int));

            _ds.Tables.Add(table);

            table = new DataTable("TestTable2");
            table.Columns.Add("Col1", typeof(int));
            table.Columns.Add("Col2", typeof(int));
            table.Columns.Add("Col3", typeof(int));

            _ds.Tables.Add(table);
        }

        // Tests ctor (string, DataColumn, DataColumn)
        [Fact]
        public void Ctor1()
        {
            DataTable Table = _ds.Tables[0];

            Assert.Equal(0, Table.Constraints.Count);
            Table = _ds.Tables[1];
            Assert.Equal(0, Table.Constraints.Count);

            // ctor (string, DataColumn, DataColumn
            ForeignKeyConstraint Constraint = new ForeignKeyConstraint("test", _ds.Tables[0].Columns[2], _ds.Tables[1].Columns[0]);
            Table = _ds.Tables[1];
            Table.Constraints.Add(Constraint);

            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("test", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(ForeignKeyConstraint), Table.Constraints[0].GetType());

            Table = _ds.Tables[0];
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(UniqueConstraint), Table.Constraints[0].GetType());
        }

        // Tests ctor (DataColumn, DataColumn)
        [Fact]
        public void Ctor2()
        {
            DataTable Table = _ds.Tables[0];

            Assert.Equal(0, Table.Constraints.Count);
            Table = _ds.Tables[1];
            Assert.Equal(0, Table.Constraints.Count);

            // ctor (string, DataColumn, DataColumn
            ForeignKeyConstraint Constraint = new ForeignKeyConstraint(_ds.Tables[0].Columns[2], _ds.Tables[1].Columns[0]);
            Table = _ds.Tables[1];
            Table.Constraints.Add(Constraint);

            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(ForeignKeyConstraint), Table.Constraints[0].GetType());

            Table = _ds.Tables[0];
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(UniqueConstraint), Table.Constraints[0].GetType());
        }

        // Test ctor (DataColumn [], DataColumn [])
        [Fact]
        public void Ctor3()
        {
            DataTable Table = _ds.Tables[0];

            Assert.Equal(0, Table.Constraints.Count);
            Table = _ds.Tables[1];
            Assert.Equal(0, Table.Constraints.Count);

            DataColumn[] Cols1 = new DataColumn[2];
            Cols1[0] = _ds.Tables[0].Columns[1];
            Cols1[1] = _ds.Tables[0].Columns[2];

            DataColumn[] Cols2 = new DataColumn[2];
            Cols2[0] = _ds.Tables[1].Columns[0];
            Cols2[1] = _ds.Tables[1].Columns[1];

            ForeignKeyConstraint Constraint = new ForeignKeyConstraint(Cols1, Cols2);
            Table = _ds.Tables[1];
            Table.Constraints.Add(Constraint);

            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(ForeignKeyConstraint), Table.Constraints[0].GetType());

            Table = _ds.Tables[0];
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(UniqueConstraint), Table.Constraints[0].GetType());
        }

        // Tests ctor (string, DataColumn [], DataColumn [])	
        [Fact]
        public void Ctor4()
        {
            DataTable Table = _ds.Tables[0];

            Assert.Equal(0, Table.Constraints.Count);
            Table = _ds.Tables[1];
            Assert.Equal(0, Table.Constraints.Count);

            DataColumn[] Cols1 = new DataColumn[2];
            Cols1[0] = _ds.Tables[0].Columns[1];
            Cols1[1] = _ds.Tables[0].Columns[2];

            DataColumn[] Cols2 = new DataColumn[2];
            Cols2[0] = _ds.Tables[1].Columns[0];
            Cols2[1] = _ds.Tables[1].Columns[1];

            ForeignKeyConstraint Constraint = new ForeignKeyConstraint("Test", Cols1, Cols2);
            Table = _ds.Tables[1];
            Table.Constraints.Add(Constraint);

            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Test", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(ForeignKeyConstraint), Table.Constraints[0].GetType());

            Table = _ds.Tables[0];
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(UniqueConstraint), Table.Constraints[0].GetType());
        }

        [Fact]
        public void TestCtor5()
        {
            DataTable table1 = new DataTable("Table1");
            DataTable table2 = new DataTable("Table2");
            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(table1);
            dataSet.Tables.Add(table2);
            DataColumn column1 = new DataColumn("col1");
            DataColumn column2 = new DataColumn("col2");
            DataColumn column3 = new DataColumn("col3");
            table1.Columns.Add(column1);
            table1.Columns.Add(column2);
            table1.Columns.Add(column3);
            DataColumn column4 = new DataColumn("col4");
            DataColumn column5 = new DataColumn("col5");
            DataColumn column6 = new DataColumn("col6");
            table2.Columns.Add(column4);
            table2.Columns.Add(column5);
            table2.Columns.Add(column6);
            string[] parentColumnNames = { "col1", "col2", "col3" };
            string[] childColumnNames = { "col4", "col5", "col6" };
            string parentTableName = "table1";

            // Create a ForeingKeyConstraint Object using the constructor
            // ForeignKeyConstraint (string, string, string[], string[], AcceptRejectRule, Rule, Rule);
            ForeignKeyConstraint fkc = new ForeignKeyConstraint("hello world", parentTableName, parentColumnNames, childColumnNames, AcceptRejectRule.Cascade, Rule.Cascade, Rule.Cascade);                                                                                                                            // Assert that the Constraint object does not belong to any table yet
            try
            {
                DataTable tmp = fkc.Table;
                Assert.False(true);
            }
            catch (NullReferenceException)
            { // actually .NET throws this (bug)
            }
            catch (InvalidOperationException)
            {
            }

            Constraint[] constraints = new Constraint[3];
            constraints[0] = new UniqueConstraint(column1);
            constraints[1] = new UniqueConstraint(column2);
            constraints[2] = fkc;

            // Try to add the constraint to ConstraintCollection of the DataTable through Add()
            try
            {
                table2.Constraints.Add(fkc);
                throw new ApplicationException("An Exception was expected");
            }
            // LAMESPEC : spec says InvalidConstraintException but throws this
            catch (NullReferenceException)
            {
            }

#if false // FIXME: Here this test crashes under MS.NET.
                        // OK - So AddRange() is the only way!
                        table2.Constraints.AddRange (constraints);
			   // After AddRange(), Check the properties of ForeignKeyConstraint object
                        Assert.True(fkc.RelatedColumns [0].ColumnName.Equals ("col1"));
                        Assert.True(fkc.RelatedColumns [1].ColumnName.Equals ("col2"));
                        Assert.True(fkc.RelatedColumns [2].ColumnName.Equals ("col3"));
                        Assert.True(fkc.Columns [0].ColumnName.Equals ("col4"));
                        Assert.True(fkc.Columns [1].ColumnName.Equals ("col5"));
                        Assert.True(fkc.Columns [2].ColumnName.Equals ("col6"));
#endif
            // Try to add columns with names which do not exist in the table
            parentColumnNames[2] = "noColumn";
            ForeignKeyConstraint foreignKeyConstraint = new ForeignKeyConstraint("hello world", parentTableName, parentColumnNames, childColumnNames, AcceptRejectRule.Cascade, Rule.Cascade, Rule.Cascade);
            constraints[0] = new UniqueConstraint(column1);
            constraints[1] = new UniqueConstraint(column2);
            constraints[2] = foreignKeyConstraint;
            try
            {
                table2.Constraints.AddRange(constraints);
                throw new ApplicationException("An Exception was expected");
            }
            catch (ArgumentException e)
            {
            }
            catch (InvalidConstraintException e)
            { // Could not test on ms.net, as ms.net does not reach here so far.        
            }

#if false // FIXME: Here this test crashes under MS.NET.
                        // Check whether the child table really contains the foreign key constraint named "hello world"
                        Assert.True(table2.Constraints.Contains ("hello world"));
#endif
        }



        //  If Childs and parents are in same table
        [Fact]
        public void KeyBetweenColumns()
        {
            DataTable Table = _ds.Tables[0];

            Assert.Equal(0, Table.Constraints.Count);
            Table = _ds.Tables[1];
            Assert.Equal(0, Table.Constraints.Count);


            ForeignKeyConstraint Constraint = new ForeignKeyConstraint("Test", _ds.Tables[0].Columns[0], _ds.Tables[0].Columns[2]);
            Table = _ds.Tables[0];
            Table.Constraints.Add(Constraint);

            Assert.Equal(2, Table.Constraints.Count);
            Assert.Equal("Constraint1", Table.Constraints[0].ConstraintName);
            Assert.Equal(typeof(UniqueConstraint), Table.Constraints[0].GetType());
            Assert.Equal("Test", Table.Constraints[1].ConstraintName);
            Assert.Equal(typeof(ForeignKeyConstraint), Table.Constraints[1].GetType());
        }

        [Fact]
        public void CtorExceptions()
        {
            ForeignKeyConstraint fkc;

            DataTable localTable = new DataTable();
            localTable.Columns.Add("Col1", typeof(int));
            localTable.Columns.Add("Col2", typeof(bool));

            //Null
            Assert.Throws<NullReferenceException>(() =>
            {
                fkc = new ForeignKeyConstraint(null, (DataColumn)null);
            });

            //zero length collection
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                fkc = new ForeignKeyConstraint(new DataColumn[] { }, new DataColumn[] { });
            });

            //different datasets
            Assert.Throws<InvalidOperationException>(() =>
            {
                fkc = new ForeignKeyConstraint(_ds.Tables[0].Columns[0], localTable.Columns[0]);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                fkc = new ForeignKeyConstraint(_ds.Tables[0].Columns[0], localTable.Columns[1]);
            });

            // Cannot create a Key from Columns that belong to
            // different tables.
            Assert.Throws<InvalidConstraintException>(() =>
            {
                fkc = new ForeignKeyConstraint(new DataColumn[] { _ds.Tables[0].Columns[0], _ds.Tables[0].Columns[1] }, new DataColumn[] { localTable.Columns[1], _ds.Tables[1].Columns[0] });
            });
        }

        [Fact]
        public void CtorExceptions2()
        {
            DataColumn col = new DataColumn("MyCol1", typeof(int));

            ForeignKeyConstraint fkc;

            //Columns must belong to a Table
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                fkc = new ForeignKeyConstraint(col, _ds.Tables[0].Columns[0]);
            });

            //Columns must belong to the same table
            //InvalidConstraintException

            DataColumn[] difTable = new DataColumn[] {_ds.Tables[0].Columns[2],
                                       _ds.Tables[1].Columns[0]};
            Assert.Throws<InvalidConstraintException>(() =>
            {
                fkc = new ForeignKeyConstraint(difTable, new DataColumn[] {
                                 _ds.Tables[0].Columns[1],
                                _ds.Tables[0].Columns[0]});
            });

            //parent columns and child columns should be the same length
            //ArgumentException
            DataColumn[] twoCol =
                new DataColumn[] { _ds.Tables[0].Columns[0], _ds.Tables[0].Columns[1] };


            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                fkc = new ForeignKeyConstraint(twoCol,
                    new DataColumn[] { _ds.Tables[0].Columns[0] });
            });

            //InvalidOperation: Parent and child are the same column.
            Assert.Throws<InvalidOperationException>(() =>
            {
                fkc = new ForeignKeyConstraint(_ds.Tables[0].Columns[0],
                    _ds.Tables[0].Columns[0]);
            });
        }

        [Fact]
        public void EqualsAndHashCode()
        {
            DataTable tbl = _ds.Tables[0];
            DataTable tbl2 = _ds.Tables[1];

            ForeignKeyConstraint fkc = new ForeignKeyConstraint(
                new DataColumn[] { tbl.Columns[0], tbl.Columns[1] },
                new DataColumn[] { tbl2.Columns[0], tbl2.Columns[1] });

            ForeignKeyConstraint fkc2 = new ForeignKeyConstraint(
                new DataColumn[] { tbl.Columns[0], tbl.Columns[1] },
                new DataColumn[] { tbl2.Columns[0], tbl2.Columns[1] });

            ForeignKeyConstraint fkcDiff =
                new ForeignKeyConstraint(tbl.Columns[1], tbl.Columns[2]);

            Assert.True(fkc.Equals(fkc2));
            Assert.True(fkc2.Equals(fkc));
            Assert.True(fkc.Equals(fkc));

            Assert.True(fkc.Equals(fkcDiff) == false);

            //Assert.True( "Hash Code Assert.True.Failed. 1");
            Assert.True(fkc.GetHashCode() != fkcDiff.GetHashCode());
        }

        [Fact]
        public void ViolationTest()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTable parent = _ds.Tables[0];
                DataTable child = _ds.Tables[1];

                parent.Rows.Add(new object[] { 1, 1, 1 });
                child.Rows.Add(new object[] { 2, 2, 2 });

                try
                {
                    child.Constraints.Add(new ForeignKeyConstraint(parent.Columns[0],
                                              child.Columns[0])
                                   );
                }
                finally
                {
                    // clear the rows for further testing
                    _ds.Clear();
                }
            });
        }

        [Fact]
        public void NoViolationTest()
        {
            DataTable parent = _ds.Tables[0];
            DataTable child = _ds.Tables[1];

            parent.Rows.Add(new object[] { 1, 1, 1 });
            child.Rows.Add(new object[] { 2, 2, 2 });

            try
            {
                _ds.EnforceConstraints = false;
                child.Constraints.Add(new ForeignKeyConstraint(parent.Columns[0],
                                          child.Columns[0])
                               );
            }
            finally
            {
                // clear the rows for further testing
                _ds.Clear();
                _ds.EnforceConstraints = true;
            }
        }

        [Fact]
        public void ModifyParentKeyBeforeAcceptChanges()
        {
            DataSet ds1 = new DataSet();
            DataTable t1 = ds1.Tables.Add("t1");
            DataTable t2 = ds1.Tables.Add("t2");
            t1.Columns.Add("col1", typeof(int));
            t2.Columns.Add("col2", typeof(int));
            ds1.Relations.Add("fk", t1.Columns[0], t2.Columns[0]);

            t1.Rows.Add(new object[] { 10 });
            t2.Rows.Add(new object[] { 10 });

            t1.Rows[0][0] = 20;
            Assert.True((int)t2.Rows[0][0] == 20);
        }

        [Fact]
        // https://bugzilla.novell.com/show_bug.cgi?id=650402
        public void ForeignKey_650402()
        {
            DataSet data = new DataSet();
            DataTable parent = new DataTable("parent");
            DataColumn pk = parent.Columns.Add("PK");
            DataTable child = new DataTable("child");
            DataColumn fk = child.Columns.Add("FK");

            data.Tables.Add(parent);
            data.Tables.Add(child);
            data.Relations.Add(pk, fk);

            parent.Rows.Add("value");
            child.Rows.Add("value");
            data.AcceptChanges();
            child.Rows[0].Delete();
            parent.Rows[0][0] = "value2";

            data.EnforceConstraints = false;
            data.EnforceConstraints = true;
        }
    }
}
