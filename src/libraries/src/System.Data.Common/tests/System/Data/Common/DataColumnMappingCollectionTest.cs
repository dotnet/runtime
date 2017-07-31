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
using System.Collections;
using System.Data.Common;

namespace System.Data.Tests.Common
{
    public class DataColumnMappingCollectionTest : IDisposable
    {
        //DataTableMapping tableMap;
        private DataColumnMappingCollection _columnMapCollection;
        private DataColumnMapping[] _cols;

        public DataColumnMappingCollectionTest()
        {
            _cols = new DataColumnMapping[5];
            _cols[0] = new DataColumnMapping("sourceName", "dataSetName");
            _cols[1] = new DataColumnMapping("sourceID", "dataSetID");
            _cols[2] = new DataColumnMapping("sourceAddress", "dataSetAddress");
            _cols[3] = new DataColumnMapping("sourcePhone", "dataSetPhone");
            _cols[4] = new DataColumnMapping("sourcePIN", "dataSetPIN");
            _columnMapCollection = new DataColumnMappingCollection();
        }

        public void Dispose()
        {
            _columnMapCollection.Clear();
        }

        [Fact]
        public void Add()
        {
            DataColumnMapping col1 = new DataColumnMapping("sourceName", "dataSetName");
            int t = _columnMapCollection.Add(col1);
            Assert.Equal(0, t);
            bool eq1 = col1.Equals(_columnMapCollection[0]);
            Assert.Equal(true, eq1);
            Assert.Equal(1, _columnMapCollection.Count);
            DataColumnMapping col2;
            col2 = _columnMapCollection.Add("sourceID", "dataSetID");
            bool eq2 = col2.Equals(_columnMapCollection[1]);
            Assert.Equal(true, eq2);
            Assert.Equal(2, _columnMapCollection.Count);
        }

        [Fact]
        public void AddException1()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                DataColumnMappingCollection c = new DataColumnMappingCollection();
                _columnMapCollection.Add(c);
            });
        }

        [Fact]
        public void AddRange()
        {
            _columnMapCollection.Add(new DataColumnMapping("sourceAge", "dataSetAge"));
            Assert.Equal(1, _columnMapCollection.Count);
            _columnMapCollection.AddRange(_cols);
            Assert.Equal(6, _columnMapCollection.Count);
            bool eq;
            eq = _cols[0].Equals(_columnMapCollection[1]);
            Assert.Equal(true, eq);
            eq = _cols[1].Equals(_columnMapCollection[2]);
            Assert.Equal(true, eq);

            eq = _cols[0].Equals(_columnMapCollection[0]);
            Assert.Equal(false, eq);
            eq = _cols[1].Equals(_columnMapCollection[0]);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void Clear()
        {
            DataColumnMapping col1 = new DataColumnMapping("sourceName", "dataSetName");
            _columnMapCollection.Add(col1);
            Assert.Equal(1, _columnMapCollection.Count);
            _columnMapCollection.Clear();
            Assert.Equal(0, _columnMapCollection.Count);
            _columnMapCollection.AddRange(_cols);
            Assert.Equal(5, _columnMapCollection.Count);
            _columnMapCollection.Clear();
            Assert.Equal(0, _columnMapCollection.Count);
        }

        [Fact]
        public void Contains()
        {
            DataColumnMapping col1 = new DataColumnMapping("sourceName", "dataSetName");
            _columnMapCollection.AddRange(_cols);
            bool eq;
            eq = _columnMapCollection.Contains(_cols[0]);
            Assert.Equal(true, eq);
            eq = _columnMapCollection.Contains(_cols[1]);
            Assert.Equal(true, eq);

            eq = _columnMapCollection.Contains(col1);
            Assert.Equal(false, eq);

            eq = _columnMapCollection.Contains(_cols[0].SourceColumn);
            Assert.Equal(true, eq);
            eq = _columnMapCollection.Contains(_cols[1].SourceColumn);
            Assert.Equal(true, eq);

            eq = _columnMapCollection.Contains(col1.SourceColumn);
            Assert.Equal(true, eq);

            eq = _columnMapCollection.Contains(_cols[0].DataSetColumn);
            Assert.Equal(false, eq);
            eq = _columnMapCollection.Contains(_cols[1].DataSetColumn);
            Assert.Equal(false, eq);

            eq = _columnMapCollection.Contains(col1.DataSetColumn);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void ContainsException1()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                object o = new object();
                bool a = _columnMapCollection.Contains(o);
            });
        }

        [Fact]
        public void CopyTo()
        {
            DataColumnMapping[] colcops = new DataColumnMapping[5];
            _columnMapCollection.AddRange(_cols);
            _columnMapCollection.CopyTo(colcops, 0);
            bool eq;
            for (int i = 0; i < 5; i++)
            {
                eq = _columnMapCollection[i].Equals(colcops[i]);
                Assert.Equal(true, eq);
            }
            colcops = null;
            colcops = new DataColumnMapping[7];
            _columnMapCollection.CopyTo(colcops, 2);
            for (int i = 0; i < 5; i++)
            {
                eq = _columnMapCollection[i].Equals(colcops[i + 2]);
                Assert.Equal(true, eq);
            }
            eq = _columnMapCollection[0].Equals(colcops[0]);
            Assert.Equal(false, eq);
            eq = _columnMapCollection[0].Equals(colcops[1]);
            Assert.Equal(false, eq);
        }

        [Fact]
        public void Equals()
        {
            //			DataColumnMappingCollection collect2=new DataColumnMappingCollection();
            _columnMapCollection.AddRange(_cols);
            //			collect2.AddRange(cols);
            DataColumnMappingCollection copy1;
            copy1 = _columnMapCollection;

            //			Assert.Equal (false, columnMapCollection.Equals(collect2));
            Assert.Equal(true, _columnMapCollection.Equals(copy1));
            //			Assert.Equal (false, collect2.Equals(columnMapCollection));
            Assert.Equal(true, copy1.Equals(_columnMapCollection));
            //			Assert.Equal (false, collect2.Equals(copy1));
            Assert.Equal(true, copy1.Equals(_columnMapCollection));
            Assert.Equal(true, _columnMapCollection.Equals(_columnMapCollection));
            //			Assert.Equal (true, collect2.Equals(collect2));
            Assert.Equal(true, copy1.Equals(copy1));

            //			Assert.Equal (false, Object.Equals(collect2, columnMapCollection));
            Assert.Equal(true, object.Equals(copy1, _columnMapCollection));
            //			Assert.Equal (false, Object.Equals(columnMapCollection, collect2));
            Assert.Equal(true, object.Equals(_columnMapCollection, copy1));
            //			Assert.Equal (false, Object.Equals(copy1, collect2));
            Assert.Equal(true, object.Equals(_columnMapCollection, copy1));
            Assert.Equal(true, object.Equals(_columnMapCollection, _columnMapCollection));
            //			Assert.Equal (true, Object.Equals(collect2, collect2));
            Assert.Equal(true, object.Equals(copy1, copy1));
            //			Assert.Equal (false, Object.Equals(columnMapCollection, collect2));
            Assert.Equal(true, object.Equals(_columnMapCollection, copy1));
            //			Assert.Equal (false, Object.Equals(collect2, columnMapCollection));
            Assert.Equal(true, object.Equals(copy1, _columnMapCollection));
            //			Assert.Equal (false, Object.Equals(collect2, copy1));
            Assert.Equal(true, object.Equals(copy1, _columnMapCollection));
        }

        [Fact]
        public void GetByDataSetColumn()
        {
            _columnMapCollection.AddRange(_cols);
            bool eq;
            DataColumnMapping col1;
            col1 = _columnMapCollection.GetByDataSetColumn("dataSetName");
            eq = (col1.DataSetColumn.Equals("dataSetName") && col1.SourceColumn.Equals("sourceName"));
            Assert.Equal(true, eq);
            col1 = _columnMapCollection.GetByDataSetColumn("dataSetID");
            eq = (col1.DataSetColumn.Equals("dataSetID") && col1.SourceColumn.Equals("sourceID"));
            Assert.Equal(true, eq);

            col1 = _columnMapCollection.GetByDataSetColumn("datasetname");
            eq = (col1.DataSetColumn.Equals("dataSetName") && col1.SourceColumn.Equals("sourceName"));
            Assert.Equal(true, eq);
            col1 = _columnMapCollection.GetByDataSetColumn("datasetid");
            eq = (col1.DataSetColumn.Equals("dataSetID") && col1.SourceColumn.Equals("sourceID"));
            Assert.Equal(true, eq);
        }

        [Fact]
        public void GetByDataSetColumn_String_InvalidArguments()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            Assert.Throws<IndexOutOfRangeException>(() => dataColumnMappingCollection.GetByDataSetColumn((string)null));
        }

        [Fact]
        public void GetColumnMappingBySchemaAction()
        {
            _columnMapCollection.AddRange(_cols);
            bool eq;
            DataColumnMapping col1;
            col1 = DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMapCollection, "sourceName", MissingMappingAction.Passthrough);
            eq = (col1.DataSetColumn.Equals("dataSetName") && col1.SourceColumn.Equals("sourceName"));
            Assert.Equal(true, eq);
            col1 = DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMapCollection, "sourceID", MissingMappingAction.Passthrough);
            eq = (col1.DataSetColumn.Equals("dataSetID") && col1.SourceColumn.Equals("sourceID"));
            Assert.Equal(true, eq);

            col1 = DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMapCollection, "sourceData", MissingMappingAction.Passthrough);
            eq = (col1.DataSetColumn.Equals("sourceData") && col1.SourceColumn.Equals("sourceData"));
            Assert.Equal(true, eq);
            eq = _columnMapCollection.Contains(col1);
            Assert.Equal(false, eq);
            col1 = DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMapCollection, "sourceData", MissingMappingAction.Ignore);
            Assert.Equal(null, col1);
        }

        [Fact]
        public void GetColumnMappingBySchemaActionException1()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMapCollection, "sourceName", MissingMappingAction.Error);
            });
        }

        [Fact]
        public void IndexOf()
        {
            _columnMapCollection.AddRange(_cols);
            int ind;
            ind = _columnMapCollection.IndexOf(_cols[0]);
            Assert.Equal(0, ind);
            ind = _columnMapCollection.IndexOf(_cols[1]);
            Assert.Equal(1, ind);

            ind = _columnMapCollection.IndexOf(_cols[0].SourceColumn);
            Assert.Equal(0, ind);
            ind = _columnMapCollection.IndexOf(_cols[1].SourceColumn);
            Assert.Equal(1, ind);
        }

        [Fact]
        public void IndexOf_Object_IsNull()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            Assert.Equal(-1, dataColumnMappingCollection.IndexOf((object)null));
        }

        [Fact]
        public void IndexOf_String_IsNull()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            Assert.Equal(-1, dataColumnMappingCollection.IndexOf((string)null));
        }

        [Fact]
        public void IndexOfDataSetColumn()
        {
            _columnMapCollection.AddRange(_cols);
            int ind;
            ind = _columnMapCollection.IndexOfDataSetColumn(_cols[0].DataSetColumn);
            Assert.Equal(0, ind);
            ind = _columnMapCollection.IndexOfDataSetColumn(_cols[1].DataSetColumn);
            Assert.Equal(1, ind);

            ind = _columnMapCollection.IndexOfDataSetColumn("datasetname");
            Assert.Equal(0, ind);
            ind = _columnMapCollection.IndexOfDataSetColumn("datasetid");
            Assert.Equal(1, ind);

            ind = _columnMapCollection.IndexOfDataSetColumn("sourcedeter");
            Assert.Equal(-1, ind);
        }

        [Fact]
        public void Insert()
        {
            _columnMapCollection.AddRange(_cols);
            DataColumnMapping mymap = new DataColumnMapping("sourceAge", "dataSetAge");
            _columnMapCollection.Insert(3, mymap);
            int ind = _columnMapCollection.IndexOfDataSetColumn("dataSetAge");
            Assert.Equal(3, ind);
        }

        [Fact]
        public void Remove_DataColumnMapping_InvalidArguments()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            Assert.Throws<ArgumentNullException>(() => dataColumnMappingCollection.Remove((DataColumnMapping)null));
        }

        [Fact]
        public void Remove_DataColumnMapping_Success()
        {
            DataColumnMapping dataColumnMapping = new DataColumnMapping("source", "dataSet");
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection
            {
                dataColumnMapping
            };
            Assert.Equal(1, dataColumnMappingCollection.Count);

            dataColumnMappingCollection.Remove(dataColumnMapping);

            Assert.Equal(0, dataColumnMappingCollection.Count);
        }

        [Fact]
        public void RemoveException1()
        {
            Assert.Throws<InvalidCastException>(() =>
            {
                string te = "testingdata";
                _columnMapCollection.AddRange(_cols);
                _columnMapCollection.Remove(te);
            });
        }

        [Fact]
        public void RemoveException2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                _columnMapCollection.AddRange(_cols);
                DataColumnMapping mymap = new DataColumnMapping("sourceAge", "dataSetAge");
                _columnMapCollection.Remove(mymap);
            });
        }

        [Fact]
        public void RemoveAt()
        {
            _columnMapCollection.AddRange(_cols);
            bool eq;
            _columnMapCollection.RemoveAt(0);
            eq = _columnMapCollection.Contains(_cols[0]);
            Assert.Equal(false, eq);
            eq = _columnMapCollection.Contains(_cols[1]);
            Assert.Equal(true, eq);

            _columnMapCollection.RemoveAt("sourceID");
            eq = _columnMapCollection.Contains(_cols[1]);
            Assert.Equal(false, eq);
            eq = _columnMapCollection.Contains(_cols[2]);
            Assert.Equal(true, eq);
        }

        [Fact]
        public void RemoveAtException1()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                _columnMapCollection.RemoveAt(3);
            });
        }

        [Fact]
        public void RemoveAtException2()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                _columnMapCollection.RemoveAt("sourceAge");
            });
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("System.Data.Common.DataColumnMappingCollection", _columnMapCollection.ToString());
        }

        [Fact]
        public void Insert_Int_DataColumnMapping_InvalidArguments()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            Assert.Throws<ArgumentNullException>(() => dataColumnMappingCollection.Insert(123, (DataColumnMapping)null));
        }

        [Fact]
        public void GetDataColumn_DataColumnMappingCollection_String_Type_DataTable_MissingMappingAction_MissingSchemaAction_InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentException>("sourceColumn", () => DataColumnMappingCollection.GetDataColumn((DataColumnMappingCollection)null, null, typeof(string), new DataTable(), new MissingMappingAction(), new MissingSchemaAction()));
        }

        [Fact]
        public void GetDataColumn_DataColumnMappingCollection_String_Type_DataTable_MissingMappingAction_MissingSchemaAction_MissingMappingActionIgnoreReturnsNull()
        {
            Assert.Null(DataColumnMappingCollection.GetDataColumn((DataColumnMappingCollection)null, "not null", typeof(string), new DataTable(), MissingMappingAction.Ignore, new MissingSchemaAction()));
        }

        [Fact]
        public void GetDataColumn_DataColumnMappingCollection_String_Type_DataTable_MissingMappingAction_MissingSchemaAction_MissingMappingActionErrorThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => DataColumnMappingCollection.GetDataColumn((DataColumnMappingCollection)null, "not null", typeof(string), new DataTable(), MissingMappingAction.Error, new MissingSchemaAction()));
        }

        [Fact]
        public void GetDataColumn_DataColumnMappingCollection_String_Type_DataTable_MissingMappingAction_MissingSchemaAction_MissingMappingActionNotFoundThrowsException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("MissingMappingAction", () => DataColumnMappingCollection.GetDataColumn((DataColumnMappingCollection)null, "not null", typeof(string), new DataTable(), new MissingMappingAction(), new MissingSchemaAction()));
        }

        [Fact]
        public void GetColumnMappingBySchemaAction_DataColumnMappingCollection_String_MissingMappingAction_InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentException>("sourceColumn", () => DataColumnMappingCollection.GetColumnMappingBySchemaAction((DataColumnMappingCollection)null, null, new MissingMappingAction()));
        }

        [Fact]
        public void GetColumnMappingBySchemaAction_DataColumnMappingCollection_String_MissingMappingAction_MissingMappingActionNotFoundThrowsException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("MissingMappingAction", () => DataColumnMappingCollection.GetColumnMappingBySchemaAction((DataColumnMappingCollection)null, "not null", new MissingMappingAction()));
        }

        [Fact]
        public void AddRange_Array_PassingNullThrowsException()
        {
            Array array = null;
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            AssertExtensions.Throws<ArgumentNullException>("values", () => dataColumnMappingCollection.AddRange(array));
        }

        [Fact]
        public void Indexer_String_SetAndGetOK()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(new DataColumnMapping("sourcePIN", "dataSetPIN"));

            dataColumnMappingCollection["sourcePIN"] = new DataColumnMapping("sourcePIN", "dataSetPINSet");
            DataColumnMapping dataColumnMapping = dataColumnMappingCollection["sourcePIN"];
            Assert.Equal("dataSetPINSet", dataColumnMapping.DataSetColumn);
        }

        [Fact]
        public void CopyTo_Array_Int_Success()
        {
            Array array = new DataColumnMapping[1];
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(new DataColumnMapping("sourcePIN", "dataSetPIN"));
            IEnumerator enumerator = array.GetEnumerator();
            enumerator.MoveNext();
            Assert.Null(enumerator.Current);

            dataColumnMappingCollection.CopyTo(array, 0);

            enumerator = array.GetEnumerator();
            enumerator.MoveNext();
            Assert.Equal("dataSetPIN", ((DataColumnMapping)enumerator.Current).DataSetColumn);
        }

        [Fact]
        public void GetEnumerator_Success()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(new DataColumnMapping("a", "b"));

            IEnumerator enumerator = dataColumnMappingCollection.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal("b", ((DataColumnMapping)enumerator.Current).DataSetColumn);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Insert_Int_Object_Success()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            Assert.Equal(0, dataColumnMappingCollection.Count);

            dataColumnMappingCollection.Insert(0, (object)new DataColumnMapping("sourcePIN", "dataSetPIN"));

            Assert.Equal(1, dataColumnMappingCollection.Count);
            Assert.Equal("dataSetPIN", dataColumnMappingCollection["sourcePIN"].DataSetColumn);
        }

        [Fact]
        public void Remove_Object_Success()
        {
            DataColumnMapping dataColumnMapping = new DataColumnMapping("sourcePIN", "dataSetPIN");
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(dataColumnMapping);
            Assert.Equal(1, dataColumnMappingCollection.Count);

            dataColumnMappingCollection.Remove((object)dataColumnMapping);

            Assert.Equal(0, dataColumnMappingCollection.Count);
        }

        [Fact]
        public void Remove_Object_PassingNullThrowsException()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();

            AssertExtensions.Throws<ArgumentNullException>("value", () => dataColumnMappingCollection.Remove((object)null));
        }

        [Fact]
        public void Indexer_Int_SetAndGetOK()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(new DataColumnMapping("sourcePIN", "dataSetPIN"));

            dataColumnMappingCollection[0] = new DataColumnMapping("sourcePIN", "dataSetPINSet");
            DataColumnMapping dataColumnMapping = dataColumnMappingCollection[0];
            Assert.Equal("dataSetPINSet", dataColumnMapping.DataSetColumn);
        }

        [Fact]
        public void GetDataColumn_DataColumnMappingCollection_String_Type_DataTable_MissingMappingAction_MissingSchemaAction()
        {
            DataColumnMappingCollection dataColumnMappingCollection = new DataColumnMappingCollection();
            dataColumnMappingCollection.Add(new DataColumnMapping("sourcePIN", "dataSetPIN"));

            DataColumn dataColumn = DataColumnMappingCollection.GetDataColumn(dataColumnMappingCollection, "sourcePIN", null, new DataTable(), MissingMappingAction.Ignore, MissingSchemaAction.Ignore);

            Assert.Null(dataColumn);
        }
    }
}
