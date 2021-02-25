// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Tests
{
    public class PriorityQueue_NonGeneric_Tests : TestBase
    {
        protected PriorityQueue<string, int> CreateSmallPriorityQueue(out HashSet<(string, int)> items)
        {
            items = new HashSet<(string, int)>
            {
                ("one", 1),
                ("two", 2),
                ("three", 3)
            };
            var queue = new PriorityQueue<string, int>(items);

            return queue;
        }

        protected PriorityQueue<int, int> CreatePriorityQueue(int initialCapacity, int count)
        {
            var pq = new PriorityQueue<int, int>(initialCapacity);
            for (int i = 0; i < count; i++)
            {
                pq.Enqueue(i, i);
            }

            return pq;
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_Empty()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            Assert.Equal("hello", queue.EnqueueDequeue("hello", 42));
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_SmallerThanMin()
        {
            PriorityQueue<string, int> queue = CreateSmallPriorityQueue(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("zero", 0);

            Assert.Equal("zero", actualElement);
            Assert.True(enqueuedItems.SetEquals(queue.UnorderedItems));
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_LargerThanMin()
        {
            PriorityQueue<string, int> queue = CreateSmallPriorityQueue(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("four", 4);

            Assert.Equal("one", actualElement);
            Assert.Equal("two", queue.Dequeue());
            Assert.Equal("three", queue.Dequeue());
            Assert.Equal("four", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueDequeue_EqualToMin()
        {
            PriorityQueue<string, int> queue = CreateSmallPriorityQueue(out HashSet<(string, int)> enqueuedItems);

            string actualElement = queue.EnqueueDequeue("one-not-to-enqueue", 1);

            Assert.Equal("one-not-to-enqueue", actualElement);
            Assert.True(enqueuedItems.SetEquals(queue.UnorderedItems));
        }

        [Fact]
        public void PriorityQueue_Generic_Constructor_IEnumerable_Null()
        {
            (string, int)[] itemsToEnqueue = new(string, int)[] { (null, 0), ("one", 1) } ;
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>(itemsToEnqueue);
            Assert.Null(queue.Dequeue());
            Assert.Equal("one", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_Enqueue_Null()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            queue.Enqueue(element: null, 1);
            queue.Enqueue(element: "zero", 0);
            queue.Enqueue(element: "two", 2);

            Assert.Equal("zero", queue.Dequeue());
            Assert.Null(queue.Dequeue());
            Assert.Equal("two", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Generic_EnqueueRange_Null()
        {
            PriorityQueue<string, int> queue = new PriorityQueue<string, int>();

            queue.EnqueueRange(new string[] { null, null, null }, 0);
            queue.EnqueueRange(new string[] { "not null" }, 1);
            queue.EnqueueRange(new string[] { null, null, null }, 0);

            for (int i = 0; i < 6; ++i)
            {
                Assert.Null(queue.Dequeue());
            }

            Assert.Equal("not null", queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new PriorityQueue<int, int>(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCapacity", () => new PriorityQueue<int, int>(int.MinValue));
        }

        [Fact]
        public void PriorityQueue_Constructor_Enumerable_null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => new PriorityQueue<int, int>(items: null));
            AssertExtensions.Throws<ArgumentNullException>("items", () => new PriorityQueue<int, int>(items: null, comparer: Comparer<int>.Default));
        }

        [Fact]
        public void PriorityQueue_EnqueueRange_null_ThrowsArgumentNullException()
        {
            var queue = new PriorityQueue<int, int>();
            AssertExtensions.Throws<ArgumentNullException>("items", () => queue.EnqueueRange(null));
        }

        [Fact]
        public void PriorityQueue_EmptyCollection_Dequeue_ShouldThrowException()
        {
            var queue = new PriorityQueue<int, int>();

            Assert.Equal(0, queue.Count);
            Assert.False(queue.TryDequeue(out _, out _));
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void PriorityQueue_EmptyCollection_Peek_ShouldReturnFalse()
        {
            var queue = new PriorityQueue<int, int>();

            Assert.False(queue.TryPeek(out _, out _));
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
        }

        #region EnsureCapacity, TrimExcess

        [Fact]
        public void PriorityQueue_EnsureCapacity_Negative_ShouldThrowException()
        {
            PriorityQueue<int, int> queue = new PriorityQueue<int, int>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => queue.EnsureCapacity(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => queue.EnsureCapacity(int.MinValue));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void PriorityQueue_TrimExcess_ShouldNotChangeCount(int initialCapacity, int count)
        {
            PriorityQueue<int, int> queue = CreatePriorityQueue(initialCapacity, count);

            Assert.Equal(count, queue.Count);
            queue.TrimExcess();
            Assert.Equal(count, queue.Count);
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_TrimExcess_Repeatedly_ShouldNotChangeCount(int count)
        {
            PriorityQueue<int, int> queue = CreatePriorityQueue(initialCapacity: count, count);

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
            IReadOnlyCollection<(int, int)> itemsToEnqueue = Enumerable.Range(1, count).Select(i => (i, i)).ToArray();
            var queue = new PriorityQueue<int, int>();
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

            foreach ((int element, int priority) in itemsToEnqueue)
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

        private static int GetUnderlyingBufferCapacity<TPriority, TElement>(PriorityQueue<TPriority, TElement> queue)
        {
            FieldInfo nodesField = queue.GetType().GetField("_nodes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(nodesField);
            var nodes = ((TElement Element, TPriority Priority)[])nodesField.GetValue(queue);
            return nodes.Length;
        }

        #endregion

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_InvalidateOnModifiedCollection(int count)
        {
            IReadOnlyCollection<(int, int)> itemsToEnqueue = Enumerable.Range(1, count).Select(i => (i, i)).ToArray();
            PriorityQueue<int, int> queue = new PriorityQueue<int, int>();
            queue.EnqueueRange(itemsToEnqueue.Take(count - 1));
            var enumerator = queue.UnorderedItems.GetEnumerator();

            (int element, int priority) = itemsToEnqueue.Last();
            queue.Enqueue(element, priority);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        #region Enumeration

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_InvalidationOnModifiedCapacity(int count)
        {
            PriorityQueue<int, int> queue = CreatePriorityQueue(initialCapacity: 0, count);
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
