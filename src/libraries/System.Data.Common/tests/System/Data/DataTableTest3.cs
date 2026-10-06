// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2007 Novell, Inc
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

using System.IO;

using Xunit;

namespace System.Data.Tests
{
    public class DataTableTest3 : IDisposable
    {
        private readonly string _tempFile;
        private DataSet _dataSet;
        private DataTable _parentTable;
        private DataTable _childTable;
        private DataTable _secondChildTable;

        public DataTableTest3()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (_tempFile != null)
                File.Delete(_tempFile);
        }

        private void MakeParentTable()
        {
            // Create a new Table
            _parentTable = new DataTable("ParentTable");
            _dataSet = new DataSet("XmlSchemaDataSet");
            DataColumn column;
            DataRow row;

            // Create new DataColumn, set DataType,
            // ColumnName and add to Table.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "id";
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            _parentTable.Columns.Add(column);

            // Create second column
            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = "ParentItem";
            column.AutoIncrement = false;
            column.Caption = "ParentItem";
            column.Unique = false;
            // Add the column to the table
            _parentTable.Columns.Add(column);

            // Create third column.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "DepartmentID";
            column.Caption = "DepartmentID";
            // Add the column to the table.
            _parentTable.Columns.Add(column);

            // Make the ID column the primary key column.
            DataColumn[] PrimaryKeyColumns = new DataColumn[2];
            PrimaryKeyColumns[0] = _parentTable.Columns["id"];
            PrimaryKeyColumns[1] = _parentTable.Columns["DepartmentID"];
            _parentTable.PrimaryKey = PrimaryKeyColumns;

            _dataSet.Tables.Add(_parentTable);

            // Create three new DataRow objects and add
            // them to the DataTable
            for (int i = 0; i <= 2; i++)
            {
                row = _parentTable.NewRow();
                row["id"] = i + 1;
                row["ParentItem"] = "ParentItem " + (i + 1);
                row["DepartmentID"] = i + 1;
                _parentTable.Rows.Add(row);
            }
        }

        private void MakeChildTable()
        {
            // Create a new Table
            _childTable = new DataTable("ChildTable");
            DataColumn column;
            DataRow row;

            // Create first column and add to the DataTable.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "ChildID";
            column.AutoIncrement = true;
            column.Caption = "ID";
            column.Unique = true;

            // Add the column to the DataColumnCollection
            _childTable.Columns.Add(column);

            // Create second column
            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = "ChildItem";
            column.AutoIncrement = false;
            column.Caption = "ChildItem";
            column.Unique = false;
            _childTable.Columns.Add(column);

            //Create third column
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "ParentID";
            column.AutoIncrement = false;
            column.Caption = "ParentID";
            column.Unique = false;
            _childTable.Columns.Add(column);

            _dataSet.Tables.Add(_childTable);

            // Create three sets of DataRow objects,
            // five rows each, and add to DataTable.
            for (int i = 0; i <= 1; i++)
            {
                row = _childTable.NewRow();
                row["childID"] = i + 1;
                row["ChildItem"] = "ChildItem " + (i + 1);
                row["ParentID"] = 1;
                _childTable.Rows.Add(row);
            }

            for (int i = 0; i <= 1; i++)
            {
                row = _childTable.NewRow();
                row["childID"] = i + 5;
                row["ChildItem"] = "ChildItem " + (i + 1);
                row["ParentID"] = 2;
                _childTable.Rows.Add(row);
            }

            for (int i = 0; i <= 1; i++)
            {
                row = _childTable.NewRow();
                row["childID"] = i + 10;
                row["ChildItem"] = "ChildItem " + (i + 1);
                row["ParentID"] = 3;
                _childTable.Rows.Add(row);
            }
        }

        private void MakeSecondChildTable()
        {
            // Create a new Table
            _secondChildTable = new DataTable("SecondChildTable");
            DataColumn column;
            DataRow row;

            // Create first column and add to the DataTable.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "ChildID";
            column.AutoIncrement = true;
            column.Caption = "ID";
            column.ReadOnly = true;
            column.Unique = true;

            // Add the column to the DataColumnCollection.
            _secondChildTable.Columns.Add(column);

            // Create second column.
            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = "ChildItem";
            column.AutoIncrement = false;
            column.Caption = "ChildItem";
            column.ReadOnly = false;
            column.Unique = false;
            _secondChildTable.Columns.Add(column);

            //Create third column.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "ParentID";
            column.AutoIncrement = false;
            column.Caption = "ParentID";
            column.ReadOnly = false;
            column.Unique = false;
            _secondChildTable.Columns.Add(column);

            //Create fourth column.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "DepartmentID";
            column.Caption = "DepartmentID";
            column.Unique = false;
            _secondChildTable.Columns.Add(column);

            _dataSet.Tables.Add(_secondChildTable);

            // Create three sets of DataRow objects,
            // five rows each, and add to DataTable.
            for (int i = 0; i <= 1; i++)
            {
                row = _secondChildTable.NewRow();
                row["childID"] = i + 1;
                row["ChildItem"] = "SecondChildItem " + (i + 1);
                row["ParentID"] = 1;
                row["DepartmentID"] = 1;
                _secondChildTable.Rows.Add(row);
            }

            for (int i = 0; i <= 1; i++)
            {
                row = _secondChildTable.NewRow();
                row["childID"] = i + 5;
                row["ChildItem"] = "SecondChildItem " + (i + 1);
                row["ParentID"] = 2;
                row["DepartmentID"] = 2;
                _secondChildTable.Rows.Add(row);
            }

            for (int i = 0; i <= 1; i++)
            {
                row = _secondChildTable.NewRow();
                row["childID"] = i + 10;
                row["ChildItem"] = "SecondChildItem " + (i + 1);
                row["ParentID"] = 3;
                row["DepartmentID"] = 3;
                _secondChildTable.Rows.Add(row);
            }
        }

        private void MakeDataRelation()
        {
            DataColumn parentColumn = _dataSet.Tables["ParentTable"].Columns["id"];
            DataColumn childColumn = _dataSet.Tables["ChildTable"].Columns["ParentID"];
            DataRelation relation = new DataRelation("ParentChild_Relation1", parentColumn, childColumn);
            _dataSet.Tables["ChildTable"].ParentRelations.Add(relation);

            DataColumn[] parentColumn1 = new DataColumn[2];
            DataColumn[] childColumn1 = new DataColumn[2];

            parentColumn1[0] = _dataSet.Tables["ParentTable"].Columns["id"];
            parentColumn1[1] = _dataSet.Tables["ParentTable"].Columns["DepartmentID"];

            childColumn1[0] = _dataSet.Tables["SecondChildTable"].Columns["ParentID"];
            childColumn1[1] = _dataSet.Tables["SecondChildTable"].Columns["DepartmentID"];

            DataRelation secondRelation = new DataRelation("ParentChild_Relation2", parentColumn1, childColumn1);
            _dataSet.Tables["SecondChildTable"].ParentRelations.Add(secondRelation);
        }

        //Test properties of a table which does not belongs to a DataSet
        private void VerifyTableSchema(DataTable table, string tableName, DataSet ds)
        {
            //Check Properties of Table
            Assert.Equal("", table.Namespace);
            Assert.Equal(ds, table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal(tableName, table.TableName);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal("", table.Prefix);
            Assert.Equal("Constraint2", table.Constraints[0].ToString());
            Assert.Equal("Constraint1", table.Constraints[1].ToString());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[1].GetType());
            Assert.Equal(2, table.PrimaryKey.Length);
            Assert.Equal("id", table.PrimaryKey[0].ToString());
            Assert.Equal("DepartmentID", table.PrimaryKey[1].ToString());

            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(0, table.ChildRelations.Count);

            //Check properties of each column
            //First Column
            DataColumn col = table.Columns[0];
            Assert.False(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("id", col.Caption);
            Assert.Equal("id", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(0, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal(tableName, col.Table.ToString());
            Assert.True(col.Unique);

            //Second Column
            col = table.Columns[1];
            Assert.True(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ParentItem", col.Caption);
            Assert.Equal("ParentItem", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal("", col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(1, col.Ordinal);
            Assert.Equal("", col.Prefix);
            Assert.Equal(tableName, col.Table.ToString());
            Assert.False(col.Unique);

            //Third Column
            col = table.Columns[2];
            Assert.False(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("DepartmentID", col.Caption);
            Assert.Equal("DepartmentID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal("", col.Prefix);
            Assert.Equal(tableName, col.Table.ToString());
            Assert.False(col.Unique);
        }

        private void VerifyParentTableSchema(DataTable table, string tableName, DataSet ds)
        {
            //Check Properties of Table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Equal(ds.DataSetName, table.DataSet.DataSetName);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal(string.Empty, table.Prefix);

            //Check Constraints
            Assert.Equal("Constraint1", table.Constraints[0].ToString());
            Assert.Equal("Constraint2", table.Constraints[1].ToString());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[1].GetType());
            Assert.Equal(2, table.PrimaryKey.Length);
            Assert.Equal("id", table.PrimaryKey[0].ToString());
            Assert.Equal("DepartmentID", table.PrimaryKey[1].ToString());

            //Check Relations of the ParentTable
            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(2, table.ChildRelations.Count);
            Assert.Equal("ParentChild_Relation1", table.ChildRelations[0].ToString());
            Assert.Equal("ParentChild_Relation2", table.ChildRelations[1].ToString());
            Assert.Equal("ChildTable", table.ChildRelations[0].ChildTable.TableName);
            Assert.Equal("SecondChildTable", table.ChildRelations[1].ChildTable.TableName);

            Assert.Equal(1, table.ChildRelations[0].ParentColumns.Length);
            Assert.Equal("id", table.ChildRelations[0].ParentColumns[0].ColumnName);
            Assert.Equal(1, table.ChildRelations[0].ChildColumns.Length);
            Assert.Equal("ParentID", table.ChildRelations[0].ChildColumns[0].ColumnName);

            Assert.Equal(2, table.ChildRelations[1].ParentColumns.Length);
            Assert.Equal("id", table.ChildRelations[1].ParentColumns[0].ColumnName);
            Assert.Equal("DepartmentID", table.ChildRelations[1].ParentColumns[1].ColumnName);
            Assert.Equal(2, table.ChildRelations[1].ChildColumns.Length);

            Assert.Equal("ParentID", table.ChildRelations[1].ChildColumns[0].ColumnName);
            Assert.Equal("DepartmentID", table.ChildRelations[1].ChildColumns[1].ColumnName);

            //Check properties of each column
            //First Column
            DataColumn col = table.Columns[0];
            Assert.False(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("id", col.Caption);
            Assert.Equal("id", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(0, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.True(col.Unique);

            //Second Column
            col = table.Columns[1];
            Assert.True(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ParentItem", col.Caption);
            Assert.Equal("ParentItem", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(1, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.False(col.Unique);

            //Third Column
            col = table.Columns[2];
            Assert.False(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("DepartmentID", col.Caption);
            Assert.Equal("DepartmentID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.False(col.Unique);
        }

        [Fact]
        public void XmlSchemaTest1()
        {
            MakeParentTable();
            //Detach the table from the DataSet
            _dataSet.Tables.Remove(_parentTable);

            //Write
            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable.WriteXmlSchema(stream);
            }

            //Read
            DataTable table = new DataTable();
            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                table.ReadXmlSchema(stream);
            }

            VerifyTableSchema(table, _parentTable.TableName, _parentTable.DataSet);
        }

        [Fact]
        public void XmlSchemaTest2()
        {
            MakeParentTable();

            _dataSet.Tables.Remove(_parentTable);
            _parentTable.TableName = string.Empty;

            using FileStream stream = new FileStream(_tempFile, FileMode.Create);
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _parentTable.WriteXmlSchema(stream));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public void XmlSchemaTest3()
        {
            //Write
            MakeParentTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable.WriteXmlSchema(stream);
            }

            //Read
            DataTable table = new DataTable();
            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                table.ReadXmlSchema(stream);
            }

            VerifyTableSchema(table, _parentTable.TableName, null);
        }

        [Fact]
        public void XmlSchemaTest5()
        {
            MakeParentTable();
            MakeChildTable();
            MakeSecondChildTable();
            MakeDataRelation();

            //Write
            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _childTable.WriteXmlSchema(stream);
            }

            //Read
            DataTable table = new DataTable(_childTable.TableName);
            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                table.ReadXmlSchema(stream);
            }

            //Check Properties of the table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Null(table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal("ChildTable", table.TableName);
            Assert.Equal(string.Empty, table.Prefix);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal("Constraint1", table.Constraints[0].ToString());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(0, table.ChildRelations.Count);
            Assert.Equal(0, table.PrimaryKey.Length);

            //First Column
            DataColumn col = table.Columns[0];
            Assert.True(col.AllowDBNull);
            Assert.True(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ChildID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(0, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.True(col.Unique);

            //Second Column
            col = table.Columns[1];
            Assert.True(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ChildItem", col.Caption);
            Assert.Equal("ChildItem", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(1, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.False(col.Unique);

            //Third Column
            col = table.Columns[2];
            Assert.True(col.AllowDBNull);
            Assert.False(col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ParentID", col.Caption);
            Assert.Equal("ParentID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.False(col.Unique);
        }

        [Fact]
        public void XmlSchemaTest6()
        {
            MakeParentTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable.WriteXmlSchema(stream);
            }

            DataTable table = new DataTable();
            DataSet ds = new DataSet();
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ArgumentException ex = Assert.Throws<ArgumentException>(() => table.ReadXmlSchema(stream));
                // DataTable 'Table1' does not match
                // to any DataTable in source
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Table1" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void XmlSchemaTest7()
        {
            DataTable table = new DataTable();

            ArgumentException ex = Assert.Throws<ArgumentException>(() => table.ReadXmlSchema(string.Empty));
            // The URL cannot be empty
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            //Assert.Equal ("url", ex.ParamName);
        }

        [Fact]
        public void XmlSchemaTest8()
        {
            MakeParentTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable.WriteXmlSchema(stream);
            }

            //Create a table and define the schema partially
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn(_parentTable.Columns[0].ColumnName, typeof(int)));

            //ReadXmlSchema will not read any schema in this case
            table.ReadXmlSchema(_tempFile);

            Assert.Equal(string.Empty, table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(0, table.Constraints.Count);
        }

        [Fact]
        public void XmlSchemaTest9()
        {
            MakeParentTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable.WriteXmlSchema(stream);
            }

            //Create a table and define the full schema
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn(_parentTable.Columns[0].ColumnName, typeof(int)));
            table.Columns.Add(new DataColumn(_parentTable.Columns[1].ColumnName, typeof(string)));
            table.Columns.Add(new DataColumn(_parentTable.Columns[2].ColumnName, typeof(int)));

            //ReadXmlSchema will not read any schema in this case
            table.ReadXmlSchema(_tempFile);

            Assert.Equal(string.Empty, table.TableName);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Constraints.Count);
        }
    }
}
