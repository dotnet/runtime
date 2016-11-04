// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
using System.Xml;
using System.Xml.Serialization;

using Xunit;

namespace System.Data.Tests
{
    public class DataTableTest5 : IDisposable
    {
        private string _tempFile;
        private DataSet _dataSet;
        private DataTable _dummyTable;
        private DataTable _parentTable1;
        private DataTable _childTable;
        private DataTable _secondChildTable;

        public DataTableTest5()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (_tempFile != null)
                File.Delete(_tempFile);
        }

        private void WriteXmlSerializable(Stream s, DataTable dt)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (XmlWriter xw = XmlWriter.Create(s, ws))
            {
                IXmlSerializable idt = dt;
                xw.WriteStartElement("start");
                idt.WriteXml(xw);
                xw.WriteEndElement();
                xw.Close();
            }
        }

        private void ReadXmlSerializable(Stream s, DataTable dt)
        {
            using (XmlReader xr = XmlReader.Create(s))
            {
                ReadXmlSerializable(dt, xr);
            }
        }

        private static void ReadXmlSerializable(DataTable dt, XmlReader xr)
        {
            XmlSerializer serializer = new XmlSerializer(dt.GetType());
            IXmlSerializable idt = dt;
            idt.ReadXml(xr);
            xr.Close();
        }

        private void ReadXmlSerializable(string fileName, DataTable dt)
        {
            using (XmlReader xr = XmlReader.Create(fileName))
            {
                ReadXmlSerializable(dt, xr);
            }
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

        private void MakeDataRelation(DataTable dt)
        {
            DataColumn parentColumn = dt.Columns["id"];
            DataColumn childColumn = _dataSet.Tables["ChildTable"].Columns["ParentID"];
            DataRelation relation = new DataRelation("ParentChild_Relation1", parentColumn, childColumn);
            _dataSet.Tables["ChildTable"].ParentRelations.Add(relation);

            DataColumn[] parentColumn1 = new DataColumn[2];
            DataColumn[] childColumn1 = new DataColumn[2];

            parentColumn1[0] = dt.Columns["id"];
            parentColumn1[1] = dt.Columns["DepartmentID"];

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
            Assert.Equal(false, table.CaseSensitive);
            Assert.Equal(tableName, table.TableName);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal(string.Empty, table.Prefix);
            Assert.Equal(2, table.Constraints.Count);
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
            Assert.Equal(false, col.AllowDBNull);
            Assert.Equal(false, col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("id", col.Caption);
            Assert.Equal("id", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(0, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.Equal(true, col.Unique);

            //Second Column
            col = table.Columns[1];
            Assert.Equal(true, col.AllowDBNull);
            Assert.Equal(false, col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ParentItem", col.Caption);
            Assert.Equal("ParentItem", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(1, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.Equal(false, col.Unique);

            //Third Column
            col = table.Columns[2];
            Assert.Equal(false, col.AllowDBNull);
            Assert.Equal(false, col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("DepartmentID", col.Caption);
            Assert.Equal("DepartmentID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ParentTable", col.Table.ToString());
            Assert.Equal(false, col.Unique);

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

        [Fact]
        public void XmlTest1()
        {
            //Make a table without any relations
            MakeParentTable1();
            _dataSet.Tables.Remove(_parentTable1);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataTable table = new DataTable("ParentTable");
            //Read the Xml and the Schema into a table which does not belongs to any DataSet
            ReadXmlSerializable(_tempFile, table);
            VerifyTableSchema(table, _parentTable1.TableName, null);//parentTable1.DataSet);
        }

        [Fact]
        public void XmlTest2()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataTable table = new DataTable("ParentTable");
            DataSet ds = new DataSet("XmlDataSet");
            ds.Tables.Add(table);
            //Read the Xml and the Schema into a table which already belongs to a DataSet
            //and the table name matches with the table in the source XML 
            ReadXmlSerializable(_tempFile, table);
            VerifyTableSchema(table, _parentTable1.TableName, ds);
        }

        [Fact]
        public void XmlTest3()
        {
            //Create a parent table and create child tables
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            //Relate the parent and the children
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataTable table = new DataTable();
            ReadXmlSerializable(_tempFile, table);
            VerifyTableSchema(table, _parentTable1.TableName, null);
        }

        [Fact]
        public void XmlTest4()
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
                WriteXmlSerializable(stream, _childTable);
            }

            DataTable table = new DataTable();
            ReadXmlSerializable(_tempFile, table);

            //Test Schema 
            //Check Properties of Table
            Assert.Equal(string.Empty, table.Namespace);
            Assert.Null(table.DataSet);
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(false, table.CaseSensitive);
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
            Assert.Equal(true, col.AllowDBNull);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ChildID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(0, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.Equal(true, col.Unique);

            //Second Column
            col = table.Columns[1];
            Assert.Equal(true, col.AllowDBNull);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ChildItem", col.Caption);
            Assert.Equal("ChildItem", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(1, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.Equal(false, col.Unique);

            //Third Column
            col = table.Columns[2];
            Assert.Equal(true, col.AllowDBNull);
            Assert.Equal(false, col.AutoIncrement);
            Assert.Equal(0, col.AutoIncrementSeed);
            Assert.Equal(1, col.AutoIncrementStep);
            Assert.Equal("Element", col.ColumnMapping.ToString());
            Assert.Equal("ParentID", col.Caption);
            Assert.Equal("ParentID", col.ColumnName);
            Assert.Equal(typeof(int), col.DataType);
            Assert.Equal(string.Empty, col.DefaultValue.ToString());
            Assert.Equal(false, col.DesignMode);
            Assert.Equal("System.Data.PropertyCollection", col.ExtendedProperties.ToString());
            Assert.Equal(-1, col.MaxLength);
            Assert.Equal(2, col.Ordinal);
            Assert.Equal(string.Empty, col.Prefix);
            Assert.Equal("ChildTable", col.Table.ToString());
            Assert.Equal(false, col.Unique);

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

        [Fact]
        public void XmlTest5()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
                stream.Close();
            }

            DataTable table = new DataTable("ParentTable");
            DataSet dataSet = new DataSet("XmlDataSet");
            dataSet.Tables.Add(table);
            table.Columns.Add(new DataColumn("id", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.NotNull(table.DataSet);

            //Check Rows
            DataRow row = table.Rows[0];
            Assert.Equal("1", row[0]);

            row = table.Rows[1];
            Assert.Equal("2", row[0]);

            row = table.Rows[2];
            Assert.Equal("3", row[0]);
        }

        [Fact]
        public void XmlTest6()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
                stream.Close();
            }

            //Create a target table which has nomatching column(s) names
            DataTable table = new DataTable("ParentTable");
            DataSet dataSet = new DataSet("XmlDataSet");
            dataSet.Tables.Add(table);
            table.Columns.Add(new DataColumn("sid", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                // ReadXml does not read anything as the column 
                // names are not matching
                ReadXmlSerializable(stream, table);
            }

            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Columns.Count);
            Assert.Equal("sid", table.Columns[0].ColumnName);
            Assert.Equal(typeof(string), table.Columns[0].DataType);
            Assert.NotNull(table.DataSet);
        }

        [Fact]
        public void XmlTest7()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
                stream.Close();
            }

            //Create a target table which has matching
            // column(s) name and an extra column
            DataTable table = new DataTable("ParentTable");
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("ParentItem", typeof(string)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(int)));
            table.Columns.Add(new DataColumn("DummyColumn", typeof(string)));
            DataSet dataSet = new DataSet("XmlDataSet");
            dataSet.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(4, table.Columns.Count);
            Assert.NotNull(table.DataSet);

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

            row = table.Rows[1];
            Assert.Equal(2, row["id"]);
            Assert.Equal("ParentItem 2", row["ParentItem"]);
            Assert.Equal(2, row["DepartmentID"]);

            row = table.Rows[2];
            Assert.Equal(3, row["id"]);
            Assert.Equal("ParentItem 3", row["ParentItem"]);
            Assert.Equal(3, row["DepartmentID"]);
        }

        [Fact]
        public void XmlTest8()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataTable table = new DataTable("ParentTable");
            DataSet dataSet = new DataSet("XmlDataSet");
            dataSet.Tables.Add(table);
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(int)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

            Assert.Equal("ParentTable", table.TableName);
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(int), table.Columns[1].DataType);
            Assert.NotNull(table.DataSet);

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
        public void XmlTest9()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable("ParentTable");
            table.Columns.Add(new DataColumn("id", typeof(int)));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

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
        public void XmlTest10()
        {
            MakeParentTable1();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataSet ds = new DataSet();
            DataTable table = new DataTable("ParentTable");
            table.Columns.Add(new DataColumn("id", typeof(int)));
            table.Columns.Add(new DataColumn("DepartmentID", typeof(string)));
            ds.Tables.Add(table);

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

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
        public void XmlTest11()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _dummyTable);
            }

            //Create a table and set the table name
            DataTable table = new DataTable("DummyTable");
            //define the table schame partially
            table.Columns.Add(new DataColumn("DummyItem", typeof(string)));

            using (FileStream stream = new FileStream(_tempFile, FileMode.Open))
            {
                ReadXmlSerializable(stream, table);
            }

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
        public void XmlTest14()
        {
            MakeParentTable1();
            MakeChildTable();
            MakeSecondChildTable();
            MakeDataRelation();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _parentTable1);
            }

            DataTable table1 = new DataTable("ParentTable");
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[0].ColumnName, typeof(int)));
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[1].ColumnName, typeof(string)));
            table1.Columns.Add(new DataColumn(_parentTable1.Columns[2].ColumnName, typeof(int)));

            ReadXmlSerializable(_tempFile, table1);

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
        public void XmlTest15()
        {
            MakeDummyTable();

            using (FileStream stream = new FileStream(_tempFile, FileMode.Create))
            {
                WriteXmlSerializable(stream, _dummyTable);
            }

            Assert.Equal(3, _dummyTable.Rows.Count);

            DataSet dataSet = new DataSet("HelloWorldDataSet");
            DataTable table = new DataTable("DummyTable");
            table.Columns.Add(new DataColumn("DummyItem", typeof(string)));
            dataSet.Tables.Add(table);

            //Call ReadXml on a table which belong to a DataSet
            ReadXmlSerializable(_tempFile, table);

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
        public void XmlTest16()
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
                WriteXmlSerializable(stream, parent);
            }

            DataTable table = new DataTable();
            ReadXmlSerializable(_tempFile, table);

            Assert.Equal("Parent", table.TableName);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(string), table.Columns[1].DataType);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal(typeof(UniqueConstraint), table.Constraints[0].GetType());
        }
    }
}
