// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Since <see cref="Collection{T}"/> is just a wrapper base class around an <see cref="IList{T}"/>,
    /// we just verify that the underlying list is what we expect, validate that the calls which
    /// we expect are forwarded to the underlying list, and verify that the exceptions we expect
    /// are thrown.
    /// </summary>
    public class CollectionTests : CollectionTestBase
    {
        private static readonly Collection<int> s_empty = new Collection<int>();

        [Fact]
        public static void Ctor_Empty()
        {
            Collection<int> collection = new Collection<int>();
            Assert.Empty(collection);
        }

        [Fact]
        public static void Ctor_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => new Collection<int>(null));
        }

        [Fact]
        public static void Ctor_IList()
        {
            var collection = new TestCollection<int>(s_intArray);
            Assert.Same(s_intArray, collection.GetItems());
            Assert.Equal(s_intArray.Length, collection.Count);
        }

        [Fact]
        public static void GetItems_CastableToList()
        {
            //
            // Although MSDN only documents that Collection<T>.Items returns an IList<T>,
            // apps have made it through the Windows Store that successfully cast the return value
            // of Items to List<T>.
            //
            // Thus, we must grumble and continue to honor this behavior.
            //

            TestCollection<int> c = new TestCollection<int>();
            IList<int> il = c.GetItems();

            // Avoid using the List<T> type so that we don't have to depend on the System.Collections contract
            Type type = il.GetType();
            Assert.Equal(1, type.GenericTypeArguments.Length);
            Assert.Equal(typeof(int), type.GenericTypeArguments[0]);
            Assert.Equal("System.Collections.Generic.List`1", string.Format("{0}.{1}", type.Namespace, type.Name));
        }

        [Fact]
        public static void Item_Get_Set()
        {
            var collection = new ModifiableCollection<int>(s_intArray);
            for (int i = 0; i < s_intArray.Length; i++)
            {
                collection[i] = i;
                Assert.Equal(i, collection[i]);
            }
        }

        [Fact]
        public static void Item_Get_Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var collection = new ModifiableCollection<int>(s_intArray);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection[s_intArray.Length]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => s_empty[0]);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection[-1] = 0);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection[s_intArray.Length] = 0);
        }

        [Fact]
        public static void Item_Set_InvalidType_ThrowsArgumentException()
        {
            var collection = new Collection<int>(new Collection<int>(s_intArray));
            AssertExtensions.Throws<ArgumentException>("value", () => ((IList)collection)[1] = "Two");
        }

        [Fact]
        public static void IsReadOnly_ReturnsTrue()
        {
            var collection = new Collection<int>(s_intArray);
            Assert.True(((IList)collection).IsReadOnly);
            Assert.True(((IList<int>)collection).IsReadOnly);
        }

        [Fact]
        public static void Insert_ZeroIndex()
        {
            const int itemsToInsert = 5;
            var collection = new ModifiableCollection<int>();

            for (int i = itemsToInsert - 1; i >= 0; i--)
            {
                collection.Insert(0, i);
            }

            for (int i = 0; i < itemsToInsert; i++)
            {
                Assert.Equal(i, collection[i]);
            }
        }

        [Fact]
        public static void Insert_MiddleIndex()
        {
            const int insertIndex = 3;
            const int itemsToInsert = 5;

            var collection = new ModifiableCollection<int>(s_intArray);

            for (int i = 0; i < itemsToInsert; i++)
            {
                collection.Insert(insertIndex + i, i);
            }

            // Verify from the beginning of the collection up to insertIndex
            for (int i = 0; i < insertIndex; i++)
            {
                Assert.Equal(s_intArray[i], collection[i]);
            }

            // Verify itemsToInsert items starting from insertIndex
            for (int i = 0; i < itemsToInsert; i++)
            {
                Assert.Equal(i, collection[insertIndex + i]);
            }

            // Verify the rest of the items in the collection
            for (int i = insertIndex; i < s_intArray.Length; i++)
            {
                Assert.Equal(s_intArray[i], collection[itemsToInsert + i]);
            }
        }

        [Fact]
        public static void Insert_EndIndex()
        {
            const int itemsToInsert = 5;

            var collection = new ModifiableCollection<int>(s_intArray);

            for (int i = 0; i < itemsToInsert; i++)
            {
                collection.Insert(collection.Count, i);
            }

            for (int i = 0; i < itemsToInsert; i++)
            {
                Assert.Equal(i, collection[s_intArray.Length + i]);
            }
        }

        [Fact]
        public static void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var collection = new ModifiableCollection<int>(s_intArray);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(s_intArray.Length + 1, 0));
        }

        [Fact]
        public static void Clear()
        {
            var collection = new ModifiableCollection<int>(s_intArray);
            collection.Clear();
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public static void Contains()
        {
            var collection = new Collection<int>(s_intArray);
            for (int i = 0; i < s_intArray.Length; i++)
            {
                Assert.True(collection.Contains(s_intArray[i]));
            }

            for (int i = 0; i < s_excludedFromIntArray.Length; i++)
            {
                Assert.False(collection.Contains(s_excludedFromIntArray[i]));
            }
        }

        [Fact]
        public static void CopyTo()
        {
            var collection = new Collection<int>(s_intArray);
            const int targetIndex = 3;
            int[] intArray = new int[s_intArray.Length + targetIndex];

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => ((ICollection)collection).CopyTo(new int[s_intArray.Length, s_intArray.Length], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(intArray, -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(intArray, s_intArray.Length - 1));

            collection.CopyTo(intArray, targetIndex);
            for (int i = targetIndex; i < intArray.Length; i++)
            {
                Assert.Equal(collection[i - targetIndex], intArray[i]);
            }

            object[] objectArray = new object[s_intArray.Length + targetIndex];
            ((ICollection)collection).CopyTo(intArray, targetIndex);
            for (int i = targetIndex; i < intArray.Length; i++)
            {
                Assert.Equal(collection[i - targetIndex], intArray[i]);
            }
        }

        [Fact]
        public static void IndexOf()
        {
            var collection = new Collection<int>(s_intArray);

            for (int i = 0; i < s_intArray.Length; i++)
            {
                int item = s_intArray[i];
                Assert.Equal(Array.IndexOf(s_intArray, item), collection.IndexOf(item));
            }

            for (int i = 0; i < s_excludedFromIntArray.Length; i++)
            {
                Assert.Equal(-1, collection.IndexOf(s_excludedFromIntArray[i]));
            }
        }

        private static readonly int[] s_intSequence = new int[] { 0, 1, 2, 3, 4, 5 };

        [Fact]
        public static void RemoveAt()
        {
            VerifyRemoveAt(0, 1, new[] { 1, 2, 3, 4, 5 });
            VerifyRemoveAt(3, 2, new[] { 0, 1, 2, 5 });
            VerifyRemoveAt(4, 1, new[] { 0, 1, 2, 3, 5 });
            VerifyRemoveAt(5, 1, new[] { 0, 1, 2, 3, 4 });
            VerifyRemoveAt(0, 6, s_empty);
        }

        private static void VerifyRemoveAt(int index, int count, IEnumerable<int> expected)
        {
            var collection = new ModifiableCollection<int>(s_intSequence);

            for (int i = 0; i < count; i++)
            {
                collection.RemoveAt(index);
            }

            Assert.Equal(expected, collection);
        }

        [Fact]
        public static void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var collection = new ModifiableCollection<int>(s_intSequence);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveAt(s_intArray.Length));
        }

        [Fact]
        public static void MembersForwardedToUnderlyingIList()
        {
            var expectedApiCalls =
                IListApi.Count |
                IListApi.IsReadOnly |
                IListApi.IndexerGet |
                IListApi.IndexerSet |
                IListApi.Insert |
                IListApi.Clear |
                IListApi.Contains |
                IListApi.CopyTo |
                IListApi.GetEnumeratorGeneric |
                IListApi.IndexOf |
                IListApi.RemoveAt |
                IListApi.GetEnumerator;

            var list = new CallTrackingIList<int>(expectedApiCalls);
            var collection = new Collection<int>(list);

            int count = collection.Count;
            bool readOnly = ((IList)collection).IsReadOnly;
            int x = collection[0];
            collection[0] = 22;
            collection.Add(x);
            collection.Clear();
            collection.Contains(x);
            collection.CopyTo(s_intArray, 0);
            collection.GetEnumerator();
            collection.IndexOf(x);
            collection.Insert(0, x);
            collection.Remove(x);
            collection.RemoveAt(0);
            ((IEnumerable)collection).GetEnumerator();

            list.AssertAllMembersCalled();
        }

        [Fact]
        public void ReadOnly_ModifyingCollection_ThrowsNotSupportedException()
        {
            var collection = new Collection<int>(s_intArray);

            Assert.Throws<NotSupportedException>(() => collection[0] = 0);
            Assert.Throws<NotSupportedException>(() => collection.Add(0));
            Assert.Throws<NotSupportedException>(() => collection.Clear());
            Assert.Throws<NotSupportedException>(() => collection.Insert(0, 0));
            Assert.Throws<NotSupportedException>(() => collection.Remove(0));
            Assert.Throws<NotSupportedException>(() => collection.RemoveAt(0));
        }

        [Fact]
        public void IList_Add_ValidItemButThrowsInvalidCastExceptionFromOverride_ThrowsInvalidCastException()
        {
            var collection = new ObservableCollection<int>();
            collection.CollectionChanged += delegate { throw new InvalidCastException(); };

            Assert.Throws<InvalidCastException>(() => ((IList)collection).Add(1));
        }

        [Fact]
        public void IList_Insert_ValidItemButThrowsInvalidCastExceptionFromOverride_ThrowsInvalidCastException()
        {
            var collection = new ObservableCollection<int>();
            collection.CollectionChanged += delegate { throw new InvalidCastException(); };

            Assert.Throws<InvalidCastException>(() => ((IList)collection).Insert(0, 1));
        }

        [Fact]
        public void IList_Item_Set_ValidItemButThrowsInvalidCastExceptionFromOverride_ThrowsInvalidCastException()
        {
            var collection = new ObservableCollection<int>() { 1 };
            collection.CollectionChanged += delegate { throw new InvalidCastException(); };

            Assert.Throws<InvalidCastException>(() => ((IList)collection)[0] = 2);
        }

        private class TestCollection<T> : Collection<T>
        {
            public TestCollection()
            {
            }

            public TestCollection(IList<T> items) : base(items)
            {
            }

            public IList<T> GetItems() => Items;
        }

        private class ModifiableCollection<T> : Collection<T>
        {
            public ModifiableCollection()
            {
            }

            public ModifiableCollection(IList<T> items)
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
        }
    }
}
