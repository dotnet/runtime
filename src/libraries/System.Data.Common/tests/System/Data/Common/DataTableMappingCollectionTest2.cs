// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Data.Common;
using System.Collections;

namespace System.Data.Tests.Common
{
    public class DataTableMappingCollectionTest2
    {
        [Fact]
        public void ICollectionProperties()
        {
            ICollection collection = new DataTableMappingCollection();
            Assert.False(collection.IsSynchronized);
            Assert.Same(collection, collection.SyncRoot);
        }

        [Fact]
        public void IListProperties()
        {
            IList list = new DataTableMappingCollection();
            Assert.False(list.IsFixedSize);
            Assert.False(list.IsReadOnly);
        }

        [Fact]
        public void IListIndexer()
        {
            IList list = new DataTableMappingCollection();
            var mapping = new DataTableMapping("source", "dataSet");
            Assert.Throws<IndexOutOfRangeException>(() => { var x = list[0]; });
            Assert.Throws<IndexOutOfRangeException>(() => { list[0] = mapping; });
            list.Add(mapping);
            Assert.Same(mapping, list[0]);
            Assert.Throws<ArgumentNullException>(() => { list[0] = null; });
            Assert.Throws<InvalidCastException>(() => { list[0] = "invalid"; });
            list[0] = new DataTableMapping("source2", "dataSet2");
            Assert.NotSame(mapping, list[0]);
        }

        [Fact]
        public void ITableMappingCollectionIndexer()
        {
            ITableMappingCollection collection = new DataTableMappingCollection();
            Assert.Throws<IndexOutOfRangeException>(() => { var x = collection["source"]; });
            Assert.Throws<IndexOutOfRangeException>(() => { collection["source"] = new DataTableMapping(); });
            ITableMapping mapping = collection.Add("source", "dataSet");
            Assert.Same(mapping, collection["source"]);
            Assert.Same(mapping, collection.GetByDataSetTable("dataSet"));
            Assert.Throws<ArgumentNullException>(() => { collection["source"] = null; });
            Assert.Throws<InvalidCastException>(() => { collection["source"] = "invalid"; });
            ITableMapping mapping2 = new DataTableMapping("source2", "dataSet2");
            collection["source"] = mapping2;
            Assert.Single(collection);
            Assert.Same(mapping2, collection["source2"]);
            Assert.Throws<IndexOutOfRangeException>(() => collection.GetByDataSetTable("dataSet"));
        }

        [Fact]
        public void AddRangeArray()
        {
            Array array = new DataTableMapping[] { new DataTableMapping("source", "dataSet") };
            var collection = new DataTableMappingCollection();
            collection.AddRange(array);
            Assert.Single(collection);
            Assert.Same(array.GetValue(0), collection[0]);
        }

        [Fact]
        public void AddRangeNull()
        {
            var collection = new DataTableMappingCollection();
            Assert.Throws<ArgumentNullException>(() => collection.AddRange(default(Array)));
            Assert.Throws<ArgumentNullException>(() => collection.AddRange(default(DataTableMapping[])));
        }

        [Fact]
        public void CopyToArray()
        {
            var mapping = new DataTableMapping("source", "dataSet");
            var collection = new DataTableMappingCollection
            {
                mapping,
            };
            Array array = new DataTableMapping[1];
            collection.CopyTo(array, 0);
            Assert.Same(mapping, array.GetValue(0));
        }

        [Fact]
        public void Insert()
        {
            var collection = new DataTableMappingCollection();
            Assert.Throws<ArgumentNullException>(() => collection.Insert(0, default(object)));
            Assert.Throws<ArgumentNullException>(() => collection.Insert(0, default(DataTableMapping)));
            Assert.Throws<InvalidCastException>(() => collection.Insert(0, "invalid"));
            object mapping = new DataTableMapping("source", "dataSet");
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, mapping));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(1, mapping));
            collection.Insert(0, mapping);
            Assert.Single(collection);
            Assert.Same(mapping, collection[0]);
            Assert.Throws<ArgumentException>(() => collection.Insert(1, mapping));
        }

        [Fact]
        public void Remove()
        {
            object mapping = new DataTableMapping("source", "dataSet");
            var collection = new DataTableMappingCollection
            {
                mapping,
            };
            Assert.Throws<ArgumentNullException>(() => collection.Remove(default(object)));
            Assert.Throws<ArgumentNullException>(() => collection.Remove(default(DataTableMapping)));
            Assert.Throws<ArgumentException>(() => collection.Remove(new DataTableMapping("a", "b")));
            collection.Remove(mapping);
            Assert.Empty(collection);
        }

        [Fact]
        public void Replace()
        {
            var mapping1 = new DataTableMapping("source1", "dataSet1");
            var collection1 = new DataTableMappingCollection
            {
                mapping1,
            };
            Assert.Throws<ArgumentNullException>(() => collection1[0] = null);
            Assert.Throws<ArgumentNullException>(() => collection1["source1"] = null);

            var mapping2 = new DataTableMapping("source2", "dataSet2");
            var collection2 = new DataTableMappingCollection
            {
                mapping2,
            };
            Assert.Throws<ArgumentException>(() => collection2[0] = mapping1);
        }

        [Fact]
        public void AutoGeneratedNames()
        {
            var collection = new DataTableMappingCollection
            {
                new DataTableMapping(),
                new DataTableMapping(),
            };
            Assert.Equal("SourceTable1", collection[0].SourceTable);
            Assert.Equal("SourceTable2", collection[1].SourceTable);
        }

        [Fact]
        public void GetTableMappingBySchemaAction()
        {
            var collection = new DataTableMappingCollection();
            Assert.Throws<ArgumentException>(() => DataTableMappingCollection.GetTableMappingBySchemaAction(collection, "", "", MissingMappingAction.Ignore));
            Assert.Throws<InvalidOperationException>(() => DataTableMappingCollection.GetTableMappingBySchemaAction(collection, "source", "", MissingMappingAction.Error));
            Assert.Null(DataTableMappingCollection.GetTableMappingBySchemaAction(collection, "source", "", MissingMappingAction.Ignore));
            Assert.Throws<ArgumentOutOfRangeException>(() => DataTableMappingCollection.GetTableMappingBySchemaAction(collection, "source", "", default(MissingMappingAction)));
            DataTableMapping mapping = DataTableMappingCollection.GetTableMappingBySchemaAction(collection, "source", "", MissingMappingAction.Passthrough);
            Assert.NotNull(mapping);
            Assert.Equal("source", mapping.SourceTable);
        }
    }
}
