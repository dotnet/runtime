// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
            DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(Relation);

            DataRow Row = _mom.NewRow();
            Row[0] = "Teresa";
            Row[1] = "Jack";
            _mom.Rows.Add(Row);

            Row = _mom.NewRow();
            Row[0] = "Teresa";
            Row[1] = "Dick";
            _mom.Rows.Add(Row);

            Row = _mom.NewRow();
            Row[0] = "Mary";
            Row[1] = "Harry";

            Row = _child.NewRow();
            Row[0] = "Jack";
            Row[1] = 16;
            _child.Rows.Add(Row);

            Row = _child.NewRow();
            Row[0] = "Dick";
            Row[1] = 56;
            _child.Rows.Add(Row);

            Assert.Equal(2, _child.Rows.Count);

            Row = _mom.Rows[0];
            Row.Delete();

            Assert.Equal(1, _child.Rows.Count);

            Row = _mom.NewRow();
            Row[0] = "Teresa";
            Row[1] = "Dick";

            try
            {
                _mom.Rows.Add(Row);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.IsType<ConstraintException>(e);
            }

            Row = _mom.NewRow();
            Row[0] = "Teresa";
            Row[1] = "Mich";
            _mom.Rows.Add(Row);
            Assert.Equal(1, _child.Rows.Count);

            Row = _child.NewRow();
            Row[0] = "Jack";
            Row[1] = 16;

            Assert.Throws<InvalidConstraintException>(() => _child.Rows.Add(Row));
        }

        [Fact]
        public void InvalidConstraintException()
        {
            Assert.Throws<InvalidConstraintException>(() =>
           {
               // Parent Columns and Child Columns don't have type-matching columns.
               DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[1], true);
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
            DataRelation Relation;
            Assert.Equal(0, _set.Relations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(0, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ParentRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);

            Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(Relation);

            Assert.Equal(1, _set.Relations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);

            Relation = _set.Relations[0];
            Assert.Equal(1, Relation.ParentColumns.Length);
            Assert.Equal(1, Relation.ChildColumns.Length);
            Assert.Equal("Rel", Relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal("Constraint1", Relation.ParentKeyConstraint.ConstraintName);
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
            DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(Relation);
            DataRelation Test = null;
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            Test = _child.ParentRelations[0];
            Assert.Equal("Rel", Test.ToString());
            Assert.Equal("Rel", Test.RelationName);
            Assert.Equal("Mom", Test.ParentTable.TableName);
            Assert.Equal(1, Test.ParentKeyConstraint.Columns.Length);
            Assert.False(Test.ParentKeyConstraint.IsPrimaryKey);
            Assert.Equal(1, Test.ParentColumns.Length);
            Assert.False(Test.Nested);
            Assert.Equal(0, Test.ExtendedProperties.Count);
            Assert.Equal("Child", Test.ChildTable.TableName);
            Assert.Equal("Rel", Test.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, Test.ChildColumns.Length);
        }

        [Fact]
        public void Creation2()
        {
            DataSet Set = new DataSet();
            DataTable Mom2 = new DataTable("Mom");
            DataTable Child2 = new DataTable("Child");
            DataTable Hubby = new DataTable("Hubby");
            Set.Tables.Add(Mom2);
            Set.Tables.Add(Child2);
            Set.Tables.Add(Hubby);

            DataColumn Col = new DataColumn("Name");
            DataColumn Col2 = new DataColumn("ChildName");
            DataColumn Col3 = new DataColumn("hubby");
            Mom2.Columns.Add(Col);
            Mom2.Columns.Add(Col2);
            Mom2.Columns.Add(Col3);

            DataColumn Col4 = new DataColumn("Name");
            DataColumn Col5 = new DataColumn("Age");
            DataColumn Col6 = new DataColumn("father");
            Child2.Columns.Add(Col4);
            Child2.Columns.Add(Col5);
            Child2.Columns.Add(Col6);


            DataColumn Col7 = new DataColumn("Name");
            DataColumn Col8 = new DataColumn("Age");
            Hubby.Columns.Add(Col7);
            Hubby.Columns.Add(Col8);

            DataColumn[] Parents = new DataColumn[2];
            Parents[0] = Col2;
            Parents[1] = Col3;
            DataColumn[] Childs = new DataColumn[2];
            Childs[0] = Col4;
            Childs[1] = Col7;

            Assert.Throws<InvalidConstraintException>(() => new DataRelation("Rel", Parents, Childs));

            Childs[1] = Col6;

            Set.Relations.Add(new DataRelation("Rel", Parents, Childs));

            Assert.Equal(1, Mom2.ChildRelations.Count);
            Assert.Equal(0, Child2.ChildRelations.Count);
            Assert.Equal(0, Mom2.ParentRelations.Count);
            Assert.Equal(1, Child2.ParentRelations.Count);

            DataRelation Test = Child2.ParentRelations[0];
            Assert.Equal("Rel", Test.ToString());
            Assert.Equal("Rel", Test.RelationName);
            Assert.Equal("Mom", Test.ParentTable.TableName);
            Assert.Equal(2, Test.ParentKeyConstraint.Columns.Length);
            Assert.False(Test.ParentKeyConstraint.IsPrimaryKey);
            Assert.Equal(2, Test.ParentColumns.Length);
            Assert.False(Test.Nested);
            Assert.Equal(0, Test.ExtendedProperties.Count);
            Assert.Equal("Child", Test.ChildTable.TableName);
            Assert.Equal("Rel", Test.ChildKeyConstraint.ConstraintName);
            Assert.Equal(2, Test.ChildColumns.Length);
            Assert.Equal(1, Mom2.Constraints.Count);
            Assert.Equal("Constraint1", Mom2.Constraints[0].ToString());
            Assert.Equal(1, Child2.Constraints.Count);
            Assert.Equal(0, Hubby.Constraints.Count);
        }

        [Fact]
        public void Creation3()
        {
            DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0], false);
            _set.Relations.Add(Relation);
            DataRelation Test = null;

            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            Test = _child.ParentRelations[0];

            Assert.Equal("Rel", Test.ToString());
            Assert.Equal("Rel", Test.RelationName);
            Assert.Equal("Mom", Test.ParentTable.TableName);

            Assert.Null(Test.ParentKeyConstraint);
            Assert.Null(Test.ParentKeyConstraint);

            Assert.Equal(1, Test.ParentColumns.Length);
            Assert.False(Test.Nested);
            Assert.Equal(0, Test.ExtendedProperties.Count);
            Assert.Equal("Child", Test.ChildTable.TableName);

            Assert.Null(Test.ChildKeyConstraint);
            Assert.Equal(1, Test.ChildColumns.Length);
            Assert.Equal(0, _mom.Constraints.Count);
            Assert.Equal(0, _child.Constraints.Count);
        }

        [Fact]
        public void Creation4()
        {
            DataRelation Relation = new DataRelation("Rel", "Mom", "Child",
                                                      new string[] { "ChildName" },
                                                      new string[] { "Name" }, true);

            Assert.Throws<NullReferenceException>(() => _set.Relations.Add(Relation));
            Assert.Throws<NullReferenceException>(() => _set.Relations.AddRange(new DataRelation[] { Relation }));

            _set.BeginInit();
            _set.Relations.AddRange(new DataRelation[] { Relation });
            _set.EndInit();

            DataRelation Test = null;
            Assert.Equal(1, _mom.ChildRelations.Count);
            Assert.Equal(0, _child.ChildRelations.Count);
            Assert.Equal(0, _mom.ParentRelations.Count);
            Assert.Equal(1, _child.ParentRelations.Count);

            Test = _child.ParentRelations[0];
            Assert.Equal("Rel", Test.ToString());
            Assert.Equal("Rel", Test.RelationName);
            Assert.Equal("Mom", Test.ParentTable.TableName);

            Assert.Null(Test.ParentKeyConstraint);

            Assert.Equal(1, Test.ParentColumns.Length);
            Assert.True(Test.Nested);
            Assert.Equal(0, Test.ExtendedProperties.Count);
            Assert.Equal("Child", Test.ChildTable.TableName);
            Assert.Null(Test.ChildKeyConstraint);
            Assert.Equal(1, Test.ChildColumns.Length);
        }

        [Fact]
        public void RelationFromSchema()
        {
            DataSet Set = new DataSet();
            Set.ReadXmlSchema(new StringReader(DataProvider.store));
            DataTable Table = Set.Tables[0];

            Assert.False(Table.CaseSensitive);
            Assert.Equal(1, Table.ChildRelations.Count);
            Assert.Equal(0, Table.ParentRelations.Count);
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal(1, Table.PrimaryKey.Length);
            Assert.Equal(0, Table.Rows.Count);
            Assert.Equal("bookstore", Table.TableName);
            Assert.Equal(1, Table.Columns.Count);

            DataRelation Relation = Table.ChildRelations[0];
            Assert.Equal(1, Relation.ChildColumns.Length);
            Assert.Equal("bookstore_book", Relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, Relation.ChildKeyConstraint.Columns.Length);
            Assert.Equal("book", Relation.ChildTable.TableName);
            Assert.Equal("NewDataSet", Relation.DataSet.DataSetName);
            Assert.Equal(0, Relation.ExtendedProperties.Count);
            Assert.True(Relation.Nested);
            Assert.Equal(1, Relation.ParentColumns.Length);
            Assert.Equal("Constraint1", Relation.ParentKeyConstraint.ConstraintName);
            Assert.Equal("bookstore", Relation.ParentTable.TableName);
            Assert.Equal("bookstore_book", Relation.RelationName);

            Table = Set.Tables[1];

            Assert.False(Table.CaseSensitive);
            Assert.Equal(1, Table.ChildRelations.Count);
            Assert.Equal(1, Table.ParentRelations.Count);
            Assert.Equal(2, Table.Constraints.Count);
            Assert.Equal(1, Table.PrimaryKey.Length);
            Assert.Equal(0, Table.Rows.Count);
            Assert.Equal("book", Table.TableName);
            Assert.Equal(5, Table.Columns.Count);

            Relation = Table.ChildRelations[0];
            Assert.Equal(1, Relation.ChildColumns.Length);
            Assert.Equal("book_author", Relation.ChildKeyConstraint.ConstraintName);
            Assert.Equal(1, Relation.ChildKeyConstraint.Columns.Length);
            Assert.Equal("author", Relation.ChildTable.TableName);
            Assert.Equal("NewDataSet", Relation.DataSet.DataSetName);
            Assert.Equal(0, Relation.ExtendedProperties.Count);
            Assert.True(Relation.Nested);
            Assert.Equal(1, Relation.ParentColumns.Length);
            Assert.Equal("Constraint1", Relation.ParentKeyConstraint.ConstraintName);
            Assert.Equal("book", Relation.ParentTable.TableName);
            Assert.Equal("book_author", Relation.RelationName);

            Table = Set.Tables[2];
            Assert.False(Table.CaseSensitive);
            Assert.Equal(0, Table.ChildRelations.Count);
            Assert.Equal(1, Table.ParentRelations.Count);
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal(0, Table.PrimaryKey.Length);
            Assert.Equal(0, Table.Rows.Count);
            Assert.Equal("author", Table.TableName);
            Assert.Equal(3, Table.Columns.Count);
        }

        [Fact]
        public void ChildRows()
        {
            DataRelation Relation = new DataRelation("Rel", _mom.Columns[1], _child.Columns[0]);
            _set.Relations.Add(Relation);

            DataRow TempRow = _mom.NewRow();
            TempRow[0] = "teresa";
            TempRow[1] = "john";
            _mom.Rows.Add(TempRow);

            TempRow = _mom.NewRow();
            TempRow[0] = "teresa";
            TempRow[1] = "Dick";
            _mom.Rows.Add(TempRow);

            TempRow = _child.NewRow();
            TempRow[0] = "john";
            TempRow[1] = "15";
            _child.Rows.Add(TempRow);

            TempRow = _child.NewRow();
            TempRow[0] = "Dick";
            TempRow[1] = "10";
            _child.Rows.Add(TempRow);

            DataRow Row = _mom.Rows[1];
            TempRow = Row.GetChildRows("Rel")[0];
            Assert.Equal("Dick", TempRow[0]);
            Assert.Equal("10", TempRow[1].ToString());
            TempRow = TempRow.GetParentRow("Rel");
            Assert.Equal("teresa", TempRow[0]);
            Assert.Equal("Dick", TempRow[1]);

            Row = _child.Rows[0];
            TempRow = Row.GetParentRows("Rel")[0];
            Assert.Equal("teresa", TempRow[0]);
            Assert.Equal("john", TempRow[1]);
        }
    }
}
