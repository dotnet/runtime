// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlBulkCopyColumnMappingCollectionTest
    {
        private static SqlBulkCopyColumnMappingCollection CreateCollection() => new SqlBulkCopy(new SqlConnection()).ColumnMappings;

        private static SqlBulkCopyColumnMappingCollection CreateCollection(params SqlBulkCopyColumnMapping[] mappings)
        {
            Debug.Assert(mappings != null);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection();

            foreach (SqlBulkCopyColumnMapping mapping in mappings)
            {
                Debug.Assert(mapping != null);
                collection.Add(mapping);
            }

            return collection;
        }

        [Fact]
        public void Properties_ReturnFalse()
        {
            IList list = CreateCollection();
            Assert.False(list.IsSynchronized);
            Assert.False(list.IsFixedSize);
            Assert.False(list.IsReadOnly);
        }

        [Fact]
        public void Methods_NullParameterPassed_ThrowsArgumentNullException()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();
            collection.Add(new SqlBulkCopyColumnMapping());

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));

            // Passing null to the public Add method should really throw ArgumentNullException
            // (which would be consistent with the explicit implementation of IList.Add), but
            // the full framework does not check for null in the public Add method. Instead it
            // accesses the parameter without first checking for null, resulting in
            // NullReferenceExpcetion being thrown.
            Assert.Throws<NullReferenceException>(() => collection.Add(null));

            // Passing null to the public Insert and Remove methods should really throw
            // ArgumentNullException (which would be consistent with the explicit
            // implementations of IList.Insert and IList.Remove), but the full framework
            // does not check for null in these methods.
            collection.Insert(0, null);
            collection.Remove(null);


            IList list = collection;
            Assert.Throws<ArgumentNullException>(() => list[0] = null);
            Assert.Throws<ArgumentNullException>(() => list.Add(null));
            Assert.Throws<ArgumentNullException>(() => list.CopyTo(null, 0));
            Assert.Throws<ArgumentNullException>(() => list.Insert(0, null));
            Assert.Throws<ArgumentNullException>(() => list.Remove(null));
        }

        [Fact]
        public void Members_InvalidRange_ThrowsArgumentOutOfRangeException()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();

            var item = new SqlBulkCopyColumnMapping(0, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[collection.Count]);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, item));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(collection.Count + 1, item));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(collection.Count));

            IList list = collection;
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[collection.Count]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = item);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[collection.Count] = item);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, item));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(collection.Count + 1, item));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(collection.Count));
        }

        [Fact]
        public void Add_AddItems_ItemsAddedAsEpected()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            Assert.Same(item1, collection.Add(item1));
            Assert.Same(item1, collection[0]);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            Assert.Same(item2, collection.Add(item2));
            Assert.Same(item2, collection[1]);

            IList list = CreateCollection();
            int index = list.Add(item1);
            Assert.Equal(0, index);
            Assert.Same(item1, list[0]);
            index = list.Add(item2);
            Assert.Equal(1, index);
            Assert.Same(item2, list[1]);
        }

        [Fact]
        public void Add_HelperOverloads_ItemsAddedAsExpected()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();
            SqlBulkCopyColumnMapping item;

            item = collection.Add(3, 4);
            Assert.NotNull(item);
            Assert.Equal(3, item.SourceOrdinal);
            Assert.Equal(4, item.DestinationOrdinal);

            item = collection.Add(5, "destination");
            Assert.NotNull(item);
            Assert.Equal(5, item.SourceOrdinal);
            Assert.Equal("destination", item.DestinationColumn);

            item = collection.Add("source", 6);
            Assert.NotNull(item);
            Assert.Equal("source", item.SourceColumn);
            Assert.Equal(6, item.DestinationOrdinal);

            item = collection.Add("src", "dest");
            Assert.NotNull(item);
            Assert.Equal("src", item.SourceColumn);
            Assert.Equal("dest", item.DestinationColumn);
        }

        [Fact]
        public void Add_InvalidItems_ThrowsInvalidOperationException()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();
            Assert.Throws<InvalidOperationException>(() => collection.Add(new SqlBulkCopyColumnMapping { SourceColumn = null }));
            Assert.Throws<InvalidOperationException>(() => collection.Add(new SqlBulkCopyColumnMapping { DestinationColumn = null }));

            // The explicitly implemented IList.Add should really throw InvalidOperationException to match the public
            // Add method behavior, but does not throw in the full framework.
            IList list = CreateCollection();
            Assert.Equal(0, list.Add(new SqlBulkCopyColumnMapping { SourceColumn = null }));
            Assert.Equal(1, list.Add(new SqlBulkCopyColumnMapping { DestinationColumn = null }));
        }

        [Fact]
        public void IListAddInsert_InsertNonSqlBulkCopyColumnMappingItems_DoNotThrow()
        {
            IList list = CreateCollection();
            list.Add(new SqlBulkCopyColumnMapping());

            // The following operations should really throw ArgumentException due to the
            // mismatched types, but do not throw in the full framework.
            string bogus = "Bogus";
            list[0] = bogus;
            list.Add(bogus);
            list.Insert(0, bogus);
        }

        [Fact]
        public void GetEnumerator_NoItems_EmptyEnumerator()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();
            IEnumerator e = collection.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => e.Current);
            Assert.False(e.MoveNext());
            Assert.Throws<InvalidOperationException>(() => e.Current);
        }

        [Fact]
        public void GetEnumerator_ItemsAdded_AllItemsReturnedAndEnumeratorBehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            IEnumerator e = collection.GetEnumerator();

            const int Iterations = 2;
            for (int i = 0; i < Iterations; i++)
            {
                // Not started
                Assert.Throws<InvalidOperationException>(() => e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item1, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item2, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item3, e.Current);

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                // Ended
                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void GetEnumerator_ItemsAdded_ItemsFromEnumeratorMatchesItemsFromIndexer()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            int index = 0;
            foreach (SqlBulkCopyColumnMapping enumeratorItem in collection)
            {
                SqlBulkCopyColumnMapping indexerItem = collection[index];

                Assert.NotNull(enumeratorItem);
                Assert.NotNull(indexerItem);

                Assert.Same(indexerItem, enumeratorItem);
                index++;
            }
        }

        [Fact]
        public void GetEnumerator_ModifiedCollectionDuringEnumeration_ThrowsInvalidOperationException()
        {
            SqlBulkCopyColumnMappingCollection collection = CreateCollection();

            IEnumerator e = collection.GetEnumerator();

            collection.Add(0, 0);

            // Collection changed.
            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            Assert.Throws<InvalidOperationException>(() => e.Reset());
        }


        [Fact]
        public void Contains_ItemsAdded_MatchesExpectation()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            Assert.True(collection.Contains(item1));
            Assert.True(collection.Contains(item2));
            Assert.True(collection.Contains(item3));
            Assert.False(collection.Contains(null));

            IList list = collection;
            Assert.True(list.Contains(item1));
            Assert.True(list.Contains(item2));
            Assert.True(list.Contains(item3));
            Assert.False(list.Contains(null));
            Assert.False(list.Contains("Bogus"));
        }

        [Fact]
        public void CopyTo_ItemsAdded_ItemsCopiedToArray()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            var array1 = new SqlBulkCopyColumnMapping[collection.Count];
            collection.CopyTo(array1, 0);

            Assert.Same(item1, array1[0]);
            Assert.Same(item2, array1[1]);
            Assert.Same(item3, array1[2]);

            var array2 = new SqlBulkCopyColumnMapping[collection.Count];
            ((ICollection)collection).CopyTo(array2, 0);

            Assert.Same(item1, array2[0]);
            Assert.Same(item2, array2[1]);
            Assert.Same(item3, array2[2]);
        }

        [Fact]
        public void CopyTo_InvalidArrayType_Throws()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            ICollection collection = CreateCollection(item1, item2, item3);

            Assert.Throws<InvalidCastException>(() => collection.CopyTo(new int[collection.Count], 0));
            Assert.Throws<InvalidCastException>(() => collection.CopyTo(new string[collection.Count], 0));
        }

        [Fact]
        public void Indexer_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            Assert.Same(item1, collection[0]);
            Assert.Same(item2, collection[1]);
            Assert.Same(item3, collection[2]);

            IList list = collection;
            list[0] = item2;
            list[1] = item3;
            list[2] = item1;
            Assert.Same(item2, list[0]);
            Assert.Same(item3, list[1]);
            Assert.Same(item1, list[2]);
        }

        [Fact]
        public void IndexOf_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2);

            Assert.Equal(0, collection.IndexOf(item1));
            Assert.Equal(1, collection.IndexOf(item2));
            Assert.Equal(-1, collection.IndexOf(item3));

            IList list = collection;
            Assert.Equal(0, list.IndexOf(item1));
            Assert.Equal(1, list.IndexOf(item2));
            Assert.Equal(-1, list.IndexOf(item3));
            Assert.Equal(-1, list.IndexOf("bogus"));
        }

        [Fact]
        public void Insert_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection();

            collection.Insert(0, item3);
            collection.Insert(0, item2);
            collection.Insert(0, item1);

            Assert.Equal(3, collection.Count);
            Assert.Same(item1, collection[0]);
            Assert.Same(item2, collection[1]);
            Assert.Same(item3, collection[2]);
        }

        [Fact]
        public void InsertAndClear_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection();

            collection.Insert(0, item1);
            collection.Insert(1, item2);
            collection.Insert(2, item3);
            Assert.Equal(3, collection.Count);
            Assert.Same(item1, collection[0]);
            Assert.Same(item2, collection[1]);
            Assert.Same(item3, collection[2]);

            collection.Clear();
            Assert.Equal(0, collection.Count);

            collection.Add(item1);
            collection.Add(item3);
            Assert.Equal(2, collection.Count);
            Assert.Same(item1, collection[0]);
            Assert.Same(item3, collection[1]);

            collection.Insert(1, item2);
            Assert.Equal(3, collection.Count);
            Assert.Same(item1, collection[0]);
            Assert.Same(item2, collection[1]);
            Assert.Same(item3, collection[2]);

            collection.Clear();
            Assert.Equal(0, collection.Count);

            IList list = collection;
            list.Insert(0, item1);
            list.Insert(1, item2);
            list.Insert(2, item3);
            Assert.Equal(3, list.Count);
            Assert.Same(item1, list[0]);
            Assert.Same(item2, list[1]);
            Assert.Same(item3, list[2]);

            list.Clear();
            Assert.Equal(0, list.Count);

            list.Add(item1);
            list.Add(item3);
            Assert.Equal(2, list.Count);
            Assert.Same(item1, list[0]);
            Assert.Same(item3, list[1]);

            list.Insert(1, item2);
            Assert.Equal(3, list.Count);
            Assert.Same(item1, list[0]);
            Assert.Same(item2, list[1]);
            Assert.Same(item3, list[2]);

            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void Remove_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2);

            collection.Remove(item1);
            Assert.Equal(1, collection.Count);
            Assert.Same(item2, collection[0]);

            collection.Remove(item2);
            Assert.Equal(0, collection.Count);

            // The explicit implementation of IList.Remove throws ArgumentException if
            // the item isn't in the collection, but the public Remove method does not
            // throw in the full framework.
            collection.Remove(item2);
            collection.Remove(new SqlBulkCopyColumnMapping(2, 2));


            IList list = CreateCollection(item1, item2);

            list.Remove(item1);
            Assert.Equal(1, list.Count);
            Assert.Same(item2, list[0]);

            list.Remove(item2);
            Assert.Equal(0, list.Count);

            AssertExtensions.Throws<ArgumentException>(null, () => list.Remove(item2));
            AssertExtensions.Throws<ArgumentException>(null, () => list.Remove(new SqlBulkCopyColumnMapping(2, 2)));
            AssertExtensions.Throws<ArgumentException>(null, () => list.Remove("bogus"));
        }

        [Fact]
        public void RemoveAt_BehavesAsExpected()
        {
            var item1 = new SqlBulkCopyColumnMapping(0, 0);
            var item2 = new SqlBulkCopyColumnMapping(1, 1);
            var item3 = new SqlBulkCopyColumnMapping(2, 2);

            SqlBulkCopyColumnMappingCollection collection = CreateCollection(item1, item2, item3);

            collection.RemoveAt(0);
            Assert.Equal(2, collection.Count);
            Assert.Same(item2, collection[0]);
            Assert.Same(item3, collection[1]);

            collection.RemoveAt(1);
            Assert.Equal(1, collection.Count);
            Assert.Same(item2, collection[0]);

            collection.RemoveAt(0);
            Assert.Equal(0, collection.Count);


            IList list = CreateCollection(item1, item2, item3);

            list.RemoveAt(0);
            Assert.Equal(2, list.Count);
            Assert.Same(item2, list[0]);
            Assert.Same(item3, list[1]);

            list.RemoveAt(1);
            Assert.Equal(1, list.Count);
            Assert.Same(item2, list[0]);

            list.RemoveAt(0);
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void SyncRoot_NotNullAndSameObject()
        {
            ICollection collection = CreateCollection();
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }
    }
}
