// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
//

using Xunit;
using System.ComponentModel;
using System.Collections;


namespace System.Data.Tests
{
    public class DataRelationCollectionTest2
    {
        private int _changesCounter = 0;

        private void Relations_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            _changesCounter++;
        }

        private DataSet getDataSet()
        {
            var ds = new DataSet();
            DataTable dt1 = DataProvider.CreateParentDataTable();
            DataTable dt2 = DataProvider.CreateChildDataTable();

            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            return ds;
        }

        public DataRelationCollectionTest2()
        {
            _changesCounter = 0;
        }

        [Fact]
        public void AddRange()
        {
            DataSet ds = getDataSet();
            DataRelation[] relArray = new DataRelation[2];

            relArray[0] = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            relArray[1] = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            ds.Relations.AddRange(relArray);

            Assert.Equal(2, ds.Relations.Count);
            Assert.Equal("rel1", ds.Relations[0].RelationName);
            Assert.Equal("rel2", ds.Relations[1].RelationName);

            ds.Relations.AddRange(null);
        }

        [Fact]
        public void Add_ByDataColumns()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add(ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);

            Assert.Equal(1, ds.Relations.Count);

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            Assert.Equal(typeof(UniqueConstraint), ds.Tables[0].Constraints[0].GetType());
            Assert.Equal(typeof(ForeignKeyConstraint), ds.Tables[1].Constraints[0].GetType());
        }

        [Fact]
        public void Add_ByNameDataColumns()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);

            Assert.Equal(1, ds.Relations.Count);

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            Assert.Equal(typeof(UniqueConstraint), ds.Tables[0].Constraints[0].GetType());
            Assert.Equal(typeof(ForeignKeyConstraint), ds.Tables[1].Constraints[0].GetType());

            Assert.Equal("rel1", ds.Relations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[0].ChildRelations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[1].ParentRelations[0].RelationName);
        }

        [Fact]
        public void Add_ByNameDataColumnsWithConstraint()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"], true);

            Assert.Equal(1, ds.Relations.Count);

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count); //When adding a relation);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            Assert.Equal(typeof(UniqueConstraint), ds.Tables[0].Constraints[0].GetType());
            Assert.Equal(typeof(ForeignKeyConstraint), ds.Tables[1].Constraints[0].GetType());

            Assert.Equal("rel1", ds.Relations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[0].ChildRelations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[1].ParentRelations[0].RelationName);
        }
        [Fact]
        public void Add_ByNameDataColumnsWithOutConstraint()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"], false);

            Assert.Equal(1, ds.Relations.Count);

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count); //When adding a relation);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            Assert.Equal(0, ds.Tables[0].Constraints.Count);
            Assert.Equal(0, ds.Tables[1].Constraints.Count);

            Assert.Equal("rel1", ds.Relations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[0].ChildRelations[0].RelationName);
            Assert.Equal("rel1", ds.Tables[1].ParentRelations[0].RelationName);
        }

        [Fact]
        public void CanRemove()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add(ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            Assert.Equal(true, ds.Relations.CanRemove(ds.Relations[0]));
            Assert.Equal(true, ds.Tables[0].ChildRelations.CanRemove(ds.Tables[0].ChildRelations[0]));
            Assert.Equal(true, ds.Tables[1].ParentRelations.CanRemove(ds.Tables[1].ParentRelations[0]));
            Assert.Equal(false, ds.Relations.CanRemove(null));
        }
        [Fact]
        public void CanRemove_DataRelation()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();

            DataRelation rel = new DataRelation("rel1",
                ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);

            Assert.Equal(false, ds1.Relations.CanRemove(rel));
        }

        [Fact]
        public void Clear()
        {
            DataSet ds = getDataSet();

            ds.Relations.Add(ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            ds.Relations.CollectionChanged += new CollectionChangeEventHandler(Relations_CollectionChanged);
            ds.Relations.Clear();
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(1, _changesCounter);
        }

        /// <summary>
        /// Clear was already checked at the clear sub test
        /// </summary>
        [Fact]
        public void CollectionChanged()
        {
            DataSet ds = getDataSet();

            ds.Relations.CollectionChanged += new CollectionChangeEventHandler(Relations_CollectionChanged);

            DataRelation rel = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"]
                , ds.Tables[1].Columns["ParentId"]);

            ds.Relations.Add(rel);

            ds.Relations.Remove(rel);

            Assert.Equal(2, _changesCounter);
        }

        [Fact]
        public void Contains()
        {
            DataSet ds = getDataSet();
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);

            Assert.Equal(true, ds.Relations.Contains("rel1"));
            Assert.Equal(false, ds.Relations.Contains("RelL"));
            Assert.Equal(false, ds.Relations.Contains("rel2"));
        }

        [Fact]
        public void CopyTo()
        {
            DataSet ds = getDataSet();

            DataRelation[] dataRelArray = new DataRelation[2];

            ds.Relations.Add(new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]));

            ds.Relations.CopyTo(dataRelArray, 1);

            Assert.Equal("rel1", dataRelArray[1].RelationName);

            ds.Relations.CopyTo(dataRelArray, 0);

            Assert.Equal("rel1", dataRelArray[0].RelationName);
        }

        [Fact]
        public void Count()
        {
            DataSet ds = getDataSet();
            Assert.Equal(0, ds.Relations.Count);
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            Assert.Equal(1, ds.Relations.Count);
            ds.Relations.Add("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);
            Assert.Equal(2, ds.Relations.Count);
            ds.Relations.Remove("rel2");
            Assert.Equal(1, ds.Relations.Count);
            ds.Relations.Remove("rel1");
            Assert.Equal(0, ds.Relations.Count);
        }

        [Fact]
        public void GetEnumerator()
        {
            DataSet ds = getDataSet();
            int counter = 0;
            ds.Relations.Add("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            ds.Relations.Add("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            IEnumerator myEnumerator = ds.Relations.GetEnumerator();

            while (myEnumerator.MoveNext())
            {
                counter++;
                Assert.Equal("rel", ((DataRelation)myEnumerator.Current).RelationName.Substring(0, 3));
            }
            Assert.Equal(2, counter);
        }

        [Fact]
        public void IndexOf_ByDataRelation()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();

            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);
            DataRelation rel3 = new DataRelation("rel3", ds1.Tables[0].Columns["ParentId"], ds1.Tables[1].Columns["ParentId"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal(1, ds.Relations.IndexOf(rel2));
            Assert.Equal(0, ds.Relations.IndexOf(rel1));
            Assert.Equal(-1, ds.Relations.IndexOf((DataRelation)null));
            Assert.Equal(-1, ds.Relations.IndexOf(rel3));
        }

        [Fact]
        public void IndexOf_ByDataRelationName()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();

            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);
            DataRelation rel3 = new DataRelation("rel3", ds1.Tables[0].Columns["ParentId"], ds1.Tables[1].Columns["ParentId"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal(1, ds.Relations.IndexOf("rel2"));
            Assert.Equal(0, ds.Relations.IndexOf("rel1"));
            Assert.Equal(-1, ds.Relations.IndexOf((string)null));
            Assert.Equal(-1, ds.Relations.IndexOf("rel3"));
        }

        [Fact]
        public void Item()
        {
            DataSet ds = getDataSet();
            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal("rel1", ds.Relations["rel1"].RelationName);
            Assert.Equal("rel2", ds.Relations["rel2"].RelationName);
            Assert.Equal("rel1", ds.Relations[0].RelationName);
            Assert.Equal("rel2", ds.Relations[1].RelationName);
        }

        [Fact]
        public void Add_DataColumn1()
        {
            DataSet ds = DataProvider.CreateForigenConstraint();
            int originalRelationsCount = ds.Relations.Count;

            DataRelation rel = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"]
                , ds.Tables[1].Columns["ParentId"]);

            ds.Relations.Add(rel);

            Assert.Equal(originalRelationsCount + 1, ds.Relations.Count);

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            Assert.Equal(typeof(UniqueConstraint), ds.Tables[0].Constraints[0].GetType());
            Assert.Equal(typeof(ForeignKeyConstraint), ds.Tables[1].Constraints[0].GetType());

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                ds.Relations.Add(rel);
            });

            ds.Relations.Add(null);
        }

        [Fact]
        public void Add_DataColumn2()
        {
            DataSet ds = GetDataSet();
            DataTable dt1 = ds.Tables[0];
            DataTable dt2 = ds.Tables[1];

            dt1.ChildRelations.Add(new DataRelation("rel1", dt1.Columns["ParentId"], dt2.Columns["ParentId"]));

            Assert.Equal(1, dt1.ChildRelations.Count);
            Assert.Equal(1, dt2.ParentRelations.Count);
            Assert.Equal(1, ds.Relations.Count);
        }

        [Fact]
        public void Add_DataColumn3()
        {
            DataSet ds = GetDataSet();

            ds.Tables[1].ParentRelations.Add(new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]));

            Assert.Equal(1, ds.Tables[0].ChildRelations.Count);
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);
            Assert.Equal(1, ds.Relations.Count);
        }

        [Fact]
        public void Add_DataColumn4()
        {
            DataSet ds = GetDataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("ParentId");
            Assert.Throws<InvalidConstraintException>(() =>
            {
                ds.Relations.Add(new DataRelation("rel1", dt.Columns[0], ds.Tables[0].Columns[0]));
            });
        }

        [Fact]
        public void Add_DataColumn5()
        {
            DataSet ds = GetDataSet();
        }

        private DataSet GetDataSet()
        {
            var ds = new DataSet();
            DataTable dt1 = DataProvider.CreateParentDataTable();
            DataTable dt2 = DataProvider.CreateChildDataTable();

            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            return ds;
        }

        [Fact]
        public void Remove_DataColumn()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();
            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal(2, ds.Relations.Count);

            ds.Relations.CollectionChanged += new CollectionChangeEventHandler(Relations_CollectionChanged);
            //Perform remove

            ds.Relations.Remove(rel1);
            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal("rel2", ds.Relations[0].RelationName);
            ds.Relations.Remove(rel2);
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(2, _changesCounter);
            ds.Relations.Remove((DataRelation)null);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataRelation rel3 = new DataRelation("rel3", ds1.Tables[0].Columns["ParentId"], ds1.Tables[1].Columns["ParentId"]);
                ds.Relations.Remove(rel3);
            });
        }

        [Fact]
        public void Remove_String()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();
            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal(ds.Relations.Count, 2);

            ds.Relations.CollectionChanged += new CollectionChangeEventHandler(Relations_CollectionChanged);
            //Perform remove

            ds.Relations.Remove("rel1");
            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal("rel2", ds.Relations[0].RelationName);
            ds.Relations.Remove("rel2");
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(2, _changesCounter);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                ds.Relations.Remove((string)null);
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                ds.Relations.Remove("rel3");
            });
        }

        private void RemoveAt()
        {
            DataSet ds = getDataSet();
            DataSet ds1 = getDataSet();
            DataRelation rel1 = new DataRelation("rel1", ds.Tables[0].Columns["ParentId"], ds.Tables[1].Columns["ParentId"]);
            DataRelation rel2 = new DataRelation("rel2", ds.Tables[0].Columns["String1"], ds.Tables[1].Columns["String1"]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            Assert.Equal(2, ds.Relations.Count);

            ds.Relations.CollectionChanged += new CollectionChangeEventHandler(Relations_CollectionChanged);
            //Perform remove

            ds.Relations.RemoveAt(0);
            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal("rel2", ds.Relations[0].RelationName);
            ds.Relations.RemoveAt(0);
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(2, _changesCounter);


            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                ds.Relations.RemoveAt(-1);
            });
        }
    }
}
