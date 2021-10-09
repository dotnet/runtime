// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2004 Mainsoft Co.
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

using System.Collections;
using System.ComponentModel;
using Xunit;

namespace System.Data.Tests
{
    public class ConstraintCollectionTest2
    {
        private bool _collectionChangedFlag = false;

        [Fact]
        public void CanRemove_ParentForeign()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            Assert.False(ds.Tables["parent"].Constraints.CanRemove(ds.Tables["parent"].Constraints[0]));
        }

        [Fact]
        public void CanRemove_ChildForeign()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            Assert.True(ds.Tables["child"].Constraints.CanRemove(ds.Tables["child"].Constraints[0]));
        }

        [Fact]
        public void CanRemove_ParentAndChildForeign()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            //remove the forigen and ask about the unique
            ds.Tables["child"].Constraints.Remove(ds.Tables["child"].Constraints[0]);
            Assert.True(ds.Tables["parent"].Constraints.CanRemove(ds.Tables["parent"].Constraints[0]));
        }

        [Fact]
        public void Clear_Foreign()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            foreach (DataTable dt in ds.Tables)
            {
                dt.Constraints.Clear();
            }
            Assert.Equal(0, ds.Tables[0].Constraints.Count);
            Assert.Equal(0, ds.Tables[0].Constraints.Count);
        }

        [Fact]
        public void Clear_Unique()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            int rowsCount = dt.Rows.Count;
            dt.Constraints.Clear();
            DataRow dr = dt.NewRow();
            dr[0] = 1;
            dt.Rows.Add(dr);
            Assert.Equal(rowsCount + 1, dt.Rows.Count); // verifying no exception as well
        }

        [Fact]
        public void CollectionChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            _collectionChangedFlag = false;
            dt.Constraints.CollectionChanged += new CollectionChangeEventHandler(Constraints_CollectionChangedHandler);
            dt = DataProvider.CreateUniqueConstraint(dt);
            Assert.True(_collectionChangedFlag);
        }

        [Fact]
        public void Contains_ByName()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();

            //changing the constraints's name

            ds.Tables["child"].Constraints[0].ConstraintName = "name1";
            ds.Tables["parent"].Constraints[0].ConstraintName = "name2";


            Assert.True(ds.Tables["child"].Constraints.Contains("name1"));
            Assert.False(ds.Tables["child"].Constraints.Contains("xxx"));
            Assert.True(ds.Tables["parent"].Constraints.Contains("name2"));
            Assert.False(ds.Tables["parent"].Constraints.Contains("xxx"));
        }

        [Fact]
        public void CopyTo()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.Add("constraint2", dt.Columns["String1"], true);

            var ar = new object[2];

            dt.Constraints.CopyTo(ar, 0);
            Assert.Equal(2, ar.Length);
        }

        [Fact]
        public void Count()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            Assert.Equal(1, dt.Constraints.Count);

            //Add

            dt.Constraints.Add("constraint2", dt.Columns["String1"], false);
            Assert.Equal(2, dt.Constraints.Count);

            //Remove

            dt.Constraints.Remove("constraint2");
            Assert.Equal(1, dt.Constraints.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.Add("constraint2", dt.Columns["String1"], false);

            int counter = 0;
            IEnumerator myEnumerator = dt.Constraints.GetEnumerator();
            while (myEnumerator.MoveNext())
            {
                counter++;
            }
            Assert.Equal(2, counter);
        }

        [Fact]
        public void IndexOf()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            Assert.Equal(0, dt.Constraints.IndexOf(dt.Constraints[0]));

            //Add new constraint
            Constraint con = new UniqueConstraint(dt.Columns["String1"], false);

            dt.Constraints.Add(con);
            Assert.Equal(1, dt.Constraints.IndexOf(con));

            //Remove it and try to look for it

            dt.Constraints.Remove(con);
            Assert.Equal(-1, dt.Constraints.IndexOf(con));
        }

        [Fact]
        public void IndexOf_ByName()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints[0].ConstraintName = "name1";
            Assert.Equal(0, dt.Constraints.IndexOf("name1"));

            //Add new constraint
            Constraint con = new UniqueConstraint(dt.Columns["String1"], false);
            con.ConstraintName = "name2";

            dt.Constraints.Add(con);
            Assert.Equal(1, dt.Constraints.IndexOf("name2"));

            //Remove it and try to look for it

            dt.Constraints.Remove(con);
            Assert.Equal(-1, dt.Constraints.IndexOf("name2"));
        }

        [Fact]
        public void IndexOf_SameColumns()
        {
            var ds = new DataSet();
            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");
            DataColumn pcol = table1.Columns.Add("col1");
            DataColumn ccol = table2.Columns.Add("col1");

            ds.Relations.Add("fk_rel", pcol, ccol);

            var fk = new ForeignKeyConstraint("fk", pcol, ccol);
            Assert.Equal(-1, ds.Tables[1].Constraints.IndexOf(fk));
        }

        [Fact]
        public void Add_RelationFirst_ConstraintNext()
        {
            var ds = new DataSet();
            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");
            DataColumn pcol = table1.Columns.Add("col1");
            DataColumn ccol = table2.Columns.Add("col1");

            ds.Relations.Add("fk_rel", pcol, ccol);

            Assert.Throws<DataException>(() => table2.Constraints.Add("fk_cons", pcol, ccol));
            Assert.Throws<DataException>(() => table1.Constraints.Add("pk_cons", pcol, false));
        }

        [Fact]
        public void Add_ConstraintFirst_RelationNext()
        {
            DataSet ds = new DataSet();
            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");
            DataColumn pcol = table1.Columns.Add("col1");
            DataColumn ccol = table2.Columns.Add("col1");

            table2.Constraints.Add("fk_cons", pcol, ccol);

            // Should not throw DataException
            ds.Relations.Add("fk_rel", pcol, ccol);

            Assert.Equal(1, table2.Constraints.Count);
            Assert.Equal(1, table1.Constraints.Count);
            Assert.Equal("fk_cons", table2.Constraints[0].ConstraintName);
            Assert.Equal("Constraint1", table1.Constraints[0].ConstraintName);
        }

        [Fact]
        public void IsReadOnly()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            Assert.False(dt.Constraints.IsReadOnly);
        }

        [Fact]
        public void IsSynchronized()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            Assert.False(dt.Constraints.IsSynchronized);
        }

        [Fact]
        public void Remove()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.Remove(dt.Constraints[0]);
            Assert.Equal(0, dt.Constraints.Count);
        }

        [Fact]
        public void Remove_CheckUnique()
        {
            DataTable table = new DataTable();
            DataColumn col1 = table.Columns.Add("col1");
            DataColumn col2 = table.Columns.Add("col2");

            Assert.False(col1.Unique);

            Constraint uc = table.Constraints.Add("", col1, false);
            Assert.True(col1.Unique);

            table.Constraints.Remove(uc);
            Assert.False(col1.Unique);

            table.PrimaryKey = new DataColumn[] { col2 };
            AssertExtensions.Throws<ArgumentException>(null, () => table.Constraints.Remove(table.Constraints[0]));
        }

        [Fact]
        public void Remove_ByNameSimple()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints[0].ConstraintName = "constraint1";
            dt.Constraints.Remove("constraint1");
            Assert.Equal(0, dt.Constraints.Count);
        }

        [Fact]
        public void Remove_ByNameWithAdd()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints[0].ConstraintName = "constraint1";
            Constraint con = new UniqueConstraint(dt.Columns["String1"], false);
            dt.Constraints.Add(con);
            dt.Constraints.Remove(con);

            Assert.Equal(1, dt.Constraints.Count);
            Assert.Equal("constraint1", dt.Constraints[0].ConstraintName);
        }

        [Fact]
        public void Remove_CollectionChangedEvent()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            _collectionChangedFlag = false;
            dt.Constraints.CollectionChanged += new CollectionChangeEventHandler(Constraints_CollectionChangedHandler);
            dt.Constraints.Remove(dt.Constraints[0]);
            Assert.True(_collectionChangedFlag);
        }

        [Fact]
        public void Remove_ByNameCollectionChangedEvent()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            _collectionChangedFlag = false;
            dt.Constraints.CollectionChanged += new CollectionChangeEventHandler(Constraints_CollectionChangedHandler);
            dt.Constraints.Remove("constraint1");
            Assert.True(_collectionChangedFlag);
        }

        [Fact]
        public void add_CollectionChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            _collectionChangedFlag = false;
            dt.Constraints.CollectionChanged += new CollectionChangeEventHandler(Constraints_CollectionChangedHandler);
            dt = DataProvider.CreateUniqueConstraint(dt);
            Assert.True(_collectionChangedFlag);
        }

        private void Constraints_CollectionChangedHandler(object sender, CollectionChangeEventArgs e) => _collectionChangedFlag = true;

        [Fact]
        public void Remove_Constraint()
        {
            DataTable table1 = new DataTable("table1");
            DataTable table2 = new DataTable("table2");

            DataColumn col1 = table1.Columns.Add("col1", typeof(int));
            DataColumn col2 = table1.Columns.Add("col2", typeof(int));
            DataColumn col3 = table2.Columns.Add("col1", typeof(int));

            Constraint c1 = table1.Constraints.Add("unique1", col1, false);
            Constraint c2 = table1.Constraints.Add("unique2", col2, false);
            Constraint c3 = table2.Constraints.Add("fk", col1, col3);

            table1.Constraints.Remove(c1);
            table1.Constraints.Remove(c2);
            table2.Constraints.Remove(c3);

            Assert.Equal(0, table1.Constraints.Count);
            Assert.Equal(0, table2.Constraints.Count);

            DataSet ds = new DataSet();
            ds.Tables.Add(table1);
            ds.Tables.Add(table2);

            c1 = table1.Constraints.Add("unique1", col1, false);
            c2 = table1.Constraints.Add("unique2", col2, false);
            c3 = table2.Constraints.Add("fk", col1, col3);

            AssertExtensions.Throws<ArgumentException>(null, () => table1.Constraints.Remove(c1));

            Assert.Equal(2, table1.Constraints.Count);

            table1.Constraints.Remove(c2);
            Assert.Equal(1, table1.Constraints.Count);

            table2.Constraints.Remove(c3);
            Assert.Equal(1, table1.Constraints.Count);
            Assert.Equal(0, table2.Constraints.Count);

            table1.Constraints.Remove(c1);
            Assert.Equal(0, table1.Constraints.Count);
        }

        public delegate void testExceptionMethodCallback();

        [Fact]
        public void Add_Constraint()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            Assert.Equal(1, dt.Constraints.Count);
            Assert.Equal("Constraint1", dt.Constraints[0].ConstraintName);

            DataSet ds = DataProvider.CreateForeignConstraint();
            Assert.Equal(1, ds.Tables[1].Constraints.Count);
            Assert.Equal(1, ds.Tables[0].Constraints.Count);

            var arr = new ArrayList(1);
            arr.Add(new ConstraintException());
            TestException(new testExceptionMethodCallback(DataProvider.TryToBreakUniqueConstraint), arr);

            arr = new ArrayList(1);
            arr.Add(new InvalidConstraintException());
            TestException(new testExceptionMethodCallback(DataProvider.TryToBreakForigenConstraint), arr);
        }

        private static void TestException(testExceptionMethodCallback dlg, IList exceptionList)
        {
            Exception ex = Assert.ThrowsAny<Exception>(() => dlg());
            foreach (Exception expectedEx in exceptionList)
                if ((expectedEx.GetType()) == (ex.GetType()))
                    return;
            Assert.True(false);
        }

        [Fact]
        public void Add_SDB1()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Constraints.Add("UniqueConstraint", dt.Columns["ParentId"], true);
            Assert.Equal(1, (double)dt.Constraints.Count);
            Assert.Equal("UniqueConstraint", dt.Constraints[0].ConstraintName);
        }

        [Fact]
        public void Add_SDB2()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Constraints.Add("UniqueConstraint", dt.Columns["ParentId"], false);
            Assert.Equal(1, dt.Constraints.Count);
            Assert.Equal("UniqueConstraint", dt.Constraints[0].ConstraintName);
        }

        [Fact]
        public void Add_SDB3()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Constraints.Add("UniqueConstraint", dt.Columns["ParentId"], true);
            //Break the constraint

            ArrayList arr = new ArrayList(1);
            arr.Add(new ConstraintException());
            TestException(new testExceptionMethodCallback(DataProvider.TryToBreakUniqueConstraint), arr);
        }

        [Fact]
        public void Add_SDB4()
        {
            Assert.Throws<ConstraintException>(() =>
            {
                DataTable dt = DataProvider.CreateParentDataTable();
                dt.Constraints.Add("UniqueConstraint", dt.Columns["ParentId"], false);
                DataProvider.TryToBreakUniqueConstraint();
                Assert.Equal(2, dt.Select("ParentId=1").Length);
            });
        }

        [Fact]
        public void Add_Constraint_Column_Column()
        {
            DataTable parent = DataProvider.CreateParentDataTable();
            DataTable child = DataProvider.CreateChildDataTable();

            child.Constraints.Add("ForigenConstraint", parent.Columns[0], child.Columns[0]);

            Assert.Equal(1, parent.Constraints.Count);
            Assert.Equal(1, child.Constraints.Count);
            Assert.Equal("ForigenConstraint", child.Constraints[0].ConstraintName);

            parent = DataProvider.CreateParentDataTable();
            child = DataProvider.CreateChildDataTable();

            child.Constraints.Add("ForigenConstraint", parent.Columns[0], child.Columns[0]);

            ArrayList arr = new ArrayList(1);
            arr.Add(new InvalidConstraintException());
            TestException(new testExceptionMethodCallback(DataProvider.TryToBreakForigenConstraint), arr);

            Assert.Equal(1, parent.Constraints.Count);
            Assert.Equal(1, child.Constraints.Count);
        }

        [Fact]
        public void AddRange_C1()
        {
            DataTable dt = new DataTable();
            dt.Constraints.AddRange(null);
            Assert.Equal(0, dt.Constraints.Count);
        }

        [Fact]
        public void AddRange_C2()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Tables[1].Constraints.AddRange(GetConstraintArray(ds)); //Cuz foreign key belongs to child table
            Assert.Equal(2, ds.Tables[1].Constraints.Count);
            Assert.Equal(1, ds.Tables[0].Constraints.Count);
        }

        [Fact]
        public void AddRange_C3()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                var ds = new DataSet();
                ds.Tables.Add(DataProvider.CreateParentDataTable());
                ds.Tables.Add(DataProvider.CreateChildDataTable());
                Constraint badConstraint = new UniqueConstraint(ds.Tables[0].Columns[0]);

                ds.Tables[1].Constraints.AddRange(new Constraint[] { badConstraint }); //Cuz foreign key belongs to child table
            });
        }

        private Constraint[] GetConstraintArray(DataSet ds)
        {
            DataTable parent = ds.Tables[0];
            DataTable child = ds.Tables[1];
            Constraint[] constArray = new Constraint[2];

            //Create unique
            constArray[0] = new UniqueConstraint("Unique1", child.Columns["ChildDouble"]);
            //Create foreign
            constArray[1] = new ForeignKeyConstraint(parent.Columns[0], child.Columns[1]);

            return constArray;
        }

        [Fact]
        public void Item()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints[0].ConstraintName = "constraint1";
            Assert.Equal("constraint1", dt.Constraints[0].ConstraintName);
            Assert.Equal("constraint1", dt.Constraints["constraint1"].ConstraintName);

            ArrayList arr = new ArrayList(1);
            arr.Add(new IndexOutOfRangeException());
            TestException(new testExceptionMethodCallback(Item2), arr);
        }

        private void Item2()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints[1].ConstraintName = "error";
        }

        private bool _collectionChanged = false;

        [Fact]
        public void RemoveAt_Integer()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.RemoveAt(0);
            Assert.Equal(0, dt.Constraints.Count);

            dt = DataProvider.CreateUniqueConstraint();
            Constraint con = new UniqueConstraint(dt.Columns["String1"], false);
            dt.Constraints[0].ConstraintName = "constraint1";
            con.ConstraintName = "constraint2";
            dt.Constraints.Add(con);
            dt.Constraints.RemoveAt(0);
            Assert.Equal(1, dt.Constraints.Count);
            Assert.Equal("constraint2", dt.Constraints[0].ConstraintName);

            dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.CollectionChanged += new CollectionChangeEventHandler(Constraints_CollectionChanged);
            dt.Constraints.RemoveAt(0);
            Assert.True(_collectionChanged);

            ArrayList arr = new ArrayList(1);
            arr.Add(new IndexOutOfRangeException());
            TestException(new testExceptionMethodCallback(RemoveAt_I), arr);
        }

        private void Constraints_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            _collectionChanged = true;
        }

        private void RemoveAt_I()
        {
            DataTable dt = DataProvider.CreateUniqueConstraint();
            dt.Constraints.RemoveAt(2);
        }

        [Fact]
        public void RemoveTest()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1");
            Constraint c = table.Constraints.Add("c", table.Columns[0], false);
            AssertExtensions.Throws<ArgumentException>(null, () => table.Constraints.Remove("sdfs"));

            table.Constraints.Remove(c);
            Assert.Equal(0, table.Constraints.Count);

            // No exception shud be raised
            table.Constraints.Add(c);
            Assert.Equal(1, table.Constraints.Count);
        }
    }
}
