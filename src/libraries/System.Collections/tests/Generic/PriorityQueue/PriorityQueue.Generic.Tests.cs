// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public abstract class PriorityQueue_Generic_Tests<TElement, TPriority> : IGenericSharedAPI_Tests<(TElement Element, TPriority Priority)>
    {
        #region PriorityQueue Helper methods
        protected virtual IComparer<TPriority>? GetPriorityComparer() => Comparer<TPriority>.Default;

        protected IEnumerable<(TElement, TPriority)> CreateItems(int count)
        {
            const int MagicValue = 34;
            int seed = count * MagicValue;
            for (int i = 0; i < count; i++)
            {
                yield return CreateT(seed++);
            }
        }

        protected PriorityQueue<TElement, TPriority> CreateEmptyPriorityQueue(int initialCapacity = 0)
            => new PriorityQueue<TElement, TPriority>(initialCapacity, GetPriorityComparer());

        protected PriorityQueue<TElement, TPriority> CreatePriorityQueue(
            int initialCapacity, int countOfItemsToGenerate, out List<(TElement element, TPriority priority)> generatedItems)
        {
            generatedItems = CreateItems(countOfItemsToGenerate).ToList();
            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity, GetPriorityComparer());
            queue.EnqueueRange(generatedItems);
            return queue;
        }

        #endregion

        #region Constructors

        [Fact]
        public void PriorityQueue_DefaultConstructor_ComparerEqualsDefaultComparer()
        {
            var queue = new PriorityQueue<TElement, TPriority>();

            Assert.Equal(expected: 0, queue.Count);
            Assert.Empty(queue.UnorderedItems);
            Assert.Equal(queue.Comparer, Comparer<TPriority>.Default);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_EmptyCollection_UnorderedItemsIsEmpty(int initialCapacity)
        {
            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity);
            Assert.Empty(queue.UnorderedItems);
        }

        [Fact]
        public void PriorityQueue_ComparerConstructor_ComparerShouldEqualParameter()
        {
            IComparer<TPriority> comparer = GetPriorityComparer();
            var queue = new PriorityQueue<TElement, TPriority>(comparer);
            Assert.Equal(comparer, queue.Comparer);
        }

        [Fact]
        public void PriorityQueue_ComparerConstructorNull_ComparerShouldEqualDefaultComparer()
        {
            var queue = new PriorityQueue<TElement, TPriority>(comparer: null);
            Assert.Equal(0, queue.Count);
            Assert.Same(Comparer<TPriority>.Default, queue.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_CapacityConstructor_ComparerShouldEqualDefaultComparer(int initialCapacity)
        {
            var queue = new PriorityQueue<TElement, TPriority>(initialCapacity);
            Assert.Empty(queue.UnorderedItems);
            Assert.Same(Comparer<TPriority>.Default, queue.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_EnumerableConstructor_ShouldContainAllElements(int count)
        {
            (TElement, TPriority)[] itemsToEnqueue = CreateItems(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = new PriorityQueue<TElement, TPriority>(itemsToEnqueue, GetPriorityComparer());
            Assert.Equal(itemsToEnqueue.Length, queue.Count);
            AssertExtensions.CollectionEqual(itemsToEnqueue, queue.UnorderedItems, EqualityComparer<(TElement, TPriority)>.Default);
        }

        #endregion

        #region Enqueue, Dequeue, Peek, EnqueueDequeue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Enqueue_IEnumerable(int count)
        {
            (TElement, TPriority)[] itemsToEnqueue = CreateItems(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = CreateEmptyPriorityQueue();

            foreach ((TElement element, TPriority priority) in itemsToEnqueue)
            {
                queue.Enqueue(element, priority);
            }

            AssertExtensions.CollectionEqual(itemsToEnqueue, queue.UnorderedItems, EqualityComparer<(TElement, TPriority)>.Default);
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Peek_ShouldReturnMinimalElement(int count)
        {
            IReadOnlyCollection<(TElement, TPriority)> itemsToEnqueue = CreateItems(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = CreateEmptyPriorityQueue();
            (TElement Element, TPriority Priority) minItem = itemsToEnqueue.First();

            foreach ((TElement element, TPriority priority) in itemsToEnqueue)
            {
                if (queue.Comparer.Compare(priority, minItem.Priority) < 0)
                {
                    minItem = (element, priority);
                }

                queue.Enqueue(element, priority);

                TElement actualPeekElement = queue.Peek();
                Assert.Equal(minItem.Element, actualPeekElement);

                bool actualTryPeekSuccess = queue.TryPeek(out TElement actualTryPeekElement, out TPriority actualTryPeekPriority);
                Assert.True(actualTryPeekSuccess);
                Assert.Equal(minItem.Element, actualTryPeekElement);
                Assert.Equal(minItem.Priority, actualTryPeekPriority);
            }
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(1, 1)]
        [InlineData(3, 100)]
        public void PriorityQueue_PeekAndDequeue(int initialCapacity, int count)
        {
            PriorityQueue<TElement, TPriority> queue = CreatePriorityQueue(initialCapacity, count, out List<(TElement element, TPriority priority)> generatedItems);

            TPriority[] expectedPeekPriorities = generatedItems
                .Select(x => x.priority)
                .OrderBy(x => x, queue.Comparer)
                .ToArray();

            for (int i = 0; i < count; ++i)
            {
                TPriority expectedPeekPriority = expectedPeekPriorities[i];

                bool actualTryPeekSuccess = queue.TryPeek(out TElement actualTryPeekElement, out TPriority actualTryPeekPriority);
                bool actualTryDequeueSuccess = queue.TryDequeue(out TElement actualTryDequeueElement, out TPriority actualTryDequeuePriority);

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
        public void PriorityQueue_EnqueueRange_IEnumerable(int count)
        {
            (TElement, TPriority)[] itemsToEnqueue = CreateItems(count).ToArray();
            PriorityQueue<TElement, TPriority> queue = CreateEmptyPriorityQueue();

            queue.EnqueueRange(itemsToEnqueue);

            AssertExtensions.CollectionEqual(itemsToEnqueue, queue.UnorderedItems, EqualityComparer<(TElement, TPriority)>.Default);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_EnqueueDequeue(int count)
        {
            (TElement Element, TPriority Priority)[] itemsToEnqueue = CreateItems(2 * count).ToArray();
            PriorityQueue<TElement, TPriority> queue = CreateEmptyPriorityQueue();
            queue.EnqueueRange(itemsToEnqueue.Take(count));

            foreach ((TElement element, TPriority priority) in itemsToEnqueue.Skip(count))
            {
                queue.EnqueueDequeue(element, priority);
            }

            IEnumerable<(TElement Element, TPriority Priority)> expectedItems = itemsToEnqueue.OrderByDescending(x => x.Priority, queue.Comparer).Take(count);
            AssertExtensions.CollectionEqual(expectedItems, queue.UnorderedItems, EqualityComparer<(TElement, TPriority)>.Default);
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void PriorityQueue_Clear(int count)
        {
            PriorityQueue<TElement, TPriority> queue = CreatePriorityQueue(initialCapacity: 0, count, out _);
            Assert.Equal(count, queue.Count);

            queue.Clear();

            Assert.Equal(expected: 0, queue.Count);
            Assert.False(queue.TryPeek(out _, out _));
        }

        #endregion

        #region Enumeration

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void PriorityQueue_Enumeration_OrderingIsConsistent(int count)
        {
            PriorityQueue<TElement, TPriority> queue = CreatePriorityQueue(initialCapacity: 0, count, out _);

            (TElement, TPriority)[] firstEnumeration = queue.UnorderedItems.ToArray();
            (TElement, TPriority)[] secondEnumeration = queue.UnorderedItems.ToArray();

            Assert.Equal(firstEnumeration.Length, count);
            Assert.True(firstEnumeration.SequenceEqual(secondEnumeration));
        }

        #endregion

        #region IGenericSharedAPI<T> Helper Methods

        /// <summary>
        /// <see cref="IGenericSharedAPI_Tests{T}"/> requires collections that implement IEnumerable.
        /// Since PriorityQueue does not we use a subclass that delegates to <see cref="PriorityQueue{TElement, TPriority}.UnorderedItems"/>.
        /// </summary>
        protected class EnumerablePriorityQueue : PriorityQueue<TElement, TPriority>, IEnumerable<(TElement Element, TPriority Priority)>
        {
            public EnumerablePriorityQueue(IComparer<TPriority>? comparer) : base(comparer)
            {
            }

            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => UnorderedItems.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => UnorderedItems.GetEnumerator();
        }

        protected override IEnumerable<(TElement Element, TPriority Priority)> GenericIEnumerableFactory() => new EnumerablePriorityQueue(GetPriorityComparer());
        protected override int Count(IEnumerable<(TElement Element, TPriority Priority)> enumerable) => ((EnumerablePriorityQueue)enumerable).Count;
        protected override void Add(IEnumerable<(TElement Element, TPriority Priority)> enumerable, (TElement Element, TPriority Priority) value) => ((EnumerablePriorityQueue)enumerable).Enqueue(value.Element, value.Priority);
        protected override void Clear(IEnumerable<(TElement Element, TPriority Priority)> enumerable) => ((EnumerablePriorityQueue)enumerable).Clear();
        protected override bool Contains(IEnumerable<(TElement Element, TPriority Priority)> enumerable, (TElement Element, TPriority Priority) value) => ((EnumerablePriorityQueue)enumerable).Any(elem => elem.Equals(value));
        protected override void CopyTo(IEnumerable<(TElement Element, TPriority Priority)> enumerable, (TElement Element, TPriority Priority)[] array, int index) => ((ICollection)((EnumerablePriorityQueue)enumerable).UnorderedItems).CopyTo(array, index);
        protected override bool Remove(IEnumerable<(TElement Element, TPriority Priority)> enumerable) => ((EnumerablePriorityQueue)enumerable).TryDequeue(out _, out _);
        protected override Type IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        #endregion
    }
}
