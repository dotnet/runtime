// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.Collections
{
    public static class CollectionAssert
    {
        public static void Equal(
            IEnumerable expected,
            IEnumerable actual)
        {
            Assert.Equal(
                expected.OfType<object>(),
                actual.OfType<object>());
        }

        public static void Equal(IList expected, IList actual)
        {
            Equal((IEnumerable) expected, actual);
            // explicitly test Count and the indexer
            Assert.Equal(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                var expectedArray = expected as Array;
                var actualArray = expected as Array;
                int expectedLowerBound = expectedArray == null
                                             ? 0
                                             : expectedArray
                                                   .GetLowerBound(0);
                int actualLowerBound = actualArray == null
                                           ? 0
                                           : actualArray.GetLowerBound(
                                               0);
                Assert.Equal(
                    expected[i + expectedLowerBound],
                    actual[i + actualLowerBound]);
            }
        }
    }

    public static class IListExtensions
    {
        public static void AddRange(this IList list, IEnumerable items)
        {
            foreach (object item in items)
            {
                list.Add(item);
            }
        }

        public static void InsertRange(this IList list, object[] items)
        {
            list.InsertRange(0, items, 0, items.Length);
        }

        public static void InsertRange(
            this IList list,
            int listStartIndex,
            object[] items,
            int startIndex,
            int count)
        {
            int numToInsert = items.Length - startIndex;
            if (count < numToInsert)
            {
                numToInsert = count;
            }
            for (var i = 0; i < numToInsert; i++)
            {
                list.Insert(listStartIndex + i, items[startIndex + i]);
            }
        }

        public static void AddRange(
            this IList list,
            object[] items,
            int startIndex,
            int count)
        {
            int numToAdd = items.Length - startIndex;
            if (count < numToAdd)
            {
                numToAdd = count;
            }
            for (var i = 0; i < numToAdd; i++)
            {
                list.Add(items[startIndex + i]);
            }
        }
    }

    public abstract class IListTest<T> : ICollectionTest<T>
    {
        private readonly bool _expectedIsFixedSize;
        private readonly bool _expectedIsReadOnly;
        private bool _searchingThrowsFromInvalidValue;

        protected IListTest(
            bool isSynchronized,
            bool isReadOnly,
            bool isFixedSize) : base(isSynchronized)
        {
            _expectedIsReadOnly = isReadOnly;
            _expectedIsFixedSize = isFixedSize;
            SearchingThrowsFromInvalidValue = false;
        }

        protected bool ExpectedIsReadOnly
        {
            get { return _expectedIsReadOnly; }
        }

        protected bool ExpectedIsFixedSize
        {
            get { return _expectedIsFixedSize; }
        }

        protected bool SearchingThrowsFromInvalidValue
        {
            get { return _searchingThrowsFromInvalidValue; }
            set { _searchingThrowsFromInvalidValue = value; }
        }

        public static object[][] RemoveAtInvalidData
        {
            get
            {
                return new[]
                {
                    new object[] {int.MinValue, 16},
                    new object[] {-1, 16},
                    new object[] {0, 0},
                    new object[] {1, 1},
                    new object[] {16, 16}
                };
            }
        }

        [Fact]
        public void IsFixedSize()
        {
            Assert.Equal(ExpectedIsFixedSize, GetList(null).IsFixedSize);
        }

        [Fact]
        public void IsReadOnly()
        {
            Assert.Equal(ExpectedIsReadOnly, GetList(null).IsReadOnly);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void GetItemArgumentOutOfRange(int index)
        {
            IList list = GetList(null);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => list[index]);
            CollectionAssert.Equal(Array.Empty<object>(), list);
        }

        [Fact]
        public void GetItemArgumentOutOfRangeFilledCollection()
        {
            object[] items = GenerateItems(32);
            IList list = GetList(items);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => list[list.Count]);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void GetItemEmpty()
        {
            var items = new object[0];
            IList list = GetList(items);
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                CollectionAssert.Equal(items, list);
            }
            else
            {
                list.Clear();
                items = GenerateItems(1);
                list.AddRange(items);
                CollectionAssert.Equal(items, list);

                list.Clear();
                items = GenerateItems(1024);
                list.AddRange(items);
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void SetItemNotSupported()
        {
            if (ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(10);
                IList list = GetList(items);
                Assert.Throws<NotSupportedException>(
                    () => list[0] = GenerateItem());
                CollectionAssert.Equal(items, list);

                Assert.Throws<NotSupportedException>(
                    () => list[list.Count - 1] = GenerateItem());
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void SetItemArgumentException()
        {
            Type[] expectedExceptions;
            if (ExpectedIsReadOnly)
            {
                expectedExceptions = new[]
                {
                    typeof (ArgumentOutOfRangeException),
                    typeof (NotSupportedException)
                };
            }
            else
            {
                expectedExceptions = new[]
                {
                    typeof (ArgumentOutOfRangeException)
                };
            }
            {
                object[] items = GenerateItems(10);
                IList list = GetList(items);
                AssertThrows(
                    expectedExceptions,
                    () => list[int.MinValue] = GenerateItem());
                CollectionAssert.Equal(items, list);

                AssertThrows(
                    expectedExceptions,
                    () => list[-1] = GenerateItem());
                CollectionAssert.Equal(items, list);
            }

            {
                object[] items = GenerateItems(0);
                IList list = GetList(items);
                AssertThrows(
                    expectedExceptions,
                    () => list[0] = GenerateItem());
                CollectionAssert.Equal(items, list);

                if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
                {
                    items = GenerateItems(32);
                    list.AddRange(items);
                    AssertThrows(
                        expectedExceptions,
                        () => list[list.Count] = GenerateItem());
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItemInvalidValue()
        {
            object[] items = GenerateItems(32);
            IList list = GetList(items);
            if (IsGenericCompatibility)
            {
                foreach (object invalid in GetInvalidValues())
                {
                    Type[] expectedExceptions;
                    if (invalid == null)
                    {
                        if (ExpectedIsReadOnly)
                        {
                            expectedExceptions = new[]
                            {
                                typeof (ArgumentNullException),
                                typeof (NotSupportedException)
                            };
                        }
                        else
                        {
                            expectedExceptions = new[]
                            {
                                typeof (ArgumentNullException)
                            };
                        }
                    }
                    else if (ExpectedIsReadOnly)
                    {
                        expectedExceptions = new[]
                        {
                            typeof (ArgumentException),
                            typeof (NotSupportedException)
                        };
                    }
                    else
                    {
                        expectedExceptions = new[]
                        {
                            typeof (ArgumentException)
                        };
                    }

                    object invalid1 = invalid;
                    AssertThrows(
                        expectedExceptions,
                        () => list[0] = invalid1);
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItemNonNull()
        {
            if (!ExpectedIsReadOnly)
            {
                if (ItemsMustBeNonNull)
                {
                    object[] items = GenerateItems(32);
                    IList list = GetList(items);
                    Assert.Throws<ArgumentNullException>(
                        () => list[0] = null);
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItemNullable()
        {
            if (!ExpectedIsReadOnly)
            {
                if (!ItemsMustBeNonNull)
                {
                    object[] items = GenerateItems(32);
                    IList list = GetList(items);
                    list[0] = null;
                    items[0] = null;
                    CollectionAssert.Equal(items, list);

                    items = GenerateItems(32);
                    list = GetList(items);
                    list[list.Count - 1] = null;
                    items[items.Length - 1] = null;
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItemUnique()
        {
            if (!ExpectedIsReadOnly)
            {
                if (ItemsMustBeUnique)
                {
                    object[] items = GenerateItems(32);
                    IList list = GetList(items);
                    Assert.Throws<ArgumentException>(
                        () => list[0] = items[1]);
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItemNotUnique()
        {
            if (!ExpectedIsReadOnly)
            {
                if (!ItemsMustBeUnique)
                {
                    object[] items = GenerateItems(32);
                    IList list = GetList(items);
                    for (var i = 0; i < items.Length; i++)
                    {
                        list[i] = items[items.Length - i - 1];
                    }
                    Array.Reverse(items);
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void SetItem()
        {
            if (!ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(32);
                var origItems = (object[]) items.Clone();
                IList list = GetList(items);

                // verify setting every item
                // reverse list, setting items to new elements to maintain uniqueness.
                for (var i = 0; i < items.Length; i++)
                {
                    if (i < items.Length/2)
                    {
                        list[items.Length - i - 1] = GenerateItem();
                    }
                    list[i] = items[items.Length - i - 1];
                }

                Array.Reverse(items);
                CollectionAssert.Equal(items, list);

                // verify setting items back to the original
                items = origItems;
                if (ItemsMustBeUnique)
                {
                    for (var i = 0; i < items.Length; i++)
                    {
                        list[i] = GenerateItem();
                    }
                }

                for (var i = 0; i < items.Length; i++)
                {
                    list[i] = items[i];
                }

                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddFixedSizeOrReadonlyThrows()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(10);
                IList list = GetList(items);
                Assert.Throws<NotSupportedException>(
                    () => list.Add(GenerateItem()));
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddWithNullValueInMiddle()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                const int size = 32;
                object[] items;
                IList list = GetList(null);
                object[] tempItems = GenerateItems(size);
                for (var i = 0; i < tempItems.Length/2; i++)
                {
                    list.Add(tempItems[i]);
                }

                if (ItemsMustBeNonNull)
                {
                    items = tempItems;
                    Assert.Throws<ArgumentNullException>(
                        () => list.Add(null));
                }
                else
                {
                    const int sizeWithNull = size + 1;
                    items = new object[sizeWithNull];
                    list.Add(null);
                    items[sizeWithNull/2] = null;
                    Array.Copy(tempItems, 0, items, 0, sizeWithNull/2);
                    Array.Copy(
                        tempItems,
                        sizeWithNull/2,
                        items,
                        sizeWithNull/2 + 1,
                        sizeWithNull/2);
                }
                for (var i = 0; i < size/2; i++)
                {
                    list.Add(tempItems[size/2 + i]);
                }
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddWithNullValueAtBeginning()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items;
                if (ItemsMustBeNonNull)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => list.Add(null));
                    items = GenerateItems(16);
                    list.AddRange(items);
                }
                else
                {
                    const int size = 17;
                    items = new object[size];
                    list.Add(null);
                    items[0] = null;
                    object[] tempItems = GenerateItems(size - 1);
                    list.AddRange(tempItems);
                    Array.Copy(tempItems, 0, items, 1, size - 1);
                }

                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddWithNullValueAtEnd()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(16);
                IList list = GetList(items);

                if (ItemsMustBeNonNull)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => list.Add(null));
                }
                else
                {
                    items[items.Length - 1] = null;
                    list.RemoveAt(list.Count - 1);
                    list.Add(null);
                }

                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddDuplicateValue()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                object[] items;
                IList list = GetList(null);
                if (ItemsMustBeUnique)
                {
                    items = GenerateItems(16);
                    list.AddRange(items);
                    Assert.Throws<ArgumentException>(
                        () => list.Add(items[0]));
                }
                else
                {
                    items = new object[32];
                    object[] tempItems = GenerateItems(16);
                    list.AddRange(tempItems);
                    list.AddRange(tempItems);

                    Array.Copy(tempItems, 0, items, 0, 16);
                    Array.Copy(tempItems, 0, items, 16, 16);
                }
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddAndClear()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                list.AddRange(GenerateItems(16));
                list.Clear();
                object[] items = GenerateItems(8);
                list.AddRange(items);
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddAndRemove()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                var items = new object[23];
                IList list = GetList(null);
                object[] tempItems = GenerateItems(32);
                for (var i = 0; i < 16; i++)
                {
                    list.Add(tempItems[i]);
                }
                for (var i = 0; i < 16; i++)
                {
                    if ((i & 1) == 0)
                    {
                        list.RemoveAt(i/2);
                    }
                    else
                    {
                        items[i/2] = tempItems[i];
                    }
                }

                list.RemoveAt(list.Count - 1);
                for (var i = 0; i < 16; i++)
                {
                    list.Add(tempItems[16 + i]);
                }

                Array.Copy(tempItems, 16, items, 7, 16);

                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void AddAndRemoveAll()
        {
            IList list = GetList(null);
            object[] tempItems = GenerateItems(16);
            list.AddRange(tempItems);
            int count = tempItems.Length;
            foreach (object item in tempItems)
            {
                list.Remove(item);
                count--;
                Assert.Equal(count, list.Count);
            }
            Assert.Equal(0, list.Count);

            object[] items = GenerateItems(16);
            list.AddRange(items);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddWithInvalidTypes()
        {
            if (IsGenericCompatibility)
            {
                object[] items = GenerateItems(32);
                IList list = GetList(items);
                foreach (object invalid in GetInvalidValues())
                {
                    Type[] expectedExceptions;
                    if (ExpectedIsFixedSize)
                    {
                        expectedExceptions = new[]
                        {
                            typeof (ArgumentException),
                            typeof (NotSupportedException)
                        };
                    }
                    else
                    {
                        expectedExceptions = new[]
                        {
                            typeof (ArgumentException)
                        };
                    }
                    object invalid1 = invalid;
                    AssertThrows(
                        expectedExceptions,
                        () => list.Add(invalid1));
                    CollectionAssert.Equal(items, list);
                }
            }
        }

        [Fact]
        public void ClearWithReadOnlyOrFixedSizeThrows()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(32);
                IList list = GetList(items);

                Assert.Throws<NotSupportedException>(() => list.Clear());
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void ClearOnEmptyList()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                list.Clear();
                // no exception should have been generated.
                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void RepeatClearOnEmptyList()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                list.Clear();
                list.Clear();
                list.Clear();
                // no exception should have been generated.
                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void AddRemoveClearAdd()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(16);
                list.AddRange(items);

                for (var i = 0; i < items.Length; i++)
                {
                    if ((i & 1) == 0)
                    {
                        list.RemoveAt(i/2);
                    }
                }
                list.RemoveAt(list.Count - 1);
                list.Clear();
                // no exception should have been generated
                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void AddRemoveAllClearAdd()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(16);
                list.AddRange(items);

                foreach (object item in items)
                {
                    list.Remove(item);
                }

                list.Clear();
                // no exception should have been generated
                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void ClearOneItem()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                list.Clear();
                // no exception should have been generated
                Assert.Equal(0, list.Count);
            }
        }

        [Fact]
        public void EmptyCollectionContainsNonNull()
        {
            IList list = GetList(null);
            Assert.False(list.Contains(GenerateItem()));
        }

        [Fact]
        public void EmptyCollectionContainsNull()
        {
            IList list = GetList(null);
            if (ItemsMustBeNonNull && SearchingThrowsFromInvalidValue)
            {
                Assert.Throws<ArgumentException>(
                    () => list.Contains(null));
            }
            else
            {
                Assert.False(list.Contains(null));
            }
        }

        [Fact]
        public void EmptyCollectionAddThenContainsSame()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                Assert.True(list.Contains(items[0]));
            }
        }

        [Fact]
        public void EmptyCollectionAddThenContainsDifferent()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                Assert.False(list.Contains(GenerateItem()));
            }
        }

        [Fact]
        public void EmptyCollectionAddThenContainsNull()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                if (ItemsMustBeNonNull
                    && SearchingThrowsFromInvalidValue)
                {
                    Assert.Throws<ArgumentException>(
                        () => list.Contains(null));
                }
                else
                {
                    Assert.False(list.Contains(null));
                }
            }
        }

        [Fact]
        public void AddAndContainsNullValue()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly
                && !ItemsMustBeNonNull)
            {
                IList list = GetList(null);
                list.Add(null);
                Assert.True(list.Contains(null));
            }
        }

        [Fact]
        public void ContainsValueThatExistsTwice()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly
                && !ItemsMustBeNonNull && !ItemsMustBeUnique)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(16);
                list.Add(items);
                list.Add(items);

                for (var i = 0; i < items.Length; i++)
                {
                    Assert.True(list.Contains(items[i]));
                }
                Assert.False(list.Contains(GenerateItem()));
            }
        }

        [Theory]
        [InlineData(32, 0)]
        [InlineData(32, 16)]
        [InlineData(32, 31)]
        public void NullInMiddleContains(
            int collectionSize,
            int nullIndex)
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly
                && !ItemsMustBeNonNull)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(collectionSize);
                list.AddRange(items);
                list.Insert(nullIndex, null);

                foreach (object item in items)
                {
                    Assert.True(list.Contains(item));
                }
                Assert.True(list.Contains(null));
                Assert.False(list.Contains(GenerateItem()));
            }
        }

        [Fact]
        public void ContainsUniqueItems()
        {
            object[] items = GenerateItems(32);
            IList list = GetList(items);
            for (var i = 0; i < items.Length; i++)
            {
                Assert.True(list.Contains(items[i]));
            }
            Assert.False(list.Contains(GenerateItem()));
        }

        [Fact]
        public void ContainsInvalidValue()
        {
            if (IsGenericCompatibility)
            {
                foreach (object invalid in GetInvalidValues())
                {
                    object invalid1 = invalid;
                    IList list = GetList(null);
                    if (SearchingThrowsFromInvalidValue)
                    {
                        Assert.Throws<ArgumentException>(
                            () => list.Contains(invalid1));
                    }
                    else
                    {
                        Assert.False(list.Contains(invalid1));
                    }
                }
            }
        }

        [Fact]
        public void EmptyCollectionIndexOf()
        {
            IList list = GetList(null);
            Assert.Equal(-1, list.IndexOf(GenerateItem()));
        }

        [Fact]
        public void EmptyCollectionIndexOfNull()
        {
            IList list = GetList(null);
            if (ItemsMustBeNonNull && SearchingThrowsFromInvalidValue)
            {
                Assert.Throws<ArgumentException>(
                    () => list.IndexOf(null));
            }
            else
            {
                Assert.Equal(-1, list.IndexOf(null));
            }
        }

        [Fact]
        public void AddIndexOfSameElement()
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                Assert.Equal(0, list.IndexOf(items[0]));
            }
        }

        [Fact]
        public void AddIndexOfDifferentElement()
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                Assert.Equal(-1, list.IndexOf(GenerateItem()));
            }
        }

        [Fact]
        public void AddIndexOfNull()
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(1);
                list.AddRange(items);
                if (ItemsMustBeNonNull
                    && SearchingThrowsFromInvalidValue)
                {
                    Assert.Throws<ArgumentException>(
                        () => list.IndexOf(null));
                }
                else
                {
                    Assert.Equal(-1, list.IndexOf(null));
                }
            }
        }

        [Fact]
        public void AddNullIndexOfNull()
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize
                && !ItemsMustBeNonNull)
            {
                IList list = GetList(null);
                list.Add(null);
                Assert.Equal(0, list.IndexOf(null));
            }
        }

        [Fact]
        public void IndexOfNonUnique()
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize
                && !ItemsMustBeUnique)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(16);
                list.AddRange(items);
                list.AddRange(items);
                for (var i = 0; i < items.Length; i++)
                {
                    Assert.Equal(i, list.IndexOf(items[i]));
                }

                Assert.Equal(-1, list.IndexOf(GenerateItem()));
            }
        }

        [Theory]
        [InlineData(32, 0)]
        [InlineData(32, 16)]
        [InlineData(32, 31)]
        public void IndexOfNull(int collectionSize, int nullIndex)
        {
            if (!ExpectedIsReadOnly && !ExpectedIsFixedSize
                && !ItemsMustBeNonNull)
            {
                IList list = GetList(null);
                object[] items = GenerateItems(collectionSize);
                list.AddRange(items);

                list.Insert(nullIndex, null);

                for (var i = 0; i < nullIndex; i++)
                {
                    Assert.Equal(i, list.IndexOf(items[i]));
                }
                Assert.Equal(nullIndex, list.IndexOf(null));
                for (int i = nullIndex; i < collectionSize; i++)
                {
                    Assert.Equal(i + 1, list.IndexOf(items[i]));
                }

                Assert.Equal(-1, list.IndexOf(GenerateItem()));
            }
        }

        [Fact]
        public void IndexOfUniqueItems()
        {
            object[] items = GenerateItems(32);
            IList list = GetList(items);

            for (var i = 0; i < items.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(items[i]));
            }

            Assert.Equal(-1, list.IndexOf(GenerateItem()));
        }

        [Fact]
        public void IndexOfInvalidTypes()
        {
            if (!IsGenericCompatibility)
            {
                return;
            }
            foreach (object invalid  in GetInvalidValues())
            {
                IList list = GetList(null);
                object invalid1 = invalid;
                if (SearchingThrowsFromInvalidValue)
                {
                    Assert.Throws<ArgumentException>(
                        () => list.IndexOf(invalid1));
                }
                else
                {
                    Assert.Equal(-1, list.IndexOf(invalid1));
                }
            }
        }

        [Theory]
        [InlineData(int.MinValue, 16)]
        [InlineData(-1, 16)]
        [InlineData(1, 0)]
        [InlineData(33, 32)]
        [InlineData(int.MaxValue, 32)]
        public void InsertInvalidIndexThrows(int index, int size)
        {
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => list.Insert(index, GenerateItem()));
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertInvalidTypes()
        {
            if (!IsGenericCompatibility)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            foreach (object i in GetInvalidValues())
            {
                object invalid = i;
                if (invalid == null)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => list.Insert(0, null));
                }
                else
                {
                    Assert.Throws<ArgumentException>(
                        () => list.Insert(0, invalid));
                }

                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void FixedSizeOrReadonlyInsertThrows()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            Assert.Throws<NotSupportedException>(
                () => list.Insert(0, GenerateItem()));
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertItems()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(32);
            IList list = GetList(null);
            for (var i = 0; i < items.Length; i++)
            {
                list.Insert(i, items[i]);
            }
            CollectionAssert.Equal(items, list);
        }

        [Theory]
        [InlineData(0, 32)]
        [InlineData(16, 32)]
        [InlineData(32, 32)]
        public void InsertNull(int index, int size)
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            if (ItemsMustBeNonNull)
            {
                Assert.Throws<ArgumentNullException>(
                    () => list.Insert(index, null));
            }
            else
            {
                List<object> l = items.ToList();
                l.Insert(index, null);
                items = l.ToArray();
                list.Insert(index, null);
            }
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertExistingValue()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            if (ItemsMustBeUnique)
            {
                Assert.Throws<ArgumentException>(
                    () => list.Insert(0, items[0]));
            }
            else
            {
                foreach (object item in items)
                {
                    list.Insert(list.Count, item);
                }
                items = items.Push(items);
            }
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertClearInsert()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(null);
            list.InsertRange(items);
            list.Clear();
            items = GenerateItems(16);
            list.InsertRange(items);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertRemoveInsert()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(32);
            IList list = GetList(null);
            list.InsertRange(items);
            var tempItems = (object[]) items.Clone();
            for (var i = 0; i < 16; i++)
            {
                list.RemoveAt(0);
            }

            Array.Copy(tempItems, 16, items, 0, 8);
            Array.Copy(tempItems, 24, items, 24, 8);
            tempItems = GenerateItems(16);
            for (var i = 0; i < 16; i++)
            {
                list.Insert(8 + i, tempItems[i]);
            }

            Array.Copy(tempItems, 0, items, 8, 16);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertRemoveAllInsert()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(32);
            IList list = GetList(null);
            list.InsertRange(items);
            for (var i = 0; i < 32; i++)
            {
                list.Remove(items[i]);
            }
            items = GenerateItems(32);
            list.InsertRange(items);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertAndAdd()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(48);
            IList list = GetList(null);
            list.InsertRange(0, items, 0, 16);
            list.AddRange(items, 16, 16);
            list.InsertRange(32, items, 32, 16);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void InsertAndRemove()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(32);
            var tempItems = new object[33];
            IList list = GetList(items);
            for (var i = 0; i < items.Length; i++)
            {
                object newObject = GenerateItem();
                list.Insert(i, newObject);
                Array.Copy(items, 0, tempItems, 0, i);
                tempItems[i] = newObject;
                Array.Copy(items, i, tempItems, i + 1, items.Length - i);
                CollectionAssert.Equal(tempItems, list);
                list.RemoveAt(i);
            }
        }

        [Fact]
        public void RemoveReadOnlyFixedSizeThrows()
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            Assert.Throws<NotSupportedException>(
                () => list.Remove(GenerateItem()));
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            list.Remove(GenerateItem());
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveNullEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            if (ItemsMustBeNonNull && SearchingThrowsFromInvalidValue)
            {
                Assert.Throws<ArgumentException>(
                    () => list.Remove(null));
            }
            else
            {
                list.Remove(null);
            }
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddRemoveValueEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            object item = GenerateItem();
            list.Add(item);
            list.Remove(item);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddRemoveDifferentValueEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            object item = GenerateItem();
            list.Add(item);
            items = items.Push(item);
            list.Remove(GenerateItem());
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddNonNullRemoveNullEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            object item = GenerateItem();
            list.Add(item);
            items = items.Push(item);
            if (ItemsMustBeNonNull && SearchingThrowsFromInvalidValue)
            {
                Assert.Throws<ArgumentException>(
                    () => list.Remove(null));
            }
            else
            {
                list.Remove(null);
            }
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddRemoveNullEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            if (ItemsMustBeNonNull)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            list.Add(null);
            list.Remove(null);
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void AddNullRemoveNonNullEmptyCollection()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            if (ItemsMustBeNonNull)
            {
                return;
            }
            object[] items = GenerateItems(0);
            IList list = GetList(items);
            list.Add(null);
            items = items.Push(null);
            list.Remove(GenerateItem());
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveItemsExistsTwice()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            if (ItemsMustBeUnique)
            {
                return;
            }

            object[] items = GenerateItems(16);
            var tempItems = (object[]) items.Clone();
            IList list = GetList(items);
            list.AddRange(items);
            for (var i = 0; i < items.Length; i++)
            {
                int index = i + 1;
                items =
                    new object[
                        tempItems.Length + tempItems.Length - index];
                Array.Copy(tempItems, index, items, 0, 16 - index);
                Array.Copy(tempItems, 0, items, 16 - index, 16);

                list.Remove(tempItems[i]);
                CollectionAssert.Equal(items, list);
            }
        }

        [Fact]
        public void RemoveNonExisting()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            list.Remove(GenerateItem());
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveItems()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(16);
            IList list = GetList(items);
            for (var i = 0; i < items.Length; i++)
            {
                list.Remove(items[i]);
                CollectionAssert.Equal(items.Slice(i + 1), list);
            }
        }

        [Fact]
        public void RemoveNull()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            if (ItemsMustBeNonNull)
            {
                return;
            }
            var items = new object[15];
            IList list = GetList(null);
            object[] tempItems = GenerateItems(16);
            list.AddRange(tempItems);
            Array.Copy(tempItems, 0, items, 0, 11);
            Array.Copy(tempItems, 12, items, 11, 4);
            list.Insert(8, null);
            list.Remove(tempItems[11]);
            list.Remove(null);
            list.Remove(GenerateItem());
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveInvalidValues()
        {
            if (!IsGenericCompatibility)
            {
                return;
            }
            Type[] expectedExceptions = ExpectedIsFixedSize
                                            ? new[]
                                            {
                                                typeof (
                                                  ArgumentException),
                                                typeof (
                                                  NotSupportedException)
                                            }
                                            : new[]
                                            {
                                                typeof (
                                                  ArgumentException)
                                            };

            object[] items = GenerateItems(16);
            IList list = GetList(items);
            foreach (object i in GetInvalidValues())
            {
                object invalid = i;
                if (ExpectedIsFixedSize
                    || SearchingThrowsFromInvalidValue)
                {
                    AssertThrows(
                        expectedExceptions,
                        () => list.Remove(invalid));
                }
                else
                {
                    list.Remove(invalid);
                }
                CollectionAssert.Equal(items, list);
            }
        }

        [Theory]
        [MemberData(nameof(RemoveAtInvalidData))]
        public void RemoveAtInvalid(int index, int size)
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => list.RemoveAt(index));
            CollectionAssert.Equal(items, list);
        }

        [Theory]
        [MemberData(nameof(RemoveAtInvalidData))]
        public void RemoveAtInvalidReadOnly(int index, int size)
        {
            if (!ExpectedIsFixedSize && !ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            AssertThrows(
                new[]
                {
                    typeof (ArgumentOutOfRangeException),
                    typeof (NotSupportedException)
                },
                () => list.RemoveAt(index));
            CollectionAssert.Equal(items, list);
        }

        [Fact]
        public void RemoveAtFixedSizeThrows()
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                object[] items = GenerateItems(16);
                IList list = GetList(items);
                Assert.Throws<NotSupportedException>(
                    () => list.RemoveAt(0));
                CollectionAssert.Equal(items, list);
            }
        }

        [Theory]
        [InlineData(32)]
        [InlineData(16)]
        public void RemoveAtDescending(int size)
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            for (var i = 0; i < size; i++)
            {
                list.RemoveAt(size - i - 1);
                items = items.Slice(0, items.Length - 1);
                CollectionAssert.Equal(items, list);
            }
        }

        [Theory]
        [InlineData(32)]
        [InlineData(16)]
        public void RemoveAt(int size)
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);

            for (var i = 0; i < size; i++)
            {
                list.RemoveAt(0);
                items = items.Slice(1);
                CollectionAssert.Equal(items, list);
            }
        }

        [Theory]
        [InlineData(32)]
        [InlineData(16)]
        public void RemoveAtEachIndex(int size)
        {
            if (ExpectedIsFixedSize || ExpectedIsReadOnly)
            {
                return;
            }
            object[] items = GenerateItems(size);
            IList list = GetList(items);
            for (var i = 0; i < size; i++)
            {
                list.RemoveAt(i);
                CollectionAssert.Equal(items.RemoveAt(i), list);
                list.Insert(i, items[i]);
            }
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected abstract IEnumerable GetInvalidValues();

        private IList GetList(object[] items)
        {
            ICollection obj = GetCollection(items);
            Assert.IsAssignableFrom<IList>(obj);
            return (IList) obj;
        }
    }

    public abstract class IListTest<TList, T> : IListTest<T>
        where TList : IList
    {
        private readonly bool _isGenericCompatibility;
        private readonly bool _isResetNotSupported;
        private readonly bool _itemsMustBeUnique;

        protected IListTest(
            bool isSynchronized,
            bool isReadOnly,
            bool isFixedSize,
            bool isResetNotSupported,
            bool isGenericCompatibility,
            bool itemsMustBeUnique)
            : base(isSynchronized, isReadOnly, isFixedSize)
        {
            _isResetNotSupported = isResetNotSupported;
            _isGenericCompatibility = isGenericCompatibility;
            _itemsMustBeUnique = itemsMustBeUnique;
            ValidArrayTypes = new[] {typeof (object), typeof (T)};
            SearchingThrowsFromInvalidValue = false;
        }

        protected override sealed bool IsResetNotSupported
        {
            get { return _isResetNotSupported; }
        }

        protected override sealed bool IsGenericCompatibility
        {
            get { return _isGenericCompatibility; }
        }

        protected override sealed bool ItemsMustBeUnique
        {
            get { return _itemsMustBeUnique; }
        }

        protected override sealed bool ItemsMustBeNonNull
        {
            get { return default(T) != null; }
        }

        protected override sealed object GenerateItem()
        {
            return CreateItem();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected abstract T CreateItem();

        /// <summary>
        ///     When overridden in a derived class, Gets an instance of the list under test containing the given items.
        /// </summary>
        /// <param name="items">The items to initialize the list with.</param>
        /// <returns>An instance of the list under test containing the given items.</returns>
        protected abstract TList CreateList(IEnumerable<T> items);

        /// <summary>
        ///     When overridden in a derived class, Gets an instance of the enumerable under test containing the given items.
        /// </summary>
        /// <param name="items">The items to initialize the enumerable with.</param>
        /// <returns>An instance of the enumerable under test containing the given items.</returns>
        protected override sealed IEnumerable GetEnumerable(
            object[] items)
        {
            return
                CreateList(
                    items == null
                        ? Enumerable.Empty<T>()
                        : items.Cast<T>());
        }

        /// <summary>
        ///     When overridden in a derived class, invalidates any enumerators for the given list.
        /// </summary>
        /// <param name="list">The list to invalidate enumerators for.</param>
        /// <returns>The new contents of the list.</returns>
        protected abstract IEnumerable<T> InvalidateEnumerator(
            TList list);

        /// <summary>
        ///     When overridden in a derived class, invalidates any enumerators for the given IEnumerable.
        /// </summary>
        /// <param name="enumerable">The <see cref="IEnumerable" /> to invalidate enumerators for.</param>
        /// <returns>The new set of items in the <see cref="IEnumerable" /></returns>
        protected override sealed object[] InvalidateEnumerator(
            IEnumerable enumerable)
        {
            return
                InvalidateEnumerator((TList) enumerable)
                    .OfType<object>()
                    .ToArray();
        }
    }
}
