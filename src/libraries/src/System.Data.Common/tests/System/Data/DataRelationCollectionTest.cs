// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Punit Todi

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
    public class DataRelationCollectionTest : IDisposable
    {
        private DataSet _dataset;
        private DataTable _tblparent, _tblchild;
        private DataRelation _relation;

        public DataRelationCollectionTest()
        {
            _dataset = new DataSet();
            _tblparent = new DataTable("Customer");
            _tblchild = new DataTable("Order");
            _dataset.Tables.Add(_tblchild);
            _dataset.Tables.Add(_tblparent);
            _dataset.Tables.Add("Item");
            _dataset.Tables["Customer"].Columns.Add("custid");
            _dataset.Tables["Customer"].Columns.Add("custname");
            _dataset.Tables["Order"].Columns.Add("oid");
            _dataset.Tables["Order"].Columns.Add("custid");
            _dataset.Tables["Order"].Columns.Add("itemid");
            _dataset.Tables["Order"].Columns.Add("desc");
            _dataset.Tables["Item"].Columns.Add("itemid");
            _dataset.Tables["Item"].Columns.Add("desc");
        }

        public void Dispose()
        {
            _dataset.Relations.Clear();
        }

        [Fact]
        public void Add()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataColumn parentCol = _dataset.Tables["Customer"].Columns["custid"];
            DataColumn childCol = _dataset.Tables["Order"].Columns["custid"];
            DataRelation dr = new DataRelation("CustOrder", parentCol, childCol);

            drcol.Add(dr);
            Assert.Equal("CustOrder", drcol[0].RelationName);
            drcol.Clear();

            drcol.Add(parentCol, childCol);
            Assert.Equal(1, drcol.Count);
            drcol.Clear();

            drcol.Add("NewRelation", parentCol, childCol);
            Assert.Equal("NewRelation", drcol[0].RelationName);
            drcol.Clear();

            drcol.Add("NewRelation", parentCol, childCol, false);
            Assert.Equal(1, drcol.Count);
            drcol.Clear();

            drcol.Add("NewRelation", parentCol, childCol, true);
            Assert.Equal(1, drcol.Count);
            drcol.Clear();
        }

        [Fact]
        public void AddException2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataRelationCollection drcol = _dataset.Relations;
               DataRelation dr1 = new DataRelation("CustOrder"
                               , _dataset.Tables["Customer"].Columns["custid"]
                               , _dataset.Tables["Order"].Columns["custid"]);
               drcol.Add(dr1);
               drcol.Add(dr1);
           });
        }

        [Fact]
        public void AddException3()
        {
            Assert.Throws<DuplicateNameException>(() =>
           {
               DataRelationCollection drcol = _dataset.Relations;
               DataRelation dr1 = new DataRelation("DuplicateName"
                               , _dataset.Tables["Customer"].Columns["custid"]
                               , _dataset.Tables["Order"].Columns["custid"]);
               DataRelation dr2 = new DataRelation("DuplicateName"
                               , _dataset.Tables["Item"].Columns["itemid"]
                               , _dataset.Tables["Order"].Columns["custid"]);

               drcol.Add(dr1);
               drcol.Add(dr2);
           });
        }

        [Fact]
        public void AddRange()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataRelation dr1 = new DataRelation("CustOrder"
                            , _dataset.Tables["Customer"].Columns["custid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            DataRelation dr2 = new DataRelation("ItemOrder"
                            , _dataset.Tables["Item"].Columns["itemid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            drcol.AddRange(new DataRelation[] { dr1, dr2 });

            Assert.Equal("CustOrder", drcol[0].RelationName);
            Assert.Equal("ItemOrder", drcol[1].RelationName);
        }

        [Fact]
        public void CanRemove()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataColumn parentCol = _dataset.Tables["Customer"].Columns["custid"];
            DataColumn childCol = _dataset.Tables["Order"].Columns["custid"];
            DataRelation dr = new DataRelation("CustOrder", parentCol, childCol);

            drcol.Add(dr);
            Assert.True(drcol.CanRemove(dr));
            Assert.False(drcol.CanRemove(null));
            DataRelation dr2 = new DataRelation("ItemOrder"
                        , _dataset.Tables["Item"].Columns["itemid"]
                        , _dataset.Tables["Order"].Columns["custid"]);
            Assert.False(drcol.CanRemove(dr2));
        }

        [Fact]
        public void Clear()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataColumn parentCol = _dataset.Tables["Customer"].Columns["custid"];
            DataColumn childCol = _dataset.Tables["Order"].Columns["custid"];
            drcol.Add(new DataRelation("CustOrder", parentCol, childCol));
            drcol.Add("ItemOrder", _dataset.Tables["Item"].Columns["itemid"]
                                 , _dataset.Tables["Order"].Columns["itemid"]);
            drcol.Clear();
            Assert.Equal(0, drcol.Count);
        }

        [Fact]
        public void Contains()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataColumn parentCol = _dataset.Tables["Customer"].Columns["custid"];
            DataColumn childCol = _dataset.Tables["Order"].Columns["custid"];
            DataRelation dr = new DataRelation("CustOrder", parentCol, childCol);

            drcol.Add(dr);
            Assert.True(drcol.Contains(dr.RelationName));
            string drnull = "";
            Assert.False(drcol.Contains(drnull));
            dr = new DataRelation("newRelation", childCol, parentCol);
            Assert.False(drcol.Contains("NoSuchRelation"));
        }

        [Fact]
        public void CopyTo()
        {
            DataRelationCollection drcol = _dataset.Relations;
            drcol.Add("CustOrder"
                    , _dataset.Tables["Customer"].Columns["custid"]
                    , _dataset.Tables["Order"].Columns["custid"]);
            drcol.Add("ItemOrder"
                    , _dataset.Tables["Item"].Columns["itemid"]
                    , _dataset.Tables["Order"].Columns["custid"]);

            DataRelation[] array = new DataRelation[2];
            drcol.CopyTo(array, 0);
            Assert.Equal(2, array.Length);
            Assert.Equal("CustOrder", array[0].RelationName);
            Assert.Equal("ItemOrder", array[1].RelationName);

            DataRelation[] array1 = new DataRelation[4];
            drcol.CopyTo(array1, 2);
            Assert.Null(array1[0]);
            Assert.Null(array1[1]);
            Assert.Equal("CustOrder", array1[2].RelationName);
            Assert.Equal("ItemOrder", array1[3].RelationName);
        }

        [Fact]
        public void Equals()
        {
            DataRelationCollection drcol = _dataset.Relations;
            drcol.Add("CustOrder"
                    , _dataset.Tables["Customer"].Columns["custid"]
                    , _dataset.Tables["Order"].Columns["custid"]);
            drcol.Add("ItemOrder"
                    , _dataset.Tables["Item"].Columns["itemid"]
                    , _dataset.Tables["Order"].Columns["custid"]);
            DataSet newds = new DataSet();
            DataRelationCollection drcol1 = newds.Relations;
            DataRelationCollection drcol2 = _dataset.Relations;

            Assert.True(drcol.Equals(drcol));
            Assert.True(drcol.Equals(drcol2));

            Assert.False(drcol1.Equals(drcol));
            Assert.False(drcol.Equals(drcol1));

            Assert.True(object.Equals(drcol, drcol2));
            Assert.False(object.Equals(drcol, drcol1));
        }

        [Fact]
        public void IndexOf()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataRelation dr1 = new DataRelation("CustOrder"
                            , _dataset.Tables["Customer"].Columns["custid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            DataRelation dr2 = new DataRelation("ItemOrder"
                            , _dataset.Tables["Item"].Columns["itemid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            drcol.Add(dr1);
            drcol.Add(dr2);

            Assert.Equal(0, drcol.IndexOf(dr1));
            Assert.Equal(1, drcol.IndexOf(dr2));

            Assert.Equal(0, drcol.IndexOf("CustOrder"));
            Assert.Equal(1, drcol.IndexOf("ItemOrder"));

            Assert.Equal(0, drcol.IndexOf(drcol[0]));
            Assert.Equal(1, drcol.IndexOf(drcol[1]));

            Assert.Equal(-1, drcol.IndexOf("_noRelation_"));
            DataRelation newdr = new DataRelation("newdr"
                    , _dataset.Tables["Customer"].Columns["custid"]
                    , _dataset.Tables["Order"].Columns["custid"]);

            Assert.Equal(-1, drcol.IndexOf(newdr));
        }

        [Fact]
        public void Remove()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataRelation dr1 = new DataRelation("CustOrder"
                            , _dataset.Tables["Customer"].Columns["custid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            DataRelation dr2 = new DataRelation("ItemOrder"
                            , _dataset.Tables["Item"].Columns["itemid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            drcol.Add(dr1);
            drcol.Add(dr2);

            drcol.Remove(dr1);
            Assert.False(drcol.Contains(dr1.RelationName));
            drcol.Add(dr1);

            drcol.Remove("CustOrder");
            Assert.False(drcol.Contains("CustOrder"));
            drcol.Add(dr1);

            DataRelation drnull = null;
            drcol.Remove(drnull);

            DataRelation newdr = new DataRelation("newdr"
                                , _dataset.Tables["Customer"].Columns["custid"]
                                , _dataset.Tables["Order"].Columns["custid"]);
            AssertExtensions.Throws<ArgumentException>(null, () => drcol.Remove(newdr));
        }

        [Fact]
        public void RemoveAt()
        {
            DataRelationCollection drcol = _dataset.Relations;
            DataRelation dr1 = new DataRelation("CustOrder"
                            , _dataset.Tables["Customer"].Columns["custid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            DataRelation dr2 = new DataRelation("ItemOrder"
                            , _dataset.Tables["Item"].Columns["itemid"]
                            , _dataset.Tables["Order"].Columns["custid"]);
            drcol.Add(dr1);
            drcol.Add(dr2);

            try
            {
                drcol.RemoveAt(-1);
                Assert.False(true);
            }
            catch (IndexOutOfRangeException e)
            {
            }
            try
            {
                drcol.RemoveAt(101);
                Assert.False(true);
            }
            catch (IndexOutOfRangeException e)
            {
            }

            drcol.RemoveAt(1);
            Assert.False(drcol.Contains(dr2.RelationName));
            drcol.RemoveAt(0);
            Assert.False(drcol.Contains(dr1.RelationName));
        }
    }
}
