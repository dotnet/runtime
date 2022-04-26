// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentStackTests : ProducerConsumerCollectionTests
    {
        protected override IProducerConsumerCollection<T> CreateProducerConsumerCollection<T>() => new ConcurrentStack<T>();
        protected override IProducerConsumerCollection<int> CreateProducerConsumerCollection(IEnumerable<int> collection) => new ConcurrentStack<int>(collection);
        protected override bool IsEmpty(IProducerConsumerCollection<int> pcc) => ((ConcurrentStack<int>)pcc).IsEmpty;
        protected override bool TryPeek<T>(IProducerConsumerCollection<T> pcc, out T result) => ((ConcurrentStack<T>)pcc).TryPeek(out result);
        protected override IProducerConsumerCollection<int> CreateOracle(IEnumerable<int> collection) => new StackOracle(collection);
        protected override bool ResetImplemented => false;

        [Fact]
        public void IsEmpty_TrueWhenEmpty_FalseWhenNot()
        {
            var s = new ConcurrentStack<int>();
            int item;
            Assert.False(s.TryPop(out item));
            Assert.False(s.TryPeek(out item));
            Assert.Equal(0, s.TryPopRange(new int[1]));

            int count = 15;
            for (int i = 0; i < count; i++)
            {
                s.Push(i);
            }

            Assert.Equal(0, s.TryPopRange(new int[1], 0, 0));
            Assert.Equal(count, s.Count);
            Assert.False(s.IsEmpty);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(16)]
        [InlineData(1024)]
        public void Clear_AllElementsRemoved(int count)
        {
            var s = new ConcurrentStack<int>(Enumerable.Range(0, count));
            Assert.Equal(count, s.Count);

            s.Clear();

            Assert.True(s.IsEmpty);
            Assert.Equal(0, s.Count);
        }

        [Fact]
        public void PushRange_NoItems_NothingAdded()
        {
            var s = new ConcurrentStack<int>();
            Assert.True(s.IsEmpty);

            s.PushRange(new int[1], 0, 0);
            Assert.True(s.IsEmpty);
        }

        [Fact]
        public void PushRange_NoItems_NothingAdded_EmptyArrayWithRangeSpecified()
        {
            var s = new ConcurrentStack<int>();
            Assert.True(s.IsEmpty);

            s.PushRange(new int[0], 0, 0);
            Assert.True(s.IsEmpty);
        }

        [Fact]
        public void PushRange_NoItems_NothingAdded_NonEmptyArrayWithZeroCountSpecified()
        {
            var s = new ConcurrentStack<int>();
            Assert.True(s.IsEmpty);

            int[] arr = new int[2];
            s.PushRange(arr, arr.Length, 0);
            Assert.True(s.IsEmpty);
        }

        [Fact]
        public void PushRange_NoItems_NothingAdded_EmptyArrayNoRangeSpecified()
        {
            var s = new ConcurrentStack<int>();
            Assert.True(s.IsEmpty);

            s.PushRange(new int[0]);
            Assert.True(s.IsEmpty);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(8, 10)]
        [InlineData(16, 100)]
        [InlineData(128, 100)]
        public void PushRange_Concurrent_ConsecutiveItemsInEachRange(int numThreads, int numItemsPerThread)
        {
            var stack = new ConcurrentStack<int>();

            Task.WaitAll(Enumerable.Range(0, numThreads).Select(i => Task.Factory.StartNew((obj) =>
            {
                int index = (int)obj;
                int[] array = new int[numItemsPerThread];
                for (int j = 0; j < numItemsPerThread; j++)
                {
                    array[j] = index + j;
                }

                stack.PushRange(array);
            }, i * numItemsPerThread, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)).ToArray());

            //validation
            for (int i = 0; i < numThreads; i++)
            {
                int lastItem = -1;
                for (int j = 0; j < numItemsPerThread; j++)
                {
                    int currentItem = 0;
                    Assert.True(stack.TryPop(out currentItem));
                    Assert.True((lastItem <= -1) || lastItem - currentItem == 1);
                    lastItem = currentItem;
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(8, 10)]
        [InlineData(16, 100)]
        [InlineData(128, 100)]
        public void TryPopRange_Concurrent_PoppedItemsAreConsecutive(int numThreads, int numElementsPerThread)
        {
            int numTotalElements = numThreads * numElementsPerThread;
            var allValues = new List<int>(Enumerable.Range(1, numTotalElements));
            var stack = new ConcurrentStack<int>(allValues);

            int[] array = new int[numTotalElements];
            Task.WaitAll(Enumerable.Range(0, numThreads).Select(i => Task.Factory.StartNew(obj =>
            {
                int index = (int)obj;
                int res = stack.TryPopRange(array, index, numElementsPerThread);
                Assert.Equal(numElementsPerThread, res);
            }, i * numElementsPerThread, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

            for (int i = 0; i < numThreads; i++)
            {
                for (int j = 1; j < numElementsPerThread; j++)
                {
                    int currentIndex = i * numElementsPerThread + j;
                    Assert.Equal(array[currentIndex - 1], array[currentIndex] + 1);
                }
            }
        }

        [Fact]
        public void PushRange_InvalidArguments_Throws()
        {
            var stack = new ConcurrentStack<int>();
            Assert.Throws<ArgumentNullException>(() => stack.PushRange(null));
            Assert.Throws<ArgumentNullException>(() => stack.PushRange(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.PushRange(new int[1], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.PushRange(new int[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.PushRange(new int[1], 2, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => stack.PushRange(new int[0], 0, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => stack.PushRange(new int[1], 0, 10));
        }

        [Fact]
        public void TryPopRange_InvalidArguments_Throws()
        {
            var stack = new ConcurrentStack<int>();
            Assert.Throws<ArgumentNullException>(() => stack.TryPopRange(null));
            Assert.Throws<ArgumentNullException>(() => stack.TryPopRange(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.TryPopRange(new int[1], 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.TryPopRange(new int[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => stack.TryPopRange(new int[1], 2, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => stack.TryPopRange(new int[1], 0, 10));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Concurrent_Push_TryPop_WithSuspensions()
        {
            int items = 10;
            var q = new ConcurrentStack<int>();

            // Consumer dequeues items until it sees last pushed item regardless of order
            Task consumer = Task.Run(() =>
            {
                while (!q.TryPop(out var item) || item != items) ;
            });

            // Producer queues the expected number of items
            Task producer = Task.Run(() =>
            {
                for (int i = 1; i <= items; i++)
                {
                    q.Push(i);

                    // Triggers EE suspensions. We should still make quick progress.
                    // See https://github.com/dotnet/runtime/issues/67559 for kinds of issues we look for.
                    GC.GetTotalAllocatedBytes(precise: true);
                }
            });

            Task.WaitAll(producer, consumer);
        }

        protected sealed class StackOracle : IProducerConsumerCollection<int>
        {
            private readonly Stack<int> _stack;
            public StackOracle(IEnumerable<int> collection) { _stack = new Stack<int>(collection); }
            public int Count => _stack.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => null;
            public void CopyTo(Array array, int index) => ((ICollection)_stack).CopyTo(array, index);
            public void CopyTo(int[] array, int index) => _stack.CopyTo(array, index);
            public IEnumerator<int> GetEnumerator() => _stack.GetEnumerator();
            public int[] ToArray() => _stack.ToArray();
            public bool TryAdd(int item) { _stack.Push(item); return true; }
            public bool TryTake(out int item)
            {
                if (_stack.Count > 0)
                {
                    item = _stack.Pop();
                    return true;
                }
                else
                {
                    item = 0;
                    return false;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
