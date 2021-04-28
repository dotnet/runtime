// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the Queue class.
    /// </summary>
    public abstract class Queue_Generic_Tests<T> : IGenericSharedAPI_Tests<T>
    {
        #region Queue<T> Helper Methods

        protected Queue<T> GenericQueueFactory()
        {
            return new Queue<T>();
        }

        protected Queue<T> GenericQueueFactory(int count)
        {
            Queue<T> queue = new Queue<T>(count);
            int seed = count * 34;
            for (int i = 0; i < count; i++)
                queue.Enqueue(CreateT(seed++));
            return queue;
        }

        #endregion

        #region IGenericSharedAPI<T> Helper Methods

        protected override IEnumerable<T> GenericIEnumerableFactory()
        {
            return GenericQueueFactory();
        }

        protected override IEnumerable<T> GenericIEnumerableFactory(int count)
        {
            return GenericQueueFactory(count);
        }

        protected override int Count(IEnumerable<T> enumerable) => ((Queue<T>)enumerable).Count;
        protected override void Add(IEnumerable<T> enumerable, T value) => ((Queue<T>)enumerable).Enqueue(value);
        protected override void Clear(IEnumerable<T> enumerable) => ((Queue<T>)enumerable).Clear();
        protected override bool Contains(IEnumerable<T> enumerable, T value) => ((Queue<T>)enumerable).Contains(value);
        protected override void CopyTo(IEnumerable<T> enumerable, T[] array, int index) => ((Queue<T>)enumerable).CopyTo(array, index);
        protected override bool Remove(IEnumerable<T> enumerable) => ((Queue<T>)enumerable).TryDequeue(out _);
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override Type IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        #endregion

        #region Constructor_IEnumerable

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void Queue_Generic_Constructor_IEnumerable(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            _ = setLength;
            _ = numberOfMatchingElements;
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, numberOfDuplicateElements);
            Queue<T> queue = new Queue<T>(enumerable);
            Assert.Equal(enumerable, queue);
        }

        [Fact]
        public void Queue_Generic_Constructor_IEnumerable_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("collection", () => new Queue<T>(null));
        }

        #endregion

        #region Constructor_Capacity

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_Constructor_int(int count)
        {
            Queue<T> queue = new Queue<T>(count);
            Assert.Equal(Array.Empty<T>(), queue.ToArray());
            queue.Clear();
            Assert.Equal(Array.Empty<T>(), queue.ToArray());
        }

        [Fact]
        public void Queue_Generic_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Queue<T>(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new Queue<T>(int.MinValue));
        }

        #endregion

        #region Dequeue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_Dequeue_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
                Assert.Equal(element, queue.Dequeue());
        }

        [Fact]
        public void Queue_Generic_Dequeue_OnEmptyQueue_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => new Queue<T>().Dequeue());
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void Queue_Generic_EnqueueAndDequeue(int capacity, int items)
        {
            int seed = 53134;
            var q = new Queue<T>(capacity);
            Assert.Equal(0, q.Count);

            // Enqueue some values and make sure the count is correct
            List<T> source = (List<T>)CreateEnumerable(EnumerableType.List, null, items, 0, 0);
            foreach (T val in source)
            {
                q.Enqueue(val);
            }
            Assert.Equal(source, q);

            // Dequeue to make sure the values are removed in the right order and the count is updated
            for (int i = 0; i < items; i++)
            {
                T itemToRemove = source[0];
                source.RemoveAt(0);
                Assert.Equal(itemToRemove, q.Dequeue());
                Assert.Equal(items - i - 1, q.Count);
            }

            // Can't dequeue when empty
            Assert.Throws<InvalidOperationException>(() => q.Dequeue());

            // But can still be used after a failure and after bouncing at empty
            T itemToAdd = CreateT(seed++);
            q.Enqueue(itemToAdd);
            Assert.Equal(itemToAdd, q.Dequeue());
        }

        #endregion

        #region ToArray

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_ToArray(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            Assert.True(queue.ToArray().SequenceEqual(queue.ToArray<T>()));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_ToArray_NonWrappedQueue(int count)
        {
            Queue<T> collection = new Queue<T>(count + 1);
            AddToCollection(collection, count);
            T[] elements = collection.ToArray();
            elements.Reverse();
            Assert.True(Enumerable.SequenceEqual(elements, collection.ToArray<T>()));
        }

        #endregion

        #region Peek

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_Peek_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
            {
                Assert.Equal(element, queue.Peek());
                queue.Dequeue();
            }
        }

        [Fact]
        public void Queue_Generic_Peek_OnEmptyQueue_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => new Queue<T>().Peek());
        }

        #endregion

        #region TrimExcess

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TrimExcess_OnValidQueueThatHasntBeenRemovedFrom(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            queue.TrimExcess();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TrimExcess_Repeatedly(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> expected = queue.ToList();
            queue.TrimExcess();
            queue.TrimExcess();
            queue.TrimExcess();
            Assert.True(queue.SequenceEqual(expected));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TrimExcess_AfterRemovingOneElement(int count)
        {
            if (count > 0)
            {
                Queue<T> queue = GenericQueueFactory(count);
                List<T> expected = queue.ToList();
                queue.TrimExcess();
                T removed = queue.Dequeue();
                expected.Remove(removed);
                queue.TrimExcess();

                Assert.True(queue.SequenceEqual(expected));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TrimExcess_AfterClearingAndAddingSomeElementsBack(int count)
        {
            if (count > 0)
            {
                Queue<T> queue = GenericQueueFactory(count);
                queue.TrimExcess();
                queue.Clear();
                queue.TrimExcess();
                Assert.Equal(0, queue.Count);

                AddToCollection(queue, count / 10);
                queue.TrimExcess();
                Assert.Equal(count / 10, queue.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TrimExcess_AfterClearingAndAddingAllElementsBack(int count)
        {
            if (count > 0)
            {
                Queue<T> queue = GenericQueueFactory(count);
                queue.TrimExcess();
                queue.Clear();
                queue.TrimExcess();
                Assert.Equal(0, queue.Count);

                AddToCollection(queue, count);
                queue.TrimExcess();
                Assert.Equal(count, queue.Count);
            }
        }

        #endregion

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TryDequeue_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(queue.TryDequeue(out result));
                Assert.Equal(element, result);
            }
        }

        [Fact]
        public void Queue_Generic_TryDequeue_EmptyQueue_ReturnsFalse()
        {
            T result;
            Assert.False(new Queue<T>().TryDequeue(out result));
            Assert.Equal(default(T), result);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_TryPeek_AllElements(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            List<T> elements = queue.ToList();
            foreach (T element in elements)
            {
                T result;
                Assert.True(queue.TryPeek(out result));
                Assert.Equal(element, result);

                queue.Dequeue();
            }
        }

        [Fact]
        public void Queue_Generic_TryPeek_EmptyQueue_ReturnsFalse()
        {
            T result;
            Assert.False(new Queue<T>().TryPeek(out result));
            Assert.Equal(default(T), result);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_EnsureCapacity_RequestingLargerCapacity_DoesInvalidateEnumeration(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            IEnumerator<T> copiedEnumerator = new List<T>(queue).GetEnumerator();
            IEnumerator<T> enumerator = queue.GetEnumerator();

            queue.EnsureCapacity(count + 1);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Queue_Generic_EnsureCapacity_NotInitialized_RequestedZero_ReturnsZero()
        {
            var queue = GenericQueueFactory();
            Assert.Equal(0, queue.EnsureCapacity(0));
        }

        [Fact]
        public void Queue_Generic_EnsureCapacity_NegativeCapacityRequested_Throws()
        {
            var queue = GenericQueueFactory();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => queue.EnsureCapacity(-1));
        }

        public static IEnumerable<object[]> Queue_Generic_EnsureCapacity_LargeCapacityRequested_Throws_MemberData()
        {
            yield return new object[] { Array.MaxLength + 1 };
            yield return new object[] { int.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Queue_Generic_EnsureCapacity_LargeCapacityRequested_Throws_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51411", TestRuntimes.Mono)]
        public void Queue_Generic_EnsureCapacity_LargeCapacityRequested_Throws(int requestedCapacity)
        {
            var queue = GenericQueueFactory();
            AssertExtensions.Throws<OutOfMemoryException>(() => queue.EnsureCapacity(requestedCapacity));
        }

        [Theory]
        [InlineData(5)]
        public void Queue_Generic_EnsureCapacity_RequestedCapacitySmallerThanOrEqualToCurrent_CapacityUnchanged(int currentCapacity)
        {
            var queue = new Queue<T>(currentCapacity);

            for (int requestCapacity = 0; requestCapacity <= currentCapacity; requestCapacity++)
            {
                Assert.Equal(currentCapacity, queue.EnsureCapacity(requestCapacity));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_EnsureCapacity_RequestedCapacitySmallerThanOrEqualToCount_CapacityUnchanged(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);

            for (int requestCapacity = 0; requestCapacity <= count; requestCapacity++)
            {
                Assert.Equal(count, queue.EnsureCapacity(requestCapacity));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void Queue_Generic_EnsureCapacity_CapacityIsAtLeastTheRequested(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);

            int requestCapacity = count + 1;
            int newCapacity = queue.EnsureCapacity(requestCapacity);
            Assert.InRange(newCapacity, requestCapacity, int.MaxValue);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Queue_Generic_EnsureCapacity_RequestingLargerCapacity_DoesNotImpactQueueContent(int count)
        {
            Queue<T> queue = GenericQueueFactory(count);
            var copiedList = new List<T>(queue);

            queue.EnsureCapacity(count + 1);
            Assert.Equal(copiedList, queue);

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(copiedList[i], queue.Dequeue());
            }
        }
    }
}
