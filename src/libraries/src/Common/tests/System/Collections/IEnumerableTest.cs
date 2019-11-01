// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using Xunit;

namespace Tests.Collections
{
    public enum CollectionOrder
    {
        Unspecified,
        Sequential
    }

    public abstract class IEnumerableTest<T>
    {
        private const int EnumerableSize = 16;

        protected T DefaultValue => default(T);

        protected bool MoveNextAtEndThrowsOnModifiedCollection => true;

        protected virtual CollectionOrder CollectionOrder => CollectionOrder.Sequential;

        protected abstract bool IsResetNotSupported { get; }
        protected abstract bool IsGenericCompatibility { get; }
        protected abstract object GenerateItem();

        protected object[] GenerateItems(int size)
        {
            var ret = new object[size];
            for (var i = 0; i < size; i++)
            {
                ret[i] = GenerateItem();
            }
            return ret;
        }

        /// <summary>
        ///     When overridden in a derived class, Gets an instance of the enumerable under test containing the given items.
        /// </summary>
        /// <param name="items">The items to initialize the enumerable with.</param>
        /// <returns>An instance of the enumerable under test containing the given items.</returns>
        protected abstract IEnumerable GetEnumerable(object[] items);

        /// <summary>
        ///     When overridden in a derived class, invalidates any enumerators for the given IEnumerable.
        /// </summary>
        /// <param name="enumerable">The <see cref="IEnumerable" /> to invalidate enumerators for.</param>
        /// <returns>The new set of items in the <see cref="IEnumerable" /></returns>
        protected abstract object[] InvalidateEnumerator(IEnumerable enumerable);

        private void RepeatTest(
            Action<IEnumerator, object[], int> testCode,
            int iters = 3)
        {
            object[] items = GenerateItems(32);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            for (var i = 0; i < iters; i++)
            {
                testCode(enumerator, items, i);
                if (IsResetNotSupported)
                {
                    enumerator = enumerable.GetEnumerator();
                }
                else
                {
                    enumerator.Reset();
                }
            }
        }

        private void RepeatTest(
            Action<IEnumerator, object[]> testCode,
            int iters = 3)
        {
            RepeatTest((e, i, it) => testCode(e, i), iters);
        }

        [Fact]
        public void MoveNextHitsAllItems()
        {
            RepeatTest(
                (enumerator, items) =>
                {
                    var iterations = 0;
                    while (enumerator.MoveNext())
                    {
                        iterations++;
                    }
                    Assert.Equal(items.Length, iterations);
                });
        }

        [Fact]
        public void CurrentThrowsAfterEndOfCollection()
        {
            if (IsGenericCompatibility)
            {
                return;
                    // apparently it is okay if enumerator.Current doesn't throw when the collection is generic.
            }

            RepeatTest(
                (enumerator, items) =>
                {
                    while (enumerator.MoveNext())
                    {
                    }
                    Assert.Throws<InvalidOperationException>(
                        () => enumerator.Current);
                });
        }

        [Fact]
        public void MoveNextFalseAfterEndOfCollection()
        {
            RepeatTest(
                (enumerator, items) =>
                {
                    while (enumerator.MoveNext())
                    {
                    }

                    Assert.False(enumerator.MoveNext());
                });
        }

        [Fact]
        public void Current()
        {
            // Verify that current returns proper result.
            RepeatTest(
                (enumerator, items, iteration) =>
                {
                    if (iteration == 1)
                    {
                        VerifyEnumerator(
                            enumerator,
                            items,
                            0,
                            items.Length/2,
                            true,
                            false);
                    }
                    else
                    {
                        VerifyEnumerator(enumerator, items);
                    }
                });
        }

        [Fact]
        public void Reset()
        {
            if (IsResetNotSupported)
            {
                RepeatTest(
                    (enumerator, items) =>
                    {
                        Assert.Throws<NotSupportedException>(
                            () => enumerator.Reset());
                    });
                RepeatTest(
                    (enumerator, items, iter) =>
                    {
                        if (iter == 1)
                        {
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                items.Length/2,
                                true,
                                false);
                            for (var i = 0; i < 3; i++)
                            {
                                Assert.Throws<NotSupportedException>(
                                    () => enumerator.Reset());
                            }
                            VerifyEnumerator(
                                enumerator,
                                items,
                                items.Length/2,
                                items.Length - (items.Length/2),
                                false,
                                true);
                        }
                        else if (iter == 2)
                        {
                            VerifyEnumerator(enumerator, items);
                            for (var i = 0; i < 3; i++)
                            {
                                Assert.Throws<NotSupportedException>(
                                    () => enumerator.Reset());
                            }
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                0,
                                false,
                                true);
                        }
                        else
                        {
                            VerifyEnumerator(enumerator, items);
                        }
                    });
            }
            else
            {
                RepeatTest(
                    (enumerator, items, iter) =>
                    {
                        if (iter == 1)
                        {
                            VerifyEnumerator(
                                enumerator,
                                items,
                                0,
                                items.Length/2,
                                true,
                                false);
                            enumerator.Reset();
                            enumerator.Reset();
                        }
                        else if (iter == 3)
                        {
                            VerifyEnumerator(enumerator, items);
                            enumerator.Reset();
                            enumerator.Reset();
                        }
                        else
                        {
                            VerifyEnumerator(enumerator, items);
                        }
                    },
                    5);
            }
        }

        [Fact]
        public void ModifyCollectionWithNewEnumerator()
        {
            IEnumerable enumerable =
                GetEnumerable(GenerateItems(EnumerableSize));
            IEnumerator enumerator = enumerable.GetEnumerator();
            InvalidateEnumerator(enumerable);
            VerifyModifiedEnumerator(enumerator, null, true, false);
        }

        [Fact]
        public void EnumerateFirstItemThenModify()
        {
            object[] items = GenerateItems(EnumerableSize);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            VerifyEnumerator(enumerator, items, 0, 1, true, false);
            object currentItem = enumerator.Current;
            InvalidateEnumerator(enumerable);
            VerifyModifiedEnumerator(
                enumerator,
                currentItem,
                false,
                false);
        }

        [Fact]
        public void EnumeratePartOfCollectionThenModify()
        {
            object[] items = GenerateItems(EnumerableSize);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            VerifyEnumerator(
                enumerator,
                items,
                0,
                items.Length/2,
                true,
                false);

            object currentItem = enumerator.Current;
            InvalidateEnumerator(enumerable);
            VerifyModifiedEnumerator(
                enumerator,
                currentItem,
                false,
                false);
        }

        [Fact]
        public void EnumerateEntireCollectionThenModify()
        {
            object[] items = GenerateItems(EnumerableSize);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            VerifyEnumerator(
                enumerator,
                items,
                0,
                items.Length,
                true,
                false);

            object currentItem = enumerator.Current;
            InvalidateEnumerator(enumerable);
            VerifyModifiedEnumerator(
                enumerator,
                currentItem,
                false,
                true);
        }

        [Fact]
        public void EnumerateThenModifyThrows()
        {
            object[] items = GenerateItems(EnumerableSize);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            VerifyEnumerator(
                enumerator,
                items,
                0,
                items.Length/2,
                true,
                false);

            object currentItem = enumerator.Current;
            InvalidateEnumerator(enumerable);
            Assert.Equal(currentItem, enumerator.Current);
            Assert.Throws<InvalidOperationException>(
                () => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(
                () => enumerator.Reset());
        }

        [Fact]
        public void EnumeratePastEndThenModify()
        {
            object[] items = GenerateItems(EnumerableSize);
            IEnumerable enumerable = GetEnumerable(items);
            IEnumerator enumerator = enumerable.GetEnumerator();
            // enumerate to the end
            VerifyEnumerator(enumerator, items);

            // add elements to the collection
            InvalidateEnumerator(enumerable);
            // check that it throws proper exceptions
            VerifyModifiedEnumerator(
                enumerator,
                DefaultValue,
                false,
                true);
        }

        private void VerifyModifiedEnumerator(
            IEnumerator enumerator,
            object expectedCurrent,
            bool expectCurrentThrow,
            bool atEnd)
        {
            if (expectCurrentThrow)
            {
                Assert.Throws<InvalidOperationException>(
                    () => enumerator.Current);
            }
            else
            {
                object current = enumerator.Current;
                for (var i = 0; i < 3; i++)
                {
                    Assert.Equal(expectedCurrent, current);
                    current = enumerator.Current;
                }
            }

            if (!atEnd || MoveNextAtEndThrowsOnModifiedCollection)
            {
                Assert.Throws<InvalidOperationException>(
                    () => enumerator.MoveNext());
            }
            else
            {
                Assert.False(enumerator.MoveNext());
            }

            if (!IsResetNotSupported)
            {
                Assert.Throws<InvalidOperationException>(
                    () => enumerator.Reset());
            }
        }

        private void VerifyEnumerator(
            IEnumerator enumerator,
            object[] expectedItems)
        {
            VerifyEnumerator(
                enumerator,
                expectedItems,
                0,
                expectedItems.Length,
                true,
                true);
        }

        private void VerifyEnumerator(
            IEnumerator enumerator,
            object[] expectedItems,
            int startIndex,
            int count,
            bool validateStart,
            bool validateEnd)
        {
            bool needToMatchAllExpectedItems = count - startIndex
                                               == expectedItems.Length;
            if (validateStart)
            {
                for (var i = 0; i < 3; i++)
                {
                    Assert.Throws<InvalidOperationException>(
                        () => enumerator.Current);
                }
            }

            int iterations;
            if (CollectionOrder == CollectionOrder.Unspecified)
            {
                var itemsVisited =
                    new BitArray(
                        needToMatchAllExpectedItems
                            ? count
                            : expectedItems.Length,
                        false);
                for (iterations = 0;
                     iterations < count && enumerator.MoveNext();
                     iterations++)
                {
                    object currentItem = enumerator.Current;
                    var itemFound = false;
                    for (var i = 0; i < itemsVisited.Length; ++i)
                    {
                        if (!itemsVisited[i]
                            && Equals(
                                currentItem,
                                expectedItems[
                                    i
                                    + (needToMatchAllExpectedItems
                                           ? startIndex
                                           : 0)]))
                        {
                            itemsVisited[i] = true;
                            itemFound = true;
                            break;
                        }
                    }
                    Assert.True(itemFound, "itemFound");

                    for (var i = 0; i < 3; i++)
                    {
                        object tempItem = enumerator.Current;
                        Assert.Equal(currentItem, tempItem);
                    }
                }
                if (needToMatchAllExpectedItems)
                {
                    for (var i = 0; i < itemsVisited.Length; i++)
                    {
                        Assert.True(itemsVisited[i]);
                    }
                }
                else
                {
                    var visitedItemCount = 0;
                    for (var i = 0; i < itemsVisited.Length; i++)
                    {
                        if (itemsVisited[i])
                        {
                            ++visitedItemCount;
                        }
                    }
                    Assert.Equal(count, visitedItemCount);
                }
            }
            else if (CollectionOrder == CollectionOrder.Sequential)
            {
                for (iterations = 0;
                     iterations < count && enumerator.MoveNext();
                     iterations++)
                {
                    object currentItem = enumerator.Current;
                    Assert.Equal(expectedItems[iterations], currentItem);
                    for (var i = 0; i < 3; i++)
                    {
                        object tempItem = enumerator.Current;
                        Assert.Equal(currentItem, tempItem);
                    }
                }
            }
            else
            {
                throw new ArgumentException(
                    "CollectionOrder is invalid.");
            }
            Assert.Equal(count, iterations);

            if (validateEnd)
            {
                for (var i = 0; i < 3; i++)
                {
                    Assert.False(
                        enumerator.MoveNext(),
                        "enumerator.MoveNext() returned true past the expected end.");
                }

                if (IsGenericCompatibility)
                {
                    return;
                }
                // apparently it is okay if enumerator.Current doesn't throw when the collection is generic.
                for (var i = 0; i < 3; i++)
                {
                    Assert.Throws<InvalidOperationException>(
                        () => enumerator.Current);
                }
            }
        }
    }
}
