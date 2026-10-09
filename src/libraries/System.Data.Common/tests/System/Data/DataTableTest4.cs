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
using System.Text.RegularExpressions;
using System.Xml;
using Xunit;

namespace System.Data.Tests
{
    public class DataTableTest4 : IDisposable
    {
        private string _tempFile;
        private DataSet _dataSet;
        private DataTable _dummyTable;
        private DataTable _parentTable1;
        private DataTable _childTable;
        private DataTable _secondChildTable;

        public DataTableTest4()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (_tempFile != null)
                File.Delete(_tempFile);
        }

        private void MakeParentTable1()
        {
            // Create a new Table
            _parentTable1 = new DataTable("ParentTable");
            _dataSet = new DataSet("XmlDataSet");
            DataColumn column;
            DataRow row;

            // Create new DataColumn, set DataType,
            // ColumnName and add to Table.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "id";
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            _parentTable1.Columns.Add(column);

            // Create second column
            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = "ParentItem";
            column.AutoIncrement = false;
            column.Caption = "ParentItem";
            column.Unique = false;
            // Add the column to the table
            _parentTable1.Columns.Add(column);

            // Create third column.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "DepartmentID";
            column.Caption = "DepartmentID";
            // Add the column to the table.
            _parentTable1.Columns.Add(column);

            // Make the ID column the primary key column.
            DataColumn[] PrimaryKeyColumns = new DataColumn[2];
            PrimaryKeyColumns[0] = _parentTable1.Columns["id"];
            PrimaryKeyColumns[1] = _parentTable1.Columns["DepartmentID"];
            _parentTable1.PrimaryKey = PrimaryKeyColumns;
            _dataSet.Tables.Add(_parentTable1);

            // Create three new DataRow objects and add
            // them to the DataTable
            for (int i = 0; i <= 2; i++)
            {
                row = _parentTable1.NewRow();
                row["id"] = i + 1;
                row["ParentItem"] = "ParentItem " + (i + 1);
                row["DepartmentID"] = i + 1;
                _parentTable1.Rows.Add(row);
            }
        }

        private void MakeDummyTable()
        {
            // Create a new Table
            _dataSet = new DataSet();
            _dummyTable = new DataTable("DummyTable");
            DataColumn column;
            DataRow row;

            // Create new DataColumn, set DataType,
            // ColumnName and add to Table.
            column = new DataColumn();
            column.DataType = typeof(int);
            column.ColumnName = "id";
            column.Unique = true;
            // Add the Column to the DataColumnCollection.
            _dummyTable.Columns.Add(column);

            // Create second column
            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = "DummyItem";
            column.AutoIncrement = false;
            column.Caption = "DummyItem";
            column.Unique = false;
            // Add the column to the table
            _dummyTable.Columns.Add(column);
            _dataSet.Tables.Add(_dummyTable);

            // Create three new DataRow objects and add
            // them to the DataTable
            for (int i = 0; i <= 2; i++)
            {
                row = _dummyTable.NewRow();
                row["id"] = i + 1;
                row["DummyItem"] = "DummyItem " + (i + 1);
                _dummyTable.Rows.Add(row);
            }

            DataRow row1 = _dummyTable.Rows[1];
            _dummyTable.AcceptChanges();
            row1.BeginEdit();
            row1[1] = "Changed_DummyItem " + 2;
            row1.EndEdit();
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
            //Test Schema
            //Check Properties of Table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Equal(ds, table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal(tableName, table.TableName);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal(string.Empty, table.Prefix);
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
            Assert.Equal("System.Int32", col.DataType.ToString());
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
            Assert.Equal("System.String", col.DataType.ToString());
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
            Assert.Equal("System.Int32", col.DataType.ToString());
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.False(col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.False(col.Unique);

            //Test the Xml
            Assert.Equal(3, table.Rows.Count);
            //Test values of each row
            DataRow row = table.Rows[0];
            Assert.Equal(1, row["id"]);
            Assert.Equal("ParentItem 1", row["ParentItem"]);
            Assert.Equal(1, row["DepartmentID"]);

            row = table.Rows[1];
            Assert.Equal(2, row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal(2, row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal(3, row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal(3, row["DepartmentID"]);
        }

        private void VerifyTable_WithChildren(DataTable table, string tableName, DataSet ds)
        {
            //Test Schema
            //Check Properties of Table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Equal(ds.DataSetName, table.DataSet.DataSetName);
            Assert.Equal(3, table.Columns.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal(tableName, table.TableName);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal(string.Empty, table.Prefix);
            Assert.Equal("Constraint2", table.Constraints[0].ToString());
            Assert.Equal("Constraint1", table.Constraints[1].ToString());
            Assert.Equal("System.Data.UniqueConstraint", table.Constraints[0].GetType().ToString());
            Assert.Equal("System.Data.UniqueConstraint", table.Constraints[1].GetType().ToString());
            Assert.Equal(2, table.PrimaryKey.Length);
            Assert.Equal("id", table.PrimaryKey[0].ToString());
            Assert.Equal("DepartmentID", table.PrimaryKey[1].ToString());
            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(2, table.ChildRelations.Count);

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

            //Test the Xml
            Assert.Equal(3, table.Rows.Count);
            //Test values of each row
            DataRow row = table.Rows[0];
            Assert.Equal(1, row["id"]);
            Assert.Equal("ParentItem 1", row["ParentItem"]);
            Assert.Equal(1, row["DepartmentID"]);

            row = table.Rows[1];
            Assert.Equal(2, row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal(2, row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal(3, row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal(3, row["DepartmentID"]);
        }

        private void VerifyDiffGramElement1(XmlReader reader)
        {
            //This method checks the properties of the <id> element
            Assert.True(reader.IsStartElement());
            Assert.Equal(3, reader.Depth);
            Assert.False(reader.HasAttributes);
            Assert.False(reader.HasValue);
            Assert.False(reader.IsDefault);
            Assert.False(reader.IsEmptyElement);
            Assert.Equal("id", reader.Name);
            Assert.Equal("id", reader.LocalName);
            Assert.Equal(XmlNodeType.Element, reader.NodeType);
        }

        private void VerifyDiffGramElement3(XmlReader reader)
        {
            //This method checks the property of </id> end elem
            Assert.False(reader.IsStartElement());
            Assert.Equal("id", reader.Name);
            Assert.Equal("id", reader.LocalName);
            Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
        }

        private void VerifyDiffGramElement2(XmlReader reader)
        {
            //This method tests the properties of the <DummyItem> elemnent
            Assert.True(reader.IsStartElement());
            Assert.Equal(3, reader.Depth);
            Assert.False(reader.HasAttributes);
            Assert.False(reader.HasValue);
            Assert.False(reader.IsDefault);
            Assert.False(reader.IsEmptyElement);
            Assert.Equal("DummyItem", reader.Name);
            Assert.Equal("DummyItem", reader.LocalName);
            Assert.Equal(XmlNodeType.Element, reader.NodeType);
        }

        private void VerifyDiffGramElement4(XmlReader reader)
        {
            //This method checks the properties of </DummyItem> end element
            Assert.False(reader.IsStartElement());
            Assert.Equal("DummyItem", reader.Name);
            Assert.Equal("DummyItem", reader.LocalName);
            Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
        }

        private void VerifyDiffGramElement5(XmlReader reader)
        {
            //This method check the properties of </DummyTable> end element
            Assert.False(reader.IsStartElement());
            Assert.Equal("DummyTable", reader.Name);
            Assert.Equal("DummyTable", reader.LocalName);
            Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
        }

        [Fact]
        public void XmlTest1()
        {
            MakeParentTable1();
            _dataSet.Tables.Remove(_parentTable1);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write the XML without any Schema information
                _parentTable1.WriteXml(stream);
            }

            DataTable table = new DataTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                // Should throw an exception if the Xml
                // File has no schema and target table
                // too does not define any schema
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => table.ReadXml(stream));
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void XmlTest2()
        {
            //Make a table without any relations
            MakeParentTable1();
            _dataSet.Tables.Remove(_parentTable1);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write Xml along with the Schema
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            DataTable table = new DataTable();
            //Read the Xml and the Schema into a table which does not belongs to any DataSet
            table.ReadXml(_tempFile);
            VerifyTableSchema(table, _parentTable1.TableName, _parentTable1.DataSet);
        }

        [Fact]
        public void XmlTest3()
        {
            //Make a table without any Relations
            MakeParentTable1();
            _dataSet.Tables.Remove(_parentTable1);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write the Xml and the Schema
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            DataTable table = new DataTable();
            _dataSet.Tables.Add(table);

            //Read the Xml and the Schema into a table which already belongs to a DataSet
            //and the table name does not match with the table ion the source XML 
            ArgumentException ex = Assert.Throws<ArgumentException>(() => table.ReadXml(_tempFile));
            // DataTable 'Table1' does not match to any
            // DataTable in source
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + "Table1" + @"[\p{Pf}\p{Po}]", ex.Message);
            Assert.Null(ex.ParamName);
        }

        [Fact]
        public void XmlTest4()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Here the table belong to a dataset
                //Write the Xml and the Schema
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            DataTable table = new DataTable("ParentTable");
            //Read the Xml and the Schema into a table which already belongs to a DataSet
            //and the table name matches with the table in the source XML
            table.ReadXml(_tempFile);
            VerifyTableSchema(table, _parentTable1.TableName, null);
        }

        [Fact]
        public void XmlTest5()
        {
            //Create a parent table and create child tables
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema, false);
            }

            DataTable table = new DataTable();
            table.ReadXml(_tempFile);
            VerifyTableSchema(table, _parentTable1.TableName, null);
        }

        [Fact]
        public void XmlTest6()
        {
            //Create a parent table and create child tables
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema, true);
            }

            DataTable table = new DataTable();
            table.ReadXml(_tempFile);

            VerifyTable_WithChildren(table, _parentTable1.TableName, _parentTable1.DataSet);

            //Check Properties of First Child Table
            DataTable firstChildTable = table.ChildRelations[1].ChildTable;
            Assert.Equal(string.Empty, firstChildTable.Namespace);
            Assert.Equal("XmlDataSet", firstChildTable.DataSet.DataSetName);
            Assert.Equal(3, firstChildTable.Columns.Count);
            Assert.Equal(typeof(int), firstChildTable.Columns[0].DataType);
            Assert.Equal(typeof(string), firstChildTable.Columns[1].DataType);
            Assert.Equal(typeof(int), firstChildTable.Columns[2].DataType);
            Assert.Equal(6, firstChildTable.Rows.Count);
            Assert.False(firstChildTable.CaseSensitive);
            Assert.Equal("ChildTable", firstChildTable.TableName);
            Assert.Equal(string.Empty, firstChildTable.Prefix);
            Assert.Equal(2, firstChildTable.Constraints.Count);
            Assert.Equal("Constraint1", firstChildTable.Constraints[0].ToString());
            Assert.Equal("ParentChild_Relation1", firstChildTable.Constraints[1].ToString());
            Assert.Equal(1, firstChildTable.ParentRelations.Count);
            Assert.Equal("ParentTable", firstChildTable.ParentRelations[0].ParentTable.TableName);
            Assert.Equal(0, firstChildTable.ChildRelations.Count);
            Assert.Equal(0, firstChildTable.PrimaryKey.Length);

            //Check Properties of Second Child Table
            DataTable secondChildTable = table.ChildRelations[0].ChildTable;
            Assert.Equal(string.Empty, secondChildTable.Namespace);
            Assert.Equal("XmlDataSet", secondChildTable.DataSet.DataSetName);
            Assert.Equal(4, secondChildTable.Columns.Count);
            Assert.Equal(typeof(int), secondChildTable.Columns[0].DataType);
            Assert.Equal(typeof(string), secondChildTable.Columns[1].DataType);
            Assert.Equal(typeof(int), secondChildTable.Columns[2].DataType);
            Assert.Equal(typeof(int), secondChildTable.Columns[3].DataType);
            Assert.Equal(6, secondChildTable.Rows.Count);
            Assert.False(secondChildTable.CaseSensitive);
            Assert.Equal("SecondChildTable", secondChildTable.TableName);
            Assert.Equal(string.Empty, secondChildTable.Prefix);
            Assert.Equal(2, secondChildTable.Constraints.Count);
            Assert.Equal("Constraint1", secondChildTable.Constraints[0].ToString());
            Assert.Equal("ParentChild_Relation2", secondChildTable.Constraints[1].ToString());
            Assert.Equal(1, secondChildTable.ParentRelations.Count);
            Assert.Equal("ParentTable", secondChildTable.ParentRelations[0].ParentTable.TableName);
            Assert.Equal(0, secondChildTable.ChildRelations.Count);
            Assert.Equal(0, secondChildTable.PrimaryKey.Length);
        }

        [Fact]
        public void XmlTest7()
        {
            //Create a parent table and create child tables
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //WriteXml on any of the children
                _childTable.WriteXml(stream, XmlWriteMode.WriteSchema, false);
            }

            DataTable table = new DataTable();
            table.ReadXml(_tempFile);

            //Test Schema
            //Check Properties of Table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Null(table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.False(table.CaseSensitive);
            Assert.Equal("ChildTable", table.TableName);
            Assert.Equal(string.Empty, table.Prefix);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal("Constraint1", table.Constraints[0].ToString());
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
            Assert.Equal(0, table.PrimaryKey.Length);
            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(0, table.ChildRelations.Count);


            //Check properties of each column
            //First Column
            DataColumn col = table.Columns[0];
            Assert.True(col.AllowDBNull);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ID", col.Caption);
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

            //Test the Xml
            Assert.Equal(6, table.Rows.Count);

            //Test values of each row
            DataRow row = table.Rows[0];
            Assert.Equal(1, row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal(1, row["ParentID"]);

            row = table.Rows[1];
            Assert.Equal(2, row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal(1, row["ParentID"]);

            row = table.Rows[2];
            Assert.Equal(5, row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal(2, row["ParentID"]);

            row = table.Rows[3];
            Assert.Equal(6, row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal(2, row["ParentID"]);

            row = table.Rows[4];
            Assert.Equal(10, row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal(3, row["ParentID"]);

            row = table.Rows[5];
            Assert.Equal(11, row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal(3, row["ParentID"]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void XmlTest8()
        {
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write only the Xml
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema, false);
            }

            DataSet ds = new DataSet();
            ds.ReadXml(_tempFile);

            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("ParentTable", ds.Tables[0].TableName);
            DataTable table = ds.Tables[0];

            Assert.Equal(3, table.Rows.Count);
            //Test values of each row
            DataRow row = table.Rows[0];
            Assert.Equal("1", row["id"]);
            Assert.Equal("ParentItem 1", row["ParentItem"]);
            Assert.Equal("1", row["DepartmentID"]);

            row = table.Rows[1];
            Assert.Equal("2", row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal("2", row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal("3", row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal("3", row["DepartmentID"]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void XmlTest9()
        {
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write only the Xml
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema, true);
            }

            DataSet ds = new DataSet();
            ds.ReadXml(_tempFile);

            Assert.Equal(3, ds.Tables.Count);
            Assert.Equal("ParentTable", ds.Tables[0].TableName);
            Assert.Equal("ChildTable", ds.Tables[1].TableName);
            Assert.Equal("SecondChildTable", ds.Tables[2].TableName);

            //get the first table
            DataTable table = ds.Tables[0];
            Assert.Equal(3, table.Rows.Count);

            DataRow row = table.Rows[0];
            Assert.Equal("1", row["id"]);
            Assert.Equal("ParentItem 1", row["ParentItem"]);
            Assert.Equal("1", row["DepartmentID"]);

            row = table.Rows[1];
            Assert.Equal("2", row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal("2", row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal("3", row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal("3", row["DepartmentID"]);

            //get the second table
            table = ds.Tables[1];
            Assert.Equal(6, table.Rows.Count);

            row = table.Rows[0];
            Assert.Equal("1", row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal("1", row["ParentID"]);

            row = table.Rows[1];
            Assert.Equal("2", row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal("1", row["ParentID"]);

            row = table.Rows[2];
            Assert.Equal("5", row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal("2", row["ParentID"]);

            row = table.Rows[3];
            Assert.Equal("6", row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal("2", row["ParentID"]);

            row = table.Rows[4];
            Assert.Equal("10", row["ChildID"]);
            Assert.Equal("ChildItem 1", row["ChildItem"]);
            Assert.Equal("3", row["ParentID"]);

            row = table.Rows[5];
            Assert.Equal("11", row["ChildID"]);
            Assert.Equal("ChildItem 2", row["ChildItem"]);
            Assert.Equal("3", row["ParentID"]);

            //get the third table
            table = ds.Tables[2];
            Assert.Equal(6, table.Rows.Count);

            row = table.Rows[0];
            Assert.Equal("1", row["ChildID"]);
            Assert.Equal("SecondChildItem 1", row["ChildItem"]);
            Assert.Equal("1", row["ParentID"]);
            Assert.Equal("1", row["DepartmentID"]);

            row = table.Rows[1];
            Assert.Equal("2", row["ChildID"]);
            Assert.Equal("SecondChildItem 2", row["ChildItem"]);
            Assert.Equal("1", row["ParentID"]);
            Assert.Equal("1", row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal("5", row["ChildID"]);
            Assert.Equal("SecondChildItem 1", row["ChildItem"]);
            Assert.Equal("2", row["ParentID"]);
            Assert.Equal("2", row["DepartmentID"]);

            row = table.Rows[3];
            Assert.Equal("6", row["ChildID"]);
            Assert.Equal("SecondChildItem 2", row["ChildItem"]);
            Assert.Equal("2", row["ParentID"]);
            Assert.Equal("2", row["DepartmentID"]);

            row = table.Rows[4];
            Assert.Equal("10", row["ChildID"]);
            Assert.Equal("SecondChildItem 1", row["ChildItem"]);
            Assert.Equal("3", row["ParentID"]);
            Assert.Equal("3", row["DepartmentID"]);

            row = table.Rows[5];
            Assert.Equal("11", row["ChildID"]);
            Assert.Equal("SecondChildItem 2", row["ChildItem"]);
            Assert.Equal("3", row["ParentID"]);
            Assert.Equal("3", row["DepartmentID"]);
        }

        [Fact]
        public void XmlTest10()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                XmlReader reader = XmlReader.Create(stream, settings);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(0, reader.Depth);
                Assert.True(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("diffgr:diffgram", reader.Name);
                Assert.Equal("diffgram", reader.LocalName);
                Assert.Equal(2, reader.AttributeCount);
                Assert.Equal("urn:schemas-microsoft-com:xml-msdata", reader[0]);
                Assert.Equal("urn:schemas-microsoft-com:xml-diffgram-v1", reader[1]);
                Assert.Equal("urn:schemas-microsoft-com:xml-diffgram-v1", reader.NamespaceURI);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(1, reader.Depth);
                Assert.False(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("NewDataSet", reader.Name);
                Assert.Equal("NewDataSet", reader.LocalName);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(2, reader.Depth);
                Assert.True(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("DummyTable", reader.Name);
                Assert.Equal("DummyTable", reader.LocalName);
                Assert.Equal(2, reader.AttributeCount);
                Assert.Equal("DummyTable1", reader[0]);
                Assert.Equal("0", reader[1]);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                VerifyDiffGramElement1(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("1", reader.Value);

                reader.Read();
                VerifyDiffGramElement3(reader);

                reader.Read();
                VerifyDiffGramElement2(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("DummyItem 1", reader.Value);

                reader.Read();
                Assert.False(reader.IsStartElement());
                Assert.Equal("DummyItem", reader.Name);
                Assert.Equal("DummyItem", reader.LocalName);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);

                reader.Read();
                Assert.False(reader.IsStartElement());
                Assert.Equal("DummyTable", reader.Name);
                Assert.Equal("DummyTable", reader.LocalName);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(2, reader.Depth);
                Assert.True(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("DummyTable", reader.Name);
                Assert.Equal("DummyTable", reader.LocalName);
                Assert.Equal(3, reader.AttributeCount);
                Assert.Equal("DummyTable2", reader[0]);
                Assert.Equal("1", reader[1]);
                Assert.Equal("modified", reader[2]);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                VerifyDiffGramElement1(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("2", reader.Value);

                reader.Read();
                VerifyDiffGramElement3(reader);

                reader.Read();
                VerifyDiffGramElement2(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("Changed_DummyItem 2", reader.Value);

                reader.Read();
                VerifyDiffGramElement4(reader);

                reader.Read();
                VerifyDiffGramElement5(reader);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(2, reader.Depth);
                Assert.True(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("DummyTable", reader.Name);
                Assert.Equal("DummyTable", reader.LocalName);
                Assert.Equal(2, reader.AttributeCount);
                Assert.Equal("DummyTable3", reader[0]);
                Assert.Equal("2", reader[1]);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                VerifyDiffGramElement1(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("3", reader.Value);

                reader.Read();
                VerifyDiffGramElement3(reader);

                reader.Read();
                VerifyDiffGramElement2(reader);


                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("DummyItem 3", reader.Value);

                reader.Read();
                VerifyDiffGramElement4(reader);

                reader.Read();
                VerifyDiffGramElement5(reader);

                reader.Read();
                Assert.False(reader.IsStartElement());
                Assert.Equal("NewDataSet", reader.Name);
                Assert.Equal("NewDataSet", reader.LocalName);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(1, reader.Depth);
                Assert.False(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("diffgr:before", reader.Name);
                Assert.Equal("before", reader.LocalName);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                Assert.True(reader.IsStartElement());
                Assert.Equal(2, reader.Depth);
                Assert.True(reader.HasAttributes);
                Assert.False(reader.HasValue);
                Assert.False(reader.IsDefault);
                Assert.False(reader.IsEmptyElement);
                Assert.Equal("DummyTable", reader.Name);
                Assert.Equal("DummyTable", reader.LocalName);
                Assert.Equal(2, reader.AttributeCount);
                Assert.Equal("DummyTable2", reader[0]);
                Assert.Equal("1", reader[1]);
                Assert.Equal(XmlNodeType.Element, reader.NodeType);

                reader.Read();
                VerifyDiffGramElement1(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("2", reader.Value);

                reader.Read();
                VerifyDiffGramElement3(reader);

                reader.Read();
                VerifyDiffGramElement2(reader);

                reader.Read();
                Assert.Equal(XmlNodeType.Text, reader.NodeType);
                Assert.True(reader.HasValue);
                Assert.Equal("DummyItem 2", reader.Value);

                reader.Read();
                VerifyDiffGramElement4(reader);

                reader.Read();
                VerifyDiffGramElement5(reader);

                reader.Read();
                Assert.False(reader.IsStartElement());
                Assert.Equal("diffgr:before", reader.Name);
                Assert.Equal("before", reader.LocalName);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);

                reader.Read();
                Assert.False(reader.IsStartElement());
                Assert.Equal("diffgr:diffgram", reader.Name);
                Assert.Equal("diffgram", reader.LocalName);
                Assert.Equal(XmlNodeType.EndElement, reader.NodeType);
            }
        }

        [Fact]
        public void XmlTest11()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            XmlReadMode mode = XmlReadMode.Auto;
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("id", typeof(int)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                //This should not read anything as table name is not set
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal(string.Empty, table.TableName);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Null(table.DataSet);
        }

        [Fact]
        public void XmlTest12()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            DataTable table = new DataTable("Table1");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                //Same as last test. ReadXml does not read anything as table names dont match
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("Table1", table.TableName);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Null(table.DataSet);
        }

        [Fact]
        public void XmlTest13()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.Null(table.DataSet);

            //Check Rows
            DataRow row = table.Rows[0];
            Assert.Equal("1", row[0]);

            row = table.Rows[1];
            Assert.Equal("2", row[0]);

            row = table.Rows[2];
            Assert.Equal("3", row[0]);
        }

        [Fact]
        public void XmlTest14()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            //Create a target table which has nomatching column(s) names
            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("sid", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                //ReadXml does not read anything as the column names are not matching
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("sid", table.Columns[0].ColumnName);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.Null(table.DataSet);

            //Check the rows
            foreach (DataRow row in table.Rows)
                Assert.Equal(DBNull.Value, row[0]);
        }

        [Fact]
        public void XmlTest15()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            //Create a target table which has matching column(s) name and an extra column
            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("ParentItem", typeof(string)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(int)));
            table.Columns.Add(new DataColumn("DummyColumn", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(4, table.Columns.Count);
            Assert.Null(table.DataSet);

            //Check the Columns
            Assert.Equal("id", table.Columns[0].ColumnName);
            Assert.Equal(typeof(int), table.Columns[0].DataType);

            Assert.Equal("ParentItem", table.Columns[1].ColumnName);
            Assert.Equal(typeof(string), table.Columns[1].DataType);

            Assert.Equal("DepartmentID", table.Columns[2].ColumnName);
            Assert.Equal(typeof(int), table.Columns[2].DataType);

            Assert.Equal("DummyColumn", table.Columns[3].ColumnName);
            Assert.Equal(typeof(string), table.Columns[3].DataType);

            //Check the rows
            DataRow row = table.Rows[0];
            Assert.Equal(1, row["id"]);
            Assert.Equal("ParentItem 1", row["ParentItem"]);
            Assert.Equal(1, row["DepartmentID"]);
            Assert.Equal(DBNull.Value, row["DummyColumn"]);

            row = table.Rows[1];
            Assert.Equal(2, row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal(2, row["DepartmentID"]);
            Assert.Equal(DBNull.Value, row["DummyColumn"]);

            row = table.Rows[2];
            Assert.Equal(3, row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal(3, row["DepartmentID"]);
            Assert.Equal(DBNull.Value, row["DummyColumn"]);
        }

        [Fact]
        public void XmlTest16()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                //Write the Xml with schema information
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            XmlReadMode mode = XmlReadMode.Auto;
            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn("id", typeof(int)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.ReadSchema, mode);
            Assert.Equal(string.Empty, table.TableName);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Null(table.DataSet);
        }

        [Fact]
        public void XmlTest17()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                DataTable table = new DataTable("Table1");
                table.Columns.Add(new DataColumn("id", typeof(int)));

                ArgumentException ex = Assert.Throws<ArgumentException>(() => table.ReadXml(stream));
                // DataTable 'Table1' does not match to
                // any DataTable in source
                Assert.Equal(typeof(ArgumentException), ex.GetType());
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
        public void XmlTest18()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(int)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.ReadSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(int), table.Columns[1].DataType);
            Assert.Null(table.DataSet);

            //Check rows
            DataRow row = table.Rows[0];
            Assert.Equal(1, row[0]);
            Assert.Equal(1, row[1]);

            row = table.Rows[1];
            Assert.Equal(2, row[0]);
            Assert.Equal(2, row[1]);

            row = table.Rows[2];
            Assert.Equal(3, row[0]);
            Assert.Equal(3, row[1]);
        }

        [Fact]
        public void XmlTest19()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable();
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                //ReadXml won't read anything as TableNames dont match
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("Table1", table.TableName);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal("System.Data.DataSet", table.DataSet.ToString());
            Assert.Equal("NewDataSet", table.DataSet.DataSetName);
        }

        [Fact]
        public void XmlTest20()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable("HelloWorldTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                //ReadXml won't read anything as TableNames dont match
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("HelloWorldTable", table.TableName);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal("System.Data.DataSet", table.DataSet.ToString());
            Assert.Equal("NewDataSet", table.DataSet.DataSetName);
        }

        [Fact]
        public void XmlTest21()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.IgnoreSchema);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", Type.GetType("System.Int32")));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.IgnoreSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal("System.Data.DataSet", table.DataSet.ToString());
            Assert.Equal("NewDataSet", table.DataSet.DataSetName);

            //Check the rows
            DataRow row = table.Rows[0];
            Assert.Equal(1, row[0]);

            row = table.Rows[1];
            Assert.Equal(2, row[0]);

            row = table.Rows[2];
            Assert.Equal(3, row[0]);
        }

        [Fact]
        public void XmlTest22()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                DataSet ds = new DataSet();
                DataTable table = new DataTable("Table1");
                table.Columns.Add(new DataColumn("id", Type.GetType("System.Int32")));
                ds.Tables.Add(table);

                ArgumentException ex = Assert.Throws<ArgumentException>(() => table.ReadXml(stream));
                // DataTable 'Table1' does not match to
                // any DataTable in sources
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
        public void XmlTest23()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.WriteSchema);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable("ParentTable");
            XmlReadMode mode = XmlReadMode.Auto;
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(string)));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.ReadSchema, mode);
            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal("NewDataSet", table.DataSet.DataSetName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(string), table.Columns[1].DataType);

            //Check rows
            DataRow row = table.Rows[0];
            Assert.Equal(1, row[0]);
            Assert.Equal("1", row[1]);

            row = table.Rows[1];
            Assert.Equal(2, row[0]);
            Assert.Equal("2", row[1]);

            row = table.Rows[2];
            Assert.Equal(3, row[0]);
            Assert.Equal("3", row[1]);
        }

        [Fact]
        public void XmlTest24()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            //This test is the same for case when the table name is set but no schema is defined

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                DataTable table = new DataTable();

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => table.ReadXml(stream));
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void XmlTest25()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            XmlReadMode mode = XmlReadMode.Auto;
            //Create a table but dont set the table name
            DataTable table = new DataTable();
            //define the table schame partially
            table.Columns.Add("id", typeof(int));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(string.Empty, table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal(XmlReadMode.DiffGram, mode);
        }

        [Fact]
        public void XmlTest26()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            //Create a table and set the table name
            DataTable table = new DataTable("DummyTable");
            //define the table schame partially
            table.Columns.Add(new DataColumn("DummyItem", typeof(string)));

            XmlReadMode mode = XmlReadMode.Auto;

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.DiffGram, mode);
            Assert.Null(table.DataSet);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.Equal(3, table.Rows.Count);

            //Check Rows
            DataRow row = table.Rows[0];
            Assert.Equal("DummyItem 1", row[0]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);

            row = table.Rows[1];
            Assert.Equal("Changed_DummyItem 2", row[0]);
            Assert.Equal(DataRowState.Modified, row.RowState);

            row = table.Rows[2];
            Assert.Equal("DummyItem 3", row[0]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);
        }

        [Fact]
        public void XmlTest27()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            //Create a table and set the table name
            DataTable table = new DataTable("DummyTable");
            //define the table and add an extra column in the table
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("DummyItem", typeof(string)));
            //Add an extra column which does not match any column in the source diffram
            table.Columns.Add(new DataColumn("ExtraColumn", typeof(double)));

            XmlReadMode mode = XmlReadMode.Auto;

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.DiffGram, mode);
            Assert.Null(table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(string), table.Columns[1].DataType);
            Assert.Equal(typeof(double), table.Columns[2].DataType);
            Assert.Equal(3, table.Rows.Count);

            //Check Rows
            DataRow row = table.Rows[0];
            Assert.Equal(1, row[0]);
            Assert.Equal("DummyItem 1", row[1]);
            Assert.Same(DBNull.Value, row[2]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);

            row = table.Rows[1];
            Assert.Equal(2, row[0]);
            Assert.Equal("Changed_DummyItem 2", row[1]);
            Assert.Same(DBNull.Value, row[2]);
            Assert.Equal(DataRowState.Modified, row.RowState);

            row = table.Rows[2];
            Assert.Equal(3, row[0]);
            Assert.Equal("DummyItem 3", row[1]);
            Assert.Same(DBNull.Value, row[2]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);
        }

        [Fact]
        public void XmlTest28()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            //Create a table and set the table name
            DataTable table = new DataTable("DummyTable");
            //define the table schame partially with a column name which does not match with any
            //table columns in the diffgram
            table.Columns.Add(new DataColumn("WrongColumnName", Type.GetType("System.String")));

            XmlReadMode mode = XmlReadMode.Auto;

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                mode = table.ReadXml(stream);
            }

            Assert.Equal(XmlReadMode.DiffGram, mode);
            Assert.Null(table.DataSet);
            Assert.Equal("DummyTable", table.TableName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(string), table.Columns[0].DataType);

            Assert.Equal(3, table.Rows.Count);
            foreach (DataRow row in table.Rows)
                Assert.Same(DBNull.Value, row[0]);
        }

        [Fact]
        public void XmlTest29()
        {
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _parentTable1.WriteXml(stream, XmlWriteMode.DiffGram, true);
            }

            DataTable table1 = new DataTable("ParentTable");
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[0].ColumnName, typeof(int)));
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[1].ColumnName, typeof(string)));
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[2].ColumnName, typeof(int)));

            //ReadXml on a DiffGram will never create any child relation
            XmlReadMode mode = table1.ReadXml(_tempFile);

            Assert.Equal(XmlReadMode.DiffGram, mode);
            Assert.Null(table1.DataSet);
            Assert.Equal("ParentTable", table1.TableName);
            Assert.Equal(3, table1.Columns.Count);
            Assert.Equal(typeof(int), table1.Columns[0].DataType);
            Assert.Equal(typeof(string), table1.Columns[1].DataType);
            Assert.Equal(typeof(int), table1.Columns[2].DataType);
            Assert.Equal(0, table1.ChildRelations.Count);

            Assert.Equal(3, table1.Rows.Count);
            //Check the row
            DataRow row = table1.Rows[0];
            Assert.Equal(1, row[0]);
            Assert.Equal("ParentItem 1", row[1]);
            Assert.Equal(1, row[2]);

            row = table1.Rows[1];
            Assert.Equal(2, row[0]);
            Assert.Equal("ParentItem 2", row[1]);
            Assert.Equal(2, row[2]);

            row = table1.Rows[2];
            Assert.Equal(3, row[0]);
            Assert.Equal("ParentItem 3", row[1]);
            Assert.Equal(3, row[2]);
        }

        [Fact]
        public void XmlTest30()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                _dummyTable.WriteXml(stream, XmlWriteMode.DiffGram);
            }

            Assert.Equal(3, _dummyTable.Rows.Count);

            DataSet dataSet = new DataSet("HelloWorldDataSet");
            DataTable table = new DataTable("DummyTable");
            table.Columns.Add(new DataColumn("DummyItem", typeof(string)));
            dataSet.Tables.Add(table);

            //Call ReadXml on a table which belong to a DataSet
            table.ReadXml(_tempFile);

            Assert.Equal("HelloWorldDataSet", table.DataSet.DataSetName);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.Equal(3, table.Rows.Count);

            //Check Rows
            DataRow row = table.Rows[0];
            Assert.Equal("DummyItem 1", row[0]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);

            row = table.Rows[1];
            Assert.Equal("Changed_DummyItem 2", row[0]);
            Assert.Equal(DataRowState.Modified, row.RowState);

            row = table.Rows[2];
            Assert.Equal("DummyItem 3", row[0]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);
        }

        [Fact]
        public void XmlTest31()
        {
            DataSet ds = new DataSet();
            DataTable parent = new DataTable("Parent");
            parent.Columns.Add(new DataColumn("col1", typeof(int)));
            parent.Columns.Add(new DataColumn("col2", typeof(string)));
            parent.Columns[0].Unique = true;

            DataTable child1 = new DataTable("Child1");
            child1.Columns.Add(new DataColumn("col3", typeof(int)));
            child1.Columns.Add(new DataColumn("col4", typeof(string)));
            child1.Columns.Add(new DataColumn("col5", typeof(int)));
            child1.Columns[2].Unique = true;

            DataTable child2 = new DataTable("Child2");
            child2.Columns.Add(new DataColumn("col6", typeof(int)));
            child2.Columns.Add(new DataColumn("col7"));

            parent.Rows.Add(new object[] { 1, "P_" });
            parent.Rows.Add(new object[] { 2, "P_" });

            child1.Rows.Add(new object[] { 1, "C1_", 3 });
            child1.Rows.Add(new object[] { 1, "C1_", 4 });
            child1.Rows.Add(new object[] { 2, "C1_", 5 });
            child1.Rows.Add(new object[] { 2, "C1_", 6 });

            child2.Rows.Add(new object[] { 3, "C2_" });
            child2.Rows.Add(new object[] { 3, "C2_" });
            child2.Rows.Add(new object[] { 4, "C2_" });
            child2.Rows.Add(new object[] { 4, "C2_" });
            child2.Rows.Add(new object[] { 5, "C2_" });
            child2.Rows.Add(new object[] { 5, "C2_" });
            child2.Rows.Add(new object[] { 6, "C2_" });
            child2.Rows.Add(new object[] { 6, "C2_" });

            ds.Tables.Add(parent);
            ds.Tables.Add(child1);
            ds.Tables.Add(child2);

            DataRelation relation = new DataRelation("Relation1", parent.Columns[0], child1.Columns[0]);
            parent.ChildRelations.Add(relation);

            relation = new DataRelation("Relation2", child1.Columns[2], child2.Columns[0]);
            child1.ChildRelations.Add(relation);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                parent.WriteXml(stream, XmlWriteMode.WriteSchema, true);
            }

            DataTable table = new DataTable();
            table.ReadXml(_tempFile);

            Assert.Equal("Parent", table.TableName);
            Assert.Equal("NewDataSet", table.DataSet.DataSetName);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(string), table.Columns[1].DataType);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
            Assert.Equal(1, table.ChildRelations.Count);
            Assert.Equal("Relation1", table.ChildRelations[0].RelationName);
            Assert.Equal("Parent", table.ChildRelations[0].ParentTable.TableName);
            Assert.Equal("Child1", table.ChildRelations[0].ChildTable.TableName);

            DataTable table1 = table.ChildRelations[0].ChildTable;
            Assert.Equal("Child1", table1.TableName);
            Assert.Equal("NewDataSet", table1.DataSet.DataSetName);
            Assert.Equal(3, table1.Columns.Count);
            Assert.Equal(4, table1.Rows.Count);
            Assert.Equal(typeof(int), table1.Columns[0].DataType);
            Assert.Equal(typeof(string), table1.Columns[1].DataType);
            Assert.Equal(typeof(int), table1.Columns[2].DataType);
            Assert.Equal(2, table1.Constraints.Count);
            Assert.Equal(typeof(UniqueConstraint), table1.Constraints[0].GetType());
            Assert.Equal(typeof(ForeignKeyConstraint), table1.Constraints[1].GetType());
            Assert.Equal(1, table1.ParentRelations.Count);
            Assert.Equal(1, table1.ChildRelations.Count);
            Assert.Equal("Relation1", table1.ParentRelations[0].RelationName);
            Assert.Equal("Relation2", table1.ChildRelations[0].RelationName);
            Assert.Equal("Parent", table1.ParentRelations[0].ParentTable.TableName);
            Assert.Equal("Child2", table1.ChildRelations[0].ChildTable.TableName);

            table1 = table1.ChildRelations[0].ChildTable;
            Assert.Equal("Child2", table1.TableName);
            Assert.Equal("NewDataSet", table1.DataSet.DataSetName);
            Assert.Equal(2, table1.Columns.Count);
            Assert.Equal(8, table1.Rows.Count);
            Assert.Equal(typeof(int), table1.Columns[0].DataType);
            Assert.Equal(typeof(string), table1.Columns[1].DataType);
            Assert.Equal(1, table1.Constraints.Count);
            Assert.Equal(typeof(ForeignKeyConstraint), table1.Constraints[0].GetType());
            Assert.Equal(1, table1.ParentRelations.Count);
            Assert.Equal(0, table1.ChildRelations.Count);
            Assert.Equal("Relation2", table1.ParentRelations[0].RelationName);
            Assert.Equal("Child1", table1.ParentRelations[0].ParentTable.TableName);
        }
    }
}
