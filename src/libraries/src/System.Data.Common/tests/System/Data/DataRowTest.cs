// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Copyright 2002 Franklin Wise
// (C) Copyright 2003 Daniel Morgan
// (C) Copyright 2003 Martin Willemoes Hansen
// 

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
//


using Xunit;

namespace System.Data.Tests
{
    public class DataRowTest
    {
        private DataTable _table;
        private DataRow _row;

        public DataRowTest()
        {
            _table = MakeTable();
            _row = _table.NewRow();
            _row["FName"] = "Hello";
            _row["LName"] = "World";
            _table.Rows.Add(_row);
        }

        private DataTable MakeTable()
        {
            DataTable namesTable = new DataTable("Names");
            DataColumn idColumn = new DataColumn();


            idColumn.DataType = Type.GetType("System.Int32");
            idColumn.ColumnName = "Id";
            idColumn.AutoIncrement = true;
            namesTable.Columns.Add(idColumn);


            DataColumn fNameColumn = new DataColumn();
            fNameColumn.DataType = Type.GetType("System.String");
            fNameColumn.ColumnName = "Fname";
            fNameColumn.DefaultValue = "Fname";
            namesTable.Columns.Add(fNameColumn);

            DataColumn lNameColumn = new DataColumn();
            lNameColumn.DataType = Type.GetType("System.String");
            lNameColumn.ColumnName = "LName";
            lNameColumn.DefaultValue = "LName";
            namesTable.Columns.Add(lNameColumn);


            // Set the primary key for the table
            DataColumn[] keys = new DataColumn[1];
            keys[0] = idColumn;
            namesTable.PrimaryKey = keys;
            // Return the new DataTable.
            return namesTable;
        }

        [Fact]
        public void SetColumnErrorTest()
        {
            string errorString;
            errorString = "Some error!";
            // Set the error for the specified column of the row.
            _row.SetColumnError(1, errorString);
            GetColumnErrorTest();
            GetAllErrorsTest();
        }

        private void GetColumnErrorTest()
        {
            // Print the error of a specified column.
            Assert.Equal("Some error!", _row.GetColumnError(1));
        }

        private void GetAllErrorsTest()
        {
            DataColumn[] colArr;

            if (_row.HasErrors)
            {
                colArr = _row.GetColumnsInError();

                for (int i = 0; i < colArr.Length; i++)
                {
                    Assert.Equal(_table.Columns[1], colArr[i]);
                }
                _row.ClearErrors();
            }
        }

        [Fact]
        public void DeleteRowTest()
        {
            DataRow newRow;


            for (int i = 1; i <= 2; i++)
            {
                newRow = _table.NewRow();
                newRow["FName"] = "Name " + i;
                newRow["LName"] = " Last Name" + i;
                _table.Rows.Add(newRow);
            }
            _table.AcceptChanges();

            int cnt = 1;
            for (int i = 1; i < _table.Rows.Count; i++)
            {
                DataRow r = _table.Rows[i];
                Assert.Equal("Name " + cnt, r["fName"]);
                cnt++;
            }


            // Create a DataView with the table.
            DataRowCollection rc = _table.Rows;
            rc[0].Delete();
            rc[2].Delete();


            Assert.Equal("Deleted", rc[0].RowState.ToString());
            Assert.Equal("Deleted", rc[2].RowState.ToString());


            // Accept changes
            _table.AcceptChanges();
            Assert.Equal("Name 1", (_table.Rows[0])[1]);
            try
            {
                object o = rc[2];
                Assert.False(true);
            }
            catch (Exception e)
            {
                // Never premise English.
                //Assert.Equal ("#A08", "There is no row at position 2.");
            }
        }

        [Fact]
        public void ParentRowTest()
        {
            //Clear all existing values from table
            for (int i = 0; i < _table.Rows.Count; i++)
            {
                _table.Rows[i].Delete();
            }
            _table.AcceptChanges();
            _row = _table.NewRow();
            _row["FName"] = "My FName";
            _row["Id"] = 0;
            _table.Rows.Add(_row);

            DataTable tableC = new DataTable("Child");
            DataColumn colC;
            DataRow rowC;

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            colC.AutoIncrement = true;
            tableC.Columns.Add(colC);


            colC = new DataColumn();
            colC.DataType = Type.GetType("System.String");
            colC.ColumnName = "Name";
            tableC.Columns.Add(colC);

            rowC = tableC.NewRow();
            rowC["Name"] = "My FName";
            tableC.Rows.Add(rowC);
            var ds = new DataSet();
            ds.Tables.Add(_table);
            ds.Tables.Add(tableC);
            DataRelation dr = new DataRelation("PO", _table.Columns["Id"], tableC.Columns["Id"]);
            ds.Relations.Add(dr);

            rowC.SetParentRow(_table.Rows[0], dr);

            Assert.Equal(_table.Rows[0], (tableC.Rows[0]).GetParentRow(dr));
            Assert.Equal(tableC.Rows[0], (_table.Rows[0]).GetChildRows(dr)[0]);

            ds.Relations.Clear();
            dr = new DataRelation("PO", _table.Columns["Id"], tableC.Columns["Id"], false);
            ds.Relations.Add(dr);
            rowC.SetParentRow(_table.Rows[0], dr);
            Assert.Equal(_table.Rows[0], (tableC.Rows[0]).GetParentRow(dr));
            Assert.Equal(tableC.Rows[0], (_table.Rows[0]).GetChildRows(dr)[0]);

            ds.Relations.Clear();
            dr = new DataRelation("PO", _table.Columns["Id"], tableC.Columns["Id"], false);
            tableC.ParentRelations.Add(dr);
            rowC.SetParentRow(_table.Rows[0]);
            Assert.Equal(_table.Rows[0], (tableC.Rows[0]).GetParentRow(dr));
            Assert.Equal(tableC.Rows[0], (_table.Rows[0]).GetChildRows(dr)[0]);
        }

        [Fact]
        public void ParentRowTest2()
        {
            var ds = new DataSet();
            DataTable tableP = ds.Tables.Add("Parent");
            DataTable tableC = ds.Tables.Add("Child");
            DataColumn colC;
            DataRow rowC;

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            colC.AutoIncrement = true;
            tableP.Columns.Add(colC);

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            tableC.Columns.Add(colC);

            _row = tableP.Rows.Add(new object[0]);
            rowC = tableC.NewRow();

            ds.EnforceConstraints = false;
            DataRelation dr = new DataRelation("PO", tableP.Columns["Id"], tableC.Columns["Id"]);
            ds.Relations.Add(dr);

            rowC.SetParentRow(_row, dr);
            DataRow[] rows = rowC.GetParentRows(dr);

            Assert.Equal(1, rows.Length);
            Assert.Equal(tableP.Rows[0], rows[0]);

            try
            {
                rows = _row.GetParentRows(dr);
            }
            catch (InvalidConstraintException)
            {
                //Test done
                return;
            }
            catch (Exception e)
            {
                Assert.False(true);
            }
        }

        [Fact]
        public void ChildRowTest()
        {
            //Clear all existing values from table
            for (int i = 0; i < _table.Rows.Count; i++)
            {
                _table.Rows[i].Delete();
            }
            _table.AcceptChanges();
            _row = _table.NewRow();
            _row["FName"] = "My FName";
            _row["Id"] = 0;
            _table.Rows.Add(_row);

            DataTable tableC = new DataTable("Child");
            DataColumn colC;
            DataRow rowC;

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            colC.AutoIncrement = true;
            tableC.Columns.Add(colC);

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.String");
            colC.ColumnName = "Name";
            tableC.Columns.Add(colC);

            rowC = tableC.NewRow();
            rowC["Name"] = "My FName";
            tableC.Rows.Add(rowC);
            var ds = new DataSet();
            ds.Tables.Add(_table);
            ds.Tables.Add(tableC);
            DataRelation dr = new DataRelation("PO", _table.Columns["Id"], tableC.Columns["Id"]);
            ds.Relations.Add(dr);

            rowC.SetParentRow(_table.Rows[0], dr);

            DataRow[] rows = (_table.Rows[0]).GetChildRows(dr);

            Assert.Equal(1, rows.Length);
            Assert.Equal(tableC.Rows[0], rows[0]);
        }

        [Fact]
        public void ChildRowTest2()
        {
            var ds = new DataSet();
            DataTable tableP = ds.Tables.Add("Parent");
            DataTable tableC = ds.Tables.Add("Child");
            DataColumn colC;
            DataRow rowC;

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            colC.AutoIncrement = true;
            tableP.Columns.Add(colC);

            colC = new DataColumn();
            colC.DataType = Type.GetType("System.Int32");
            colC.ColumnName = "Id";
            tableC.Columns.Add(colC);

            _row = tableP.NewRow();
            rowC = tableC.Rows.Add(new object[0]);

            ds.EnforceConstraints = false;
            DataRelation dr = new DataRelation("PO", tableP.Columns["Id"], tableC.Columns["Id"]);
            ds.Relations.Add(dr);

            rowC.SetParentRow(_row, dr);
            DataRow[] rows = _row.GetChildRows(dr);

            Assert.Equal(1, rows.Length);
            Assert.Equal(tableC.Rows[0], rows[0]);

            try
            {
                rows = rowC.GetChildRows(dr);
            }
            catch (InvalidConstraintException)
            {
                //Test done
                return;
            }
            catch (Exception e)
            {
                Assert.False(true);
            }
        }

        // tests item at row, column in table to be DBNull.Value
        private void DBNullTest(string message, DataTable dt, int row, int column)
        {
            object val = dt.Rows[row].ItemArray[column];
            Assert.Equal(DBNull.Value, val);
        }

        // tests item at row, column in table to be null
        private void NullTest(string message, DataTable dt, int row, int column)
        {
            object val = dt.Rows[row].ItemArray[column];
            Assert.Equal(null, val);
        }

        // tests item at row, column in table to be 
        private void ValueTest(string message, DataTable dt, int row, int column, object value)
        {
            object val = dt.Rows[row].ItemArray[column];
            Assert.Equal(value, val);
        }

        // test set null, DBNull.Value, and ItemArray short count
        [Fact]
        public void NullInItemArray()
        {
            string zero = "zero";
            string one = "one";
            string two = "two";

            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn(zero, typeof(string)));
            table.Columns.Add(new DataColumn(one, typeof(string)));
            table.Columns.Add(new DataColumn(two, typeof(string)));

            object[] obj = new object[3];
            // -- normal -----------------
            obj[0] = zero;
            obj[1] = one;
            obj[2] = two;
            // results:
            //   table.Rows[0].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[0].ItemArray.ItemArray[1] = "one"
            //   table.Rows[0].ItemArray.ItemArray[2] = "two"

            DataRow row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e1)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null ----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = DBNull.Value
            //   table.Rows[1].ItemArray.ItemArray[2] = "two"

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- DBNull.Value -------------
            obj[1] = DBNull.Value;
            // results:
            //   table.Rows[2].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[2].ItemArray.ItemArray[1] = DBNull.Value
            //   table.Rows[2].ItemArray.ItemArray[2] = "two"

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- object array smaller than number of columns -----
            string abc = "abc";
            string def = "def";
            obj = new object[2];
            obj[0] = abc;
            obj[1] = def;
            // results:
            //   table.Rows[3].ItemArray.ItemArray[0] = "abc"
            //   table.Rows[3].ItemArray.ItemArray[1] = "def"
            //   table.Rows[3].ItemArray.ItemArray[2] = DBNull.Value;

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- normal -----------------
            ValueTest("DR5: normal value test", table, 0, 0, zero);
            ValueTest("DR6: normal value test", table, 0, 1, one);
            ValueTest("DR7: normal value test", table, 0, 2, two);

            // -- null ----------
            ValueTest("DR8: null value test", table, 1, 0, zero);
            ValueTest("DR9: null value test", table, 1, 1, DBNull.Value);
            ValueTest("DR10: null value test", table, 1, 2, two);

            // -- DBNull.Value -------------
            ValueTest("DR11: DBNull.Value value test", table, 2, 0, zero);
            ValueTest("DR12: DBNull.Value value test", table, 2, 1, DBNull.Value);
            ValueTest("DR13: DBNull.Value value test", table, 2, 2, two);

            // -- object array smaller than number of columns -----
            ValueTest("DR14: array smaller value test", table, 3, 0, abc);
            ValueTest("DR15: array smaller value test", table, 3, 1, def);
            ValueTest("DR16: array smaller value test", table, 3, 2, DBNull.Value);
        }

        // test DefaultValue when setting ItemArray
        [Fact]
        public void DefaultValueInItemArray()
        {
            string zero = "zero";

            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("zero", typeof(string)));

            DataColumn column = new DataColumn("num", typeof(int));
            column.DefaultValue = 15;
            table.Columns.Add(column);

            object[] obj = new object[2];
            // -- normal -----------------
            obj[0] = "zero";
            obj[1] = 8;
            // results:
            //   table.Rows[0].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[0].ItemArray.ItemArray[1] = 8

            DataRow row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e1)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null ----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = 15

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- DBNull.Value -------------
            obj[1] = DBNull.Value;
            // results:
            //   table.Rows[2].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[2].ItemArray.ItemArray[1] = DBNull.Value
            //      even though internally, the v

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- object array smaller than number of columns -----
            string abc = "abc";
            string def = "def";
            obj = new object[2];
            obj[0] = abc;
            // results:
            //   table.Rows[3].ItemArray.ItemArray[0] = "abc"
            //   table.Rows[3].ItemArray.ItemArray[1] = DBNull.Value

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- normal -----------------
            ValueTest("DR20: normal value test", table, 0, 0, zero);
            ValueTest("DR21: normal value test", table, 0, 1, 8);

            // -- null ----------
            ValueTest("DR22: null value test", table, 1, 0, zero);
            ValueTest("DR23: null value test", table, 1, 1, 15);

            // -- DBNull.Value -------------
            ValueTest("DR24: DBNull.Value value test", table, 2, 0, zero);
            DBNullTest("DR25: DBNull.Value value test", table, 2, 1);

            // -- object array smaller than number of columns -----
            ValueTest("DR26: array smaller value test", table, 3, 0, abc);
            ValueTest("DR27: array smaller value test", table, 3, 1, 15);
        }

        // test AutoIncrement when setting ItemArray
        [Fact]
        public void AutoIncrementInItemArray()
        {
            string zero = "zero";
            string num = "num";

            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn(zero, typeof(string)));

            DataColumn column = new DataColumn("num", typeof(int));
            column.AutoIncrement = true;
            table.Columns.Add(column);

            object[] obj = new object[2];
            // -- normal -----------------
            obj[0] = "zero";
            obj[1] = 8;
            // results:
            //   table.Rows[0].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[0].ItemArray.ItemArray[1] = 8

            DataRow row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e1)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null 1----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = 9

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null 2----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = 10

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null 3----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = 11

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- DBNull.Value -------------
            obj[1] = DBNull.Value;
            // results:
            //   table.Rows[2].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[2].ItemArray.ItemArray[1] = DBNull.Value
            //      even though internally, the AutoIncrement value
            //      is incremented

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- null 4----------
            obj[1] = null;
            // results:
            //   table.Rows[1].ItemArray.ItemArray[0] = "zero"
            //   table.Rows[1].ItemArray.ItemArray[1] = 13

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e2)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- object array smaller than number of columns -----
            string abc = "abc";
            string def = "def";
            obj = new object[2];
            obj[0] = abc;
            // results:
            //   table.Rows[3].ItemArray.ItemArray[0] = "abc"
            //   table.Rows[3].ItemArray.ItemArray[1] = 14

            row = table.NewRow();

            try
            {
                row.ItemArray = obj;
            }
            catch (Exception e3)
            {
                Assert.False(true);
            }

            table.Rows.Add(row);

            // -- normal -----------------
            ValueTest("DR34: normal value test", table, 0, 0, zero);
            ValueTest("DR35: normal value test", table, 0, 1, 8);

            // -- null 1----------
            ValueTest("DR36: null value test", table, 1, 0, zero);
            ValueTest("DR37: null value test", table, 1, 1, 9);

            // -- null 2----------
            ValueTest("DR38: null value test", table, 2, 0, zero);
            ValueTest("DR39: null value test", table, 2, 1, 10);

            // -- null 3----------
            ValueTest("DR40: null value test", table, 3, 0, zero);
            ValueTest("DR41: null value test", table, 3, 1, 11);

            // -- DBNull.Value -------------
            ValueTest("DR42: DBNull.Value value test", table, 4, 0, zero);
            ValueTest("DR43: DBNull.Value value test", table, 4, 1, DBNull.Value);

            // -- null 4----------
            ValueTest("DR44: null value test", table, 5, 0, zero);
            ValueTest("DR45: null value test", table, 5, 1, 13);

            // -- object array smaller than number of columns -----
            ValueTest("DR46: array smaller value test", table, 6, 0, abc);
            ValueTest("DR47: array smaller value test", table, 6, 1, 14);
        }

        [Fact]
        public void AutoIncrementColumnIntegrity()
        {
            // AutoIncrement-column shouldn't raise index out of range
            // exception because of size mismatch of internal itemarray.
            DataTable dt = new DataTable();
            dt.Columns.Add("foo");
            dt.Rows.Add(new object[] { "value" });
            DataColumn col = new DataColumn("bar");
            col.AutoIncrement = true;
            dt.Columns.Add(col);
            dt.Rows[0][0] = "test";
        }

        [Fact]
        public void EnforceConstraint()
        {
            int id = 100;
            // Setup stuff
            var ds = new DataSet();
            DataTable parent = ds.Tables.Add("parent");
            parent.Columns.Add("id", typeof(int));
            DataTable child = ds.Tables.Add("child");
            child.Columns.Add("idref", typeof(int));
            Constraint uniqueId = null;
            parent.Constraints.Add(uniqueId = new UniqueConstraint("uniqueId",
                                  new DataColumn[] { parent.Columns["id"] }, true));
            ForeignKeyConstraint fkc = new ForeignKeyConstraint("ParentChildConstraint", new DataColumn[] { parent.Columns["id"] },
                      new DataColumn[] { child.Columns["idref"] });

            child.Constraints.Add(fkc);

            DataRelation relateParentChild = new DataRelation("relateParentChild",
                                         new DataColumn[] { parent.Columns["id"] },
                                         new DataColumn[] { child.Columns["idref"] },
                                         false);
            ds.Relations.Add(relateParentChild);

            ds.EnforceConstraints = false;
            DataRow parentRow = parent.Rows.Add(new object[] { id });
            DataRow childRow = child.Rows.Add(new object[] { id });
            if (parentRow == childRow.GetParentRow(relateParentChild))
            {
                foreach (DataColumn dc in parent.Columns)
                    Assert.Equal(100, parentRow[dc]);
            }
        }

        [Fact]
        public void DetachedRowItemException()
        {
            Assert.Throws<RowNotInTableException>(() =>
           {
               DataTable dt = new DataTable("table");
               dt.Columns.Add("col");
               dt.Rows.Add((new object[] { "val" }));

               DataRow dr = dt.NewRow();
               Assert.Equal(DataRowState.Detached, dr.RowState);
               dr.CancelEdit();
               Assert.Equal(DataRowState.Detached, dr.RowState);
               object o = dr["col"];
           });
        }

        [Fact]
        public void SetParentRow_Null()
        {
            var ds = new DataSet();

            DataTable child = ds.Tables.Add("child");
            child.Columns.Add("column1");

            DataRow r1 = child.NewRow();

            r1.SetParentRow(null);
        }

        [Fact]
        public void SetParentRow_DataInheritance()
        {
            var ds = new DataSet();

            var child = ds.Tables.Add("child");

            var childColumn1 = child.Columns.Add("column1");
            var childColumn2 = child.Columns.Add("column2");

            var parent1 = ds.Tables.Add("parent1");
            var parent1Column1 = parent1.Columns.Add("column1");
            var parent1Column2 = parent1.Columns.Add("column2");

            var parent2 = ds.Tables.Add("parent2");
            var parent2Column1 = parent2.Columns.Add("column1");
            var parent2Column2 = parent2.Columns.Add("column2");

            var relation1 = ds.Relations.Add("parent1-child", parent1Column1, childColumn1);
            ds.Relations.Add("parent2-child", parent2Column2, childColumn2);

            var childRow1 = child.NewRow();
            var parent1Row = parent1.NewRow();
            var parent2Row = parent2.NewRow();

            parent1Row[parent1Column1] = "p1c1";
            parent1Row[parent1Column2] = "p1c2";
            parent2Row[parent2Column1] = "p2c1";
            parent2Row[parent2Column2] = "p2c2";

            child.Rows.Add(childRow1);
            parent1.Rows.Add(parent1Row);
            parent2.Rows.Add(parent2Row);

            childRow1.SetParentRow(parent1Row);
            Assert.Equal("p1c1", childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);

            childRow1.SetParentRow(parent2Row);
            Assert.Equal("p1c1", childRow1[childColumn1]);
            Assert.Equal("p2c2", childRow1[childColumn2]);

            childRow1.SetParentRow(null);
            Assert.Equal(DBNull.Value, childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);

            childRow1.SetParentRow(parent2Row);
            Assert.Equal(DBNull.Value, childRow1[childColumn1]);
            Assert.Equal("p2c2", childRow1[childColumn2]);
        }

        [Fact]
        public void SetParentRow_with_Relation()
        {
            var ds = new DataSet();

            var child = ds.Tables.Add("child");

            var childColumn1 = child.Columns.Add("column1");
            var childColumn2 = child.Columns.Add("column2");

            var parent1 = ds.Tables.Add("parent1");
            var parent1Column1 = parent1.Columns.Add("column1");
            var parent1Column2 = parent1.Columns.Add("column2");

            var parent2 = ds.Tables.Add("parent2");
            var parent2Column1 = parent2.Columns.Add("column1");
            var parent2Column2 = parent2.Columns.Add("column2");

            var relation1 = ds.Relations.Add("parent1-child", parent1Column1, childColumn1);
            var relation2 = ds.Relations.Add("parent2-child", parent2Column2, childColumn2);

            var childRow1 = child.NewRow();
            var parent1Row = parent1.NewRow();
            var parent2Row = parent2.NewRow();

            parent1Row[parent1Column1] = "p1c1";
            parent1Row[parent1Column2] = "p1c2";
            parent2Row[parent2Column1] = "p2c1";
            parent2Row[parent2Column2] = "p2c2";

            child.Rows.Add(childRow1);
            parent1.Rows.Add(parent1Row);
            parent2.Rows.Add(parent2Row);


            childRow1.SetParentRow(null, relation2);
            Assert.Equal(DBNull.Value, childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);

            try
            {
                childRow1.SetParentRow(parent1Row, relation2);
                Assert.False(true);
            }
            catch (InvalidConstraintException e)
            {
            }
            Assert.Equal(DBNull.Value, childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);

            childRow1.SetParentRow(parent1Row, relation1);
            Assert.Equal("p1c1", childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);


            childRow1.SetParentRow(null, relation2);
            Assert.Equal("p1c1", childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);

            childRow1.SetParentRow(null, relation1);
            Assert.Equal(DBNull.Value, childRow1[childColumn1]);
            Assert.Equal(DBNull.Value, childRow1[childColumn2]);
        }

        [Fact]
        public void SetParent_missing_ParentRow()
        {
            var ds = new DataSet();

            var child = ds.Tables.Add("child");

            var childColumn1 = child.Columns.Add("column1");
            var childColumn2 = child.Columns.Add("column2");

            var parent1 = ds.Tables.Add("parent1");
            var parentColumn1 = parent1.Columns.Add("column1");

            var parent2 = ds.Tables.Add("parent2");
            var parentColumn2 = parent2.Columns.Add("column2");

            ds.Relations.Add("parent1-child", parentColumn1, childColumn1);
            ds.Relations.Add("parent2-child", parentColumn2, childColumn2);

            var childRow = child.NewRow();
            var parentRow = parent2.NewRow();

            parentRow[parentColumn2] = "value";

            child.Rows.Add(childRow);
            parent2.Rows.Add(parentRow);

            childRow.SetParentRow(parentRow);
            Assert.Equal(DBNull.Value, childRow[childColumn1]);
            Assert.Equal("value", childRow[childColumn2]);
        }
    }
}
