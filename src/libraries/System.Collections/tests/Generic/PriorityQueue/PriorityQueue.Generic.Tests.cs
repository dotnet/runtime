// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Tests
{
    public abstract class PriorityQueue_Generic_Tests<TElement, TPriority> : TestBase<(TElement, TPriority)>
    {
        #region Helper methods

        protected IEnumerable<(TElement, TPriority)> GenericIEnumerableFactory(int count)
        {
            const int MagicValue = 34;
            int seed = count * MagicValue;
            for (int i = 0; i < count; i++)
            {
                yield return CreateT(seed++);
            }
        }

        protected PriorityQueue<TElement, TPriority> GenericPriorityQueueFactory(
            int initialCapacity, int countOfItemsToGenerate, out List<(TElement element, TPriority priority)> generatedItems)
        {
            generatedItems = this.GenericIEnumerableFactory(countOfItemsToGenerate).ToList();

            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity);
            foreach (var (element, priority) in generatedItems)
            {
                queue.Enqueue(element, priority);
            }

            return queue;
        }

        #endregion

        #region Constructors

        [Fact]
        public void PriorityQueue_Generic_Constructor()
        {
            var queue = new PriorityQueue<TElement, TPriority>();

            Assert.Equal(expected: 0, queue.Count);
            Assert.Empty(queue.UnorderedItems);
            Assert.Equal(queue.Comparer, Comparer<TPriority>.Default);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_Constructor_int(int initialCapacity)
        {
            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity);

            Assert.Empty(queue.UnorderedItems);
        }

        [Fact]
        public void PriorityQueue_Generic_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new PriorityQueue<TElement, TPriority>(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new PriorityQueue<TElement, TPriority>(int.MinValue));
        }

        [Fact]
        public void PriorityQueue_Generic_Constructor_IComparer()
        {
            IComparer<TPriority> comparer = Comparer<TPriority>.Default;
            var queue = new PriorityQueue<TElement, TPriority>(comparer);

            Assert.Equal(comparer, queue.Comparer);
        }

        [Fact]
        public void PriorityQueue_Generic_Constructor_IComparer_Null()
        {
            var queue = new PriorityQueue<TElement, TPriority>((IComparer<TPriority>)null);
            Assert.Equal(Comparer<TPriority>.Default, queue.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_Constructor_int_IComparer(int initialCapacity)
        {
            IComparer<TPriority> comparer = Comparer<TPriority>.Default;
            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity);

            Assert.Empty(queue.UnorderedItems);
            Assert.Equal(comparer, queue.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_Constructor_IEnumerable(int count)
        {
            HashSet<(TElement, TPriority)> itemsToEnqueue = this.GenericIEnumerableFactory(count).ToHashSet();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>(itemsToEnqueue);
            Assert.True(itemsToEnqueue.SetEquals(queue.UnorderedItems));
        }

        #endregion

        #region Enqueue, Dequeue, Peek

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_Enqueue(int count)
        {
            PriorityQueue<TElement, TPriority> queue = GenericPriorityQueueFactory(count, count, out var generatedItems);
            HashSet<(TElement, TPriority)> expectedItems = generatedItems.ToHashSet();

            Assert.Equal(count, queue.Count);
            var actualItems = queue.UnorderedItems.ToArray();
            Assert.True(expectedItems.SetEquals(actualItems));
        }

        [Fact]
        public void PriorityQueue_Generic_Dequeue_EmptyCollection()
        {
            var queue = new PriorityQueue<TElement, TPriority>();

            Assert.False(queue.TryDequeue(out _, out _));
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_Peek_EmptyCollection()
        {
            var queue = new PriorityQueue<TElement, TPriority>();

            Assert.False(queue.TryPeek(out _, out _));
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Generic_Peek_PositiveCount(int count)
        {
            IReadOnlyCollection<(TElement, TPriority)> itemsToEnqueue = this.GenericIEnumerableFactory(count).ToArray();
            (TElement element, TPriority priority) expectedPeek = itemsToEnqueue.First();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>();

            foreach (var (element, priority) in itemsToEnqueue)
            {
                if (queue.Comparer.Compare(priority, expectedPeek.priority) < 0)
                {
                    expectedPeek = (element, priority);
                }

                queue.Enqueue(element, priority);

                var actualPeekElement = queue.Peek();
                var actualTryPeekSuccess = queue.TryPeek(out TElement actualTryPeekElement, out TPriority actualTryPeekPriority);

                Assert.Equal(expectedPeek.element, actualPeekElement);
                Assert.True(actualTryPeekSuccess);
                Assert.Equal(expectedPeek.element, actualTryPeekElement);
                Assert.Equal(expectedPeek.priority, actualTryPeekPriority);
            }
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void PriorityQueue_Generic_PeekAndDequeue(int initialCapacity, int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity, count, out var generatedItems);

            var expectedPeekPriorities = generatedItems
                .Select(x => x.priority)
                .OrderBy(x => x, queue.Comparer)
                .ToArray();

            for (var i = 0; i < count; ++i)
            {
                var expectedPeekPriority = expectedPeekPriorities[i];

                var actualTryPeekSuccess = queue.TryPeek(out TElement actualTryPeekElement, out TPriority actualTryPeekPriority);
                var actualTryDequeueSuccess = queue.TryDequeue(out TElement actualTryDequeueElement, out TPriority actualTryDequeuePriority);

                Assert.True(actualTryPeekSuccess);
                Assert.True(actualTryDequeueSuccess);
                Assert.Equal(expectedPeekPriority, actualTryPeekPriority);
                Assert.Equal(expectedPeekPriority, actualTryDequeuePriority);
            }

            Assert.Equal(expected: 0, queue.Count);
            Assert.False(queue.TryPeek(out _, out _));
            Assert.False(queue.TryDequeue(out _, out _));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_EnqueueRange_IEnumerable(int count)
        {
            HashSet<(TElement, TPriority)> itemsToEnqueue = this.GenericIEnumerableFactory(count).ToHashSet();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>();

            queue.EnqueueRange(itemsToEnqueue);

            Assert.True(itemsToEnqueue.SetEquals(queue.UnorderedItems));
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_Clear(int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity: 0, count, out _);

            Assert.Equal(count, queue.Count);
            queue.Clear();
            Assert.Equal(expected: 0, queue.Count);
        }

        #endregion

        #region EnsureCapacity, TrimExcess

        [Fact]
        public void PriorityQueue_Generic_EnsureCapacity_Negative()
        {
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => queue.EnsureCapacity(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => queue.EnsureCapacity(int.MinValue));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void PriorityQueue_Generic_TrimExcess_ValidQueueThatHasntBeenRemovedFrom(int initialCapacity, int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity, count, out _);
            queue.TrimExcess();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Generic_TrimExcess_Repeatedly(int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity: 0, count, out _);

            Assert.Equal(count, queue.Count);
            queue.TrimExcess();
            queue.TrimExcess();
            queue.TrimExcess();
            Assert.Equal(count, queue.Count);
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Generic_EnsureCapacityAndTrimExcess(int count)
        {
            IReadOnlyCollection<(TElement, TPriority)> itemsToEnqueue = this.GenericIEnumerableFactory(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>();
            int expectedCount = 0;
            Random random = new Random(Seed: 34);
            int getNextEnsureCapacity() => random.Next(0, count * 2);
            void trimAndEnsureCapacity()
            {
                queue.TrimExcess();

                int capacityAfterEnsureCapacity = queue.EnsureCapacity(getNextEnsureCapacity());
                Assert.Equal(capacityAfterEnsureCapacity, GetUnderlyingBufferCapacity(queue));

                int capacityAfterTrimExcess = (queue.Count < (int)(capacityAfterEnsureCapacity * 0.9)) ? queue.Count : capacityAfterEnsureCapacity;
                queue.TrimExcess();
                Assert.Equal(capacityAfterTrimExcess, GetUnderlyingBufferCapacity(queue));
            };

            foreach (var (element, priority) in itemsToEnqueue)
            {
                trimAndEnsureCapacity();
                queue.Enqueue(element, priority);
                expectedCount++;
                Assert.Equal(expectedCount, queue.Count);
            }

            while (expectedCount > 0)
            {
                queue.Dequeue();
                trimAndEnsureCapacity();
                expectedCount--;
                Assert.Equal(expectedCount, queue.Count);
            }

            trimAndEnsureCapacity();
            Assert.Equal(0, queue.Count);
        }

        private static int GetUnderlyingBufferCapacity(PriorityQueue<TElement, TPriority> queue)
        {
            FieldInfo nodesType = queue.GetType().GetField("_nodes", BindingFlags.NonPublic | BindingFlags.Instance);
            var nodes = ((TElement Element, TPriority Priority)[])nodesType.GetValue(queue);
            return nodes.Length;
        }

        #endregion

        #region Enumeration

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_OrderingIsConsistent(int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity: 0, count, out _);

            (TElement, TPriority)[] firstEnumeration = queue.UnorderedItems.ToArray();
            (TElement, TPriority)[] secondEnumeration = queue.UnorderedItems.ToArray();

            Assert.Equal(firstEnumeration.Length, count);
            Assert.True(firstEnumeration.SequenceEqual(secondEnumeration));
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_InvalidationOnModifiedCollection(int count)
        {
            IReadOnlyCollection<(TElement, TPriority)> itemsToEnqueue = this.GenericIEnumerableFactory(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>();
            queue.EnqueueRange(itemsToEnqueue.Take(count - 1));
            var enumerator = queue.UnorderedItems.GetEnumerator();

            (TElement element, TPriority priority) = itemsToEnqueue.Last();
            queue.Enqueue(element, priority);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_InvalidationOnModifiedCapacity(int count)
        {
            PriorityQueue<TElement, TPriority> queue = this.GenericPriorityQueueFactory(initialCapacity: 0, count, out _);
            var enumerator = queue.UnorderedItems.GetEnumerator();

            int capacityBefore = GetUnderlyingBufferCapacity(queue);
            queue.EnsureCapacity(count * 2 + 4);
            int capacityAfter = GetUnderlyingBufferCapacity(queue);

            Assert.NotEqual(capacityBefore, capacityAfter);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        #endregion
    }
}
