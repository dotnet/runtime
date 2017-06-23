// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Ameya Gargesh

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
using System.Data.Common;

namespace System.Data.Tests.Common
{
    public class DataTableMappingCollectionTest : IDisposable
    {
        private DataTableMappingCollection _tableMapCollection;
        private DataTableMapping[] _tabs;

        public DataTableMappingCollectionTest()
        {
            _tabs = new DataTableMapping[5];
            _tabs[0] = new DataTableMapping("sourceCustomers", "dataSetCustomers");
            _tabs[1] = new DataTableMapping("sourceEmployees", "dataSetEmployees");
            _tabs[2] = new DataTableMapping("sourceBooks", "dataSetBooks");
            _tabs[3] = new DataTableMapping("sourceStore", "dataSetStore");
            _tabs[4] = new DataTableMapping("sourceInventory", "dataSetInventory");
            _tableMapCollection = new DataTableMappingCollection();
        }

        public void Dispose()
        {
            _tableMapCollection.Clear();
        }

        [Fact]
        public void Add()
        {
            int t = _tableMapCollection.Add(_tabs[0]);
            Assert.Equal(0, t);
            bool eq1 = _tabs[0].Equals(_tableMapCollection[0]);
            Assert.Equal(true, eq1);
            Assert.Equal(1, _tableMapCollection.Count);
            DataTableMapping tab2;
            tab2 = _tableMapCollection.Add("sourceEmployees", "dataSetEmployees");
            bool eq2 = tab2.Equals(_tableMapCollection[1]);
            Assert.Equal(true, eq2);
            Assert.Equal(2, _tableMapCollection.Count);
        }

        [Fact]
        public void AddException1()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                DataTableMappingCollection c = new DataTableMappingCollection();
                _tableMapCollection.Add(c);
            });
        }

        [Fact]
        public void AddRange()
        {
            _tableMapCollection.Add(new DataTableMapping("sourceFactory", "dataSetFactory"));
            Assert.Equal(1, _tableMapCollection.Count);
            _tableMapCollection.AddRange(_tabs);
            Assert.Equal(6, _tableMapCollection.Count);
            bool eq;
            eq = _tabs[0].Equals(_tableMapCollection[1]);
            Assert.Equal(true, eq);
            eq = _tabs[1].Equals(_tableMapCollection[2]);
            Assert.Equal(true, eq);
            eq = _tabs[0].Equals(_tableMapCollection[0]);
            Assert.Equal(false, eq);
            eq = _tabs[1].Equals(_tableMapCollection[0]);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void Clear()
        {
            DataTableMapping tab1 = new DataTableMapping("sourceSuppliers", "dataSetSuppliers");
            _tableMapCollection.Add(tab1);
            Assert.Equal(1, _tableMapCollection.Count);
            _tableMapCollection.Clear();
            Assert.Equal(0, _tableMapCollection.Count);
            _tableMapCollection.AddRange(_tabs);
            Assert.Equal(5, _tableMapCollection.Count);
            _tableMapCollection.Clear();
            Assert.Equal(0, _tableMapCollection.Count);
        }

        [Fact]
        public void Contains()
        {
            DataTableMapping tab1 = new DataTableMapping("sourceCustomers", "dataSetCustomers");
            _tableMapCollection.AddRange(_tabs);
            bool eq;
            eq = _tableMapCollection.Contains(_tabs[0]);
            Assert.Equal(true, eq);
            eq = _tableMapCollection.Contains(_tabs[1]);
            Assert.Equal(true, eq);
            eq = _tableMapCollection.Contains(tab1);
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(_tabs[0].SourceTable);
            Assert.Equal(true, eq);
            eq = _tableMapCollection.Contains(_tabs[1].SourceTable);
            Assert.Equal(true, eq);
            eq = _tableMapCollection.Contains(tab1.SourceTable);
            Assert.Equal(true, eq);
            eq = _tableMapCollection.Contains(_tabs[0].DataSetTable);
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(_tabs[1].DataSetTable);
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(tab1.DataSetTable);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void CopyTo()
        {
            DataTableMapping[] tabcops = new DataTableMapping[5];
            _tableMapCollection.AddRange(_tabs);
            _tableMapCollection.CopyTo(tabcops, 0);
            bool eq;
            for (int i = 0; i < 5; i++)
            {
                eq = _tableMapCollection[i].Equals(tabcops[i]);
                Assert.Equal(true, eq);
            }
            tabcops = null;
            tabcops = new DataTableMapping[7];
            _tableMapCollection.CopyTo(tabcops, 2);
            for (int i = 0; i < 5; i++)
            {
                eq = _tableMapCollection[i].Equals(tabcops[i + 2]);
                Assert.Equal(true, eq);
            }
            eq = _tableMapCollection[0].Equals(tabcops[0]);
            Assert.Equal(false, eq);
            eq = _tableMapCollection[0].Equals(tabcops[1]);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void Equals()
        {
            //			DataTableMappingCollection collect2=new DataTableMappingCollection();
            _tableMapCollection.AddRange(_tabs);
            //			collect2.AddRange(tabs);
            DataTableMappingCollection copy1;
            copy1 = _tableMapCollection;

            //			Assert.Equal(false, tableMapCollection.Equals(collect2));
            Assert.Equal(true, _tableMapCollection.Equals(copy1));
            //			Assert.Equal(false, collect2.Equals(tableMapCollection));
            Assert.Equal(true, copy1.Equals(_tableMapCollection));
            //			Assert.Equal(false, collect2.Equals(copy1));
            Assert.Equal(true, copy1.Equals(_tableMapCollection));
            Assert.Equal(true, _tableMapCollection.Equals(_tableMapCollection));
            //			Assert.Equal(true, collect2.Equals(collect2));
            Assert.Equal(true, copy1.Equals(copy1));

            //			Assert.Equal(false, Object.Equals(collect2, tableMapCollection));
            Assert.Equal(true, object.Equals(copy1, _tableMapCollection));
            //			Assert.Equal(false, Object.Equals(tableMapCollection, collect2));
            Assert.Equal(true, object.Equals(_tableMapCollection, copy1));
            //			Assert.Equal(false, Object.Equals(copy1, collect2));
            Assert.Equal(true, object.Equals(_tableMapCollection, copy1));
            Assert.Equal(true, object.Equals(_tableMapCollection, _tableMapCollection));
            //			Assert.Equal(true, Object.Equals(collect2, collect2));
            Assert.Equal(true, object.Equals(copy1, copy1));
            //			Assert.Equal(false, Object.Equals(tableMapCollection, collect2));
            Assert.Equal(true, object.Equals(_tableMapCollection, copy1));
            //			Assert.Equal(false, Object.Equals(collect2, tableMapCollection));
            Assert.Equal(true, object.Equals(copy1, _tableMapCollection));
            //			Assert.Equal(false, Object.Equals(collect2, copy1));
            Assert.Equal(true, object.Equals(copy1, _tableMapCollection));
        }

        [Fact]
        public void GetByDataSetTable()
        {
            _tableMapCollection.AddRange(_tabs);
            bool eq;
            DataTableMapping tab1;
            tab1 = _tableMapCollection.GetByDataSetTable("dataSetCustomers");
            eq = (tab1.DataSetTable.Equals("dataSetCustomers") && tab1.SourceTable.Equals("sourceCustomers"));
            Assert.Equal(true, eq);
            tab1 = _tableMapCollection.GetByDataSetTable("dataSetEmployees");
            eq = (tab1.DataSetTable.Equals("dataSetEmployees") && tab1.SourceTable.Equals("sourceEmployees"));
            Assert.Equal(true, eq);

            tab1 = _tableMapCollection.GetByDataSetTable("datasetcustomers");
            eq = (tab1.DataSetTable.Equals("dataSetCustomers") && tab1.SourceTable.Equals("sourceCustomers"));
            Assert.Equal(true, eq);
            tab1 = _tableMapCollection.GetByDataSetTable("datasetemployees");
            eq = (tab1.DataSetTable.Equals("dataSetEmployees") && tab1.SourceTable.Equals("sourceEmployees"));
            Assert.Equal(true, eq);
        }

        [Fact]
        public void GetTableMappingBySchemaAction()
        {
            _tableMapCollection.AddRange(_tabs);
            bool eq;
            DataTableMapping tab1;
            tab1 = DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMapCollection, "sourceCustomers", "dataSetCustomers", MissingMappingAction.Passthrough);
            eq = (tab1.DataSetTable.Equals("dataSetCustomers") && tab1.SourceTable.Equals("sourceCustomers"));
            Assert.Equal(true, eq);
            tab1 = DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMapCollection, "sourceEmployees", "dataSetEmployees", MissingMappingAction.Passthrough);
            eq = (tab1.DataSetTable.Equals("dataSetEmployees") && tab1.SourceTable.Equals("sourceEmployees"));
            Assert.Equal(true, eq);

            tab1 = DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMapCollection, "sourceData", "dataSetData", MissingMappingAction.Passthrough);
            eq = (tab1.DataSetTable.Equals("sourceData") && tab1.SourceTable.Equals("dataSetData"));
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(tab1);
            Assert.Equal(false, eq);
            tab1 = DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMapCollection, "sourceData", "dataSetData", MissingMappingAction.Ignore);
            Assert.Equal(null, tab1);
        }

        [Fact]
        public void GetTableMappingBySchemaActionException1()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMapCollection, "sourceCustomers", "dataSetCustomers", MissingMappingAction.Error);
            });
        }

        [Fact]
        public void IndexOf()
        {
            _tableMapCollection.AddRange(_tabs);
            int ind;
            ind = _tableMapCollection.IndexOf(_tabs[0]);
            Assert.Equal(0, ind);
            ind = _tableMapCollection.IndexOf(_tabs[1]);
            Assert.Equal(1, ind);

            ind = _tableMapCollection.IndexOf(_tabs[0].SourceTable);
            Assert.Equal(0, ind);
            ind = _tableMapCollection.IndexOf(_tabs[1].SourceTable);
            Assert.Equal(1, ind);
        }

        [Fact]
        public void IndexOfDataSetTable()
        {
            _tableMapCollection.AddRange(_tabs);
            int ind;
            ind = _tableMapCollection.IndexOfDataSetTable(_tabs[0].DataSetTable);
            Assert.Equal(0, ind);
            ind = _tableMapCollection.IndexOfDataSetTable(_tabs[1].DataSetTable);
            Assert.Equal(1, ind);

            ind = _tableMapCollection.IndexOfDataSetTable("datasetcustomers");
            Assert.Equal(0, ind);
            ind = _tableMapCollection.IndexOfDataSetTable("datasetemployees");
            Assert.Equal(1, ind);

            ind = _tableMapCollection.IndexOfDataSetTable("sourcedeter");
            Assert.Equal(-1, ind);
        }

        [Fact]
        public void Insert()
        {
            _tableMapCollection.AddRange(_tabs);
            DataTableMapping mymap = new DataTableMapping("sourceTestAge", "datatestSetAge");
            _tableMapCollection.Insert(3, mymap);
            int ind = _tableMapCollection.IndexOfDataSetTable("datatestSetAge");
            Assert.Equal(3, ind);
        }

        [Fact]
        public void RemoveException1()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                string te = "testingdata";
                _tableMapCollection.AddRange(_tabs);
                _tableMapCollection.Remove(te);
            });
        }

        [Fact]
        public void RemoveException2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                _tableMapCollection.AddRange(_tabs);
                DataTableMapping mymap = new DataTableMapping("sourceAge", "dataSetAge");
                _tableMapCollection.Remove(mymap);
            });
        }

        [Fact]
        public void RemoveAt()
        {
            _tableMapCollection.AddRange(_tabs);
            bool eq;
            _tableMapCollection.RemoveAt(0);
            eq = _tableMapCollection.Contains(_tabs[0]);
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(_tabs[1]);
            Assert.Equal(true, eq);

            _tableMapCollection.RemoveAt("sourceEmployees");
            eq = _tableMapCollection.Contains(_tabs[1]);
            Assert.Equal(false, eq);
            eq = _tableMapCollection.Contains(_tabs[2]);
            Assert.Equal(true, eq);
        }

        [Fact]
        public void RemoveAtException1()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                _tableMapCollection.RemoveAt(3);
            });
        }

        [Fact]
        public void RemoveAtException2()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _tableMapCollection.RemoveAt("sourceAge"));
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("System.Data.Common.DataTableMappingCollection", _tableMapCollection.ToString());
        }
    }
}
