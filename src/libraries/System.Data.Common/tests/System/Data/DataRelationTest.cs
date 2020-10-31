// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) 2003 Ville Palo
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
using System.IO;

namespace System.Data.Tests
{
    public class DataRelationTest
    {
        private DataSet _set = null;
        private DataTable _mom = null;
        private DataTable _child = null;

        public DataRelationTest()
        {
            _set = new DataSet();
            _mom = new DataTable("Mom");
            _child = new DataTable("Child");
            _set.Tables.Add(_mom);
            _set.Tables.Add(_child);

            DataColumn Col = new DataColumn("Name");
            DataColumn Col2 = new DataColumn("ChildName");
            _mom.Columns.Add(Col);
            _mom.Columns.Add(Col2);

            DataColumn Col3 = new DataColumn("Name");
            DataColumn Col4 = new DataColumn("Age");
            Col4.DataType = Type.GetType("System.Int16");
            _child.Columns.Add(Col3);
            _child.Columns.Add(Col4);
        }

        [Fact]
        public void Foreign()
        {
            DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(relation);

            DataRow row = _mom.NewRow();
            row[0] = "Teresa";
            row[1] = "Jack";
            _mom.Rows.Add(row);

            row = _mom.NewRow();
            row[0] = "Teresa";
            row[1] = "Dick";
            _mom.Rows.Add(row);

            row = _mom.NewRow();
            row[0] = "Mary";
            row[1] = "Harry";

            row = _child.NewRow();
            row[0] = "Jack";
            row[1] = 16;
            _child.Rows.Add(row);

            row = _child.NewRow();
            row[0] = "Dick";
            row[1] = 56;
            _child.Rows.Add(row);

            Assert.Equal(2, _child.Rows.Count);

            row = _mom.Rows[0];
            row.Delete();

            Assert.Equal(1, _child.Rows.Count);

            row = _mom.NewRow();
            row[0] = "Teresa";
            row[1] = "Dick";

            Assert.Throws<ConstraintException>(() => _mom.Rows.Add(row));

            row = _mom.NewRow();
            row[0] = "Teresa";
            row[1] = "Mich";
            _mom.Rows.Add(row);
            Assert.Equal(1, _child.Rows.Count);

            row = _child.NewRow();
            row[0] = "Jack";
            row[1] = 16;

            Assert.Throws<InvalidConstraintException>(() => _child.Rows.Add(row));
        }

        [Fact]
        public void InvalidConstraintException()
        {
            Assert.Throws<InvalidConstraintException>(() =>
           {
               // Parent Columns and Child Columns don't have type-matching columns.
               DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[1], true);
           });
        }

        [Fact]
        public void InvalidConstraintException2()
        {
            Assert.Throws<InvalidConstraintException>(() =>
           {
               // Parent Columns and Child Columns don't have type-matching columns.
               _child.Columns[1].DataType = _mom.Columns[1].DataType;

               DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[1], true);
               _set.Relations.Add(Relation);
               Assert.Equal(1, _set.Relations.Count);

               _child.Columns[1].DataType = Type.GetType("System.Double");
           });
        }

        [Fact]
        public void DataSetRelations()
        {
            Assert.Equal(0, _set.Relations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(0, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ParentRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);

            DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(relation);

            Assert.Equal(1, _set.Relations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);

            relation = _set.Relations[0];
            Assert.Equal(1, relation.ParentColumns.Length);
            Assert.Equal(1, relation.ChildColumns.Length);
            Assert.Equal("Rel", relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal("Constraint1", relation.ParentKeyConstraint.ConstraintName);
        }

        [Fact]
        public void Constraints()
        {
            Assert.Equal(0, _mom.Constraints.Count);
            Assert.Equal(0, _child.Constraints.Count);

            DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(Relation);

            Assert.Equal(1, _mom.Constraints.Count);
            Assert.Equal(1, _child.Constraints.Count);
            Assert.IsType<ForeignKeyConstraint>(_child.Constraints[0]);
            Assert.IsType<UniqueConstraint>(_mom.Constraints[0]);
        }

        [Fact]
        public void Creation()
        {
            DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(relation);
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            DataRelation test = _child.ParentRelations[0];
            Assert.Equal("Rel", test.ToString());
            Assert.Equal("Rel", test.RelationName);
            Assert.Equal("Mom", test.ParentTable.TableName);
            Assert.Equal(1, test.ParentKeyConstraint.Columns.Length);
            Assert.False(test.ParentKeyConstraint.IsPrimaryKey);
            Assert.Equal(1, test.ParentColumns.Length);
            Assert.False(test.Nested);
            Assert.Equal(0, test.ExtendedProperties.Count);
            Assert.Equal("Child", test.ChildTable.TableName);
            Assert.Equal("Rel", test.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, test.ChildColumns.Length);
        }

        [Fact]
        public void Creation2()
        {
            DataSet set = new DataSet();
            DataTable mom2 = new DataTable("Mom");
            DataTable child2 = new DataTable("Child");
            DataTable hubby = new DataTable("Hubby");
            set.Tables.Add(mom2);
            set.Tables.Add(child2);
            set.Tables.Add(hubby);

            DataColumn col = new DataColumn("Name");
            DataColumn col2 = new DataColumn("ChildName");
            DataColumn col3 = new DataColumn("hubby");
            mom2.Columns.Add(col);
            mom2.Columns.Add(col2);
            mom2.Columns.Add(col3);

            DataColumn col4 = new DataColumn("Name");
            DataColumn col5 = new DataColumn("Age");
            DataColumn col6 = new DataColumn("father");
            child2.Columns.Add(col4);
            child2.Columns.Add(col5);
            child2.Columns.Add(col6);


            DataColumn col7 = new DataColumn("Name");
            DataColumn col8 = new DataColumn("Age");
            hubby.Columns.Add(col7);
            hubby.Columns.Add(col8);

            DataColumn[] Parents = new DataColumn[2];
            Parents[0] = col2;
            Parents[1] = col3;
            DataColumn[] childs = new DataColumn[2];
            childs[0] = col4;
            childs[1] = col7;

            Assert.Throws<InvalidConstraintException>(() => new DataRelation("Rel", Parents, childs));

            childs[1] = col6;

            set.Relations.Add(new DataRelation("Rel", Parents, childs));

            Assert.Equal(1, mom2.ChildRelations.Count);
            Assert.Equal(0, child2.ChildRelations.Count);
            Assert.Equal(0, mom2.ParentRelations.Count);
            Assert.Equal(1, child2.ParentRelations.Count);

            DataRelation test = child2.ParentRelations[0];
            Assert.Equal("Rel", test.ToString());
            Assert.Equal("Rel", test.RelationName);
            Assert.Equal("Mom", test.ParentTable.TableName);
            Assert.Equal(2, test.ParentKeyConstraint.Columns.Length);
            Assert.False(test.ParentKeyConstraint.IsPrimaryKey);
            Assert.Equal(2, test.ParentColumns.Length);
            Assert.False(test.Nested);
            Assert.Equal(0, test.ExtendedProperties.Count);
            Assert.Equal("Child", test.ChildTable.TableName);
            Assert.Equal("Rel", test.ChildKeyConstraint.ConstraintName);
            Assert.Equal(2, test.ChildColumns.Length);
            Assert.Equal(1, mom2.Constraints.Count);
            Assert.Equal("Constraint1", mom2.Constraints[0].ToString());
            Assert.Equal(1, child2.Constraints.Count);
            Assert.Equal(0, hubby.Constraints.Count);
        }

        [Fact]
        public void Creation3()
        {
            DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0], false);
            _set.Relations.Add(relation);
            DataRelation test = null;

            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            test = _child.ParentRelations[0];

            Assert.Equal("Rel", test.ToString());
            Assert.Equal("Rel", test.RelationName);
            Assert.Equal("Mom", test.ParentTable.TableName);

            Assert.Null(test.ParentKeyConstraint);
            Assert.Null(test.ParentKeyConstraint);

            Assert.Equal(1, test.ParentColumns.Length);
            Assert.False(test.Nested);
            Assert.Equal(0, test.ExtendedProperties.Count);
            Assert.Equal("Child", test.ChildTable.TableName);

            Assert.Null(test.ChildKeyConstraint);
            Assert.Equal(1, test.ChildColumns.Length);
            Assert.Equal(0, _mom.Constraints.Count);
            Assert.Equal(0, _child.Constraints.Count);
        }

        [Fact]
        public void Creation4()
        {
            DataRelation relation = new DataRelation("Rel", "Mom", "Child",
                                                      new string[] { "ChildName" },
                                                      new string[] { "Name" }, true);

            Assert.Throws<NullReferenceException>(() => _set.Relations.Add(relation));
            Assert.Throws<NullReferenceException>(() => _set.Relations.AddRange(new DataRelation[] { relation }));

            _set.BeginInit();
            _set.Relations.AddRange(new DataRelation[] { relation });
            _set.EndInit();

            DataRelation test = null;
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            test = _child.ParentRelations[0];
            Assert.Equal("Rel", test.ToString());
            Assert.Equal("Rel", test.RelationName);
            Assert.Equal("Mom", test.ParentTable.TableName);

            Assert.Null(test.ParentKeyConstraint);

            Assert.Equal(1, test.ParentColumns.Length);
            Assert.True(test.Nested);
            Assert.Equal(0, test.ExtendedProperties.Count);
            Assert.Equal("Child", test.ChildTable.TableName);
            Assert.Null(test.ChildKeyConstraint);
            Assert.Equal(1, test.ChildColumns.Length);
        }

        [Fact]
        public void RelationFromSchema()
        {
            DataSet set = new DataSet();
            set.ReadXmlSchema(new StringReader(DataProvider.store));
            DataTable table = set.Tables[0];

            Assert.False(table.CaseSensitive);
            Assert.Equal(1, table.ChildRelations.Count);
            Assert.Equal(0, table.ParentRelations.Count);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal(1, table.PrimaryKey.Length);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal("bookstore", table.TableName);
            Assert.Equal(1, table.Columns.Count);

            DataRelation relation = table.ChildRelations[0];
            Assert.Equal(1, relation.ChildColumns.Length);
            Assert.Equal("bookstore_book", relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, relation.ChildKeyConstraint.Columns.Length);
            Assert.Equal("book", relation.ChildTable.TableName);
            Assert.Equal("NewDataSet", relation.DataSet.DataSetName);
            Assert.Equal(0, relation.ExtendedProperties.Count);
            Assert.True(relation.Nested);
            Assert.Equal(1, relation.ParentColumns.Length);
            Assert.Equal("Constraint1", relation.ParentKeyConstraint.ConstraintName);
            Assert.Equal("bookstore", relation.ParentTable.TableName);
            Assert.Equal("bookstore_book", relation.RelationName);

            table = set.Tables[1];

            Assert.False(table.CaseSensitive);
            Assert.Equal(1, table.ChildRelations.Count);
            Assert.Equal(1, table.ParentRelations.Count);
            Assert.Equal(2, table.Constraints.Count);
            Assert.Equal(1, table.PrimaryKey.Length);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal("book", table.TableName);
            Assert.Equal(5, table.Columns.Count);

            relation = table.ChildRelations[0];
            Assert.Equal(1, relation.ChildColumns.Length);
            Assert.Equal("book_author", relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, relation.ChildKeyConstraint.Columns.Length);
            Assert.Equal("author", relation.ChildTable.TableName);
            Assert.Equal("NewDataSet", relation.DataSet.DataSetName);
            Assert.Equal(0, relation.ExtendedProperties.Count);
            Assert.True(relation.Nested);
            Assert.Equal(1, relation.ParentColumns.Length);
            Assert.Equal("Constraint1", relation.ParentKeyConstraint.ConstraintName);
            Assert.Equal("book", relation.ParentTable.TableName);
            Assert.Equal("book_author", relation.RelationName);

            table = set.Tables[2];
            Assert.False(table.CaseSensitive);
            Assert.Equal(0, table.ChildRelations.Count);
            Assert.Equal(1, table.ParentRelations.Count);
            Assert.Equal(1, table.Constraints.Count);
            Assert.Equal(0, table.PrimaryKey.Length);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal("author", table.TableName);
            Assert.Equal(3, table.Columns.Count);
        }

        [Fact]
        public void ChildRows()
        {
            DataRelation relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(relation);

            DataRow tempRow = _mom.NewRow();
            tempRow[0] = "teresa";
            tempRow[1] = "john";
            _mom.Rows.Add(tempRow);

            tempRow = _mom.NewRow();
            tempRow[0] = "teresa";
            tempRow[1] = "Dick";
            _mom.Rows.Add(tempRow);

            tempRow = _child.NewRow();
            tempRow[0] = "john";
            tempRow[1] = "15";
            _child.Rows.Add(tempRow);

            tempRow = _child.NewRow();
            tempRow[0] = "Dick";
            tempRow[1] = "10";
            _child.Rows.Add(tempRow);

            DataRow row = _mom.Rows[1];
            tempRow = row.GetChildRows("Rel")[0];
            Assert.Equal("Dick", tempRow[0]);
            Assert.Equal("10", tempRow[1].ToString());
            tempRow = tempRow.GetParentRow("Rel");
            Assert.Equal("teresa", tempRow[0]);
            Assert.Equal("Dick", tempRow[1]);

            row = _child.Rows[0];
            tempRow = row.GetParentRows("Rel")[0];
            Assert.Equal("teresa", tempRow[0]);
            Assert.Equal("john", tempRow[1]);
        }
    }
}
