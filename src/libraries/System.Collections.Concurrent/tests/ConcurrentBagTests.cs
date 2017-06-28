// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public partial class ConcurrentBagTests : ProducerConsumerCollectionTests
    {
        protected override IProducerConsumerCollection<T> CreateProducerConsumerCollection<T>() => new ConcurrentBag<T>();
        protected override IProducerConsumerCollection<int> CreateProducerConsumerCollection(IEnumerable<int> collection) => new ConcurrentBag<int>(collection);
        protected override bool IsEmpty(IProducerConsumerCollection<int> pcc) => ((ConcurrentBag<int>)pcc).IsEmpty;
        protected override bool TryPeek<T>(IProducerConsumerCollection<T> pcc, out T result) => ((ConcurrentBag<T>)pcc).TryPeek(out result);
        protected override IProducerConsumerCollection<int> CreateOracle(IEnumerable<int> collection) => new BagOracle(collection);

        protected override string CopyToNoLengthParamName => "index";

        [Theory]
        [InlineData(1, 10)]
        [InlineData(3, 100)]
        [InlineData(8, 1000)]
        public static void AddThenPeek_LatestLocalItemReturned(int threadsCount, int itemsPerThread)
        {
            var bag = new ConcurrentBag<int>();

            using (var b = new Barrier(threadsCount))
            {
                WaitAllOrAnyFailed((Enumerable.Range(0, threadsCount).Select(_ => Task.Factory.StartNew(() =>
                {
                    b.SignalAndWait();
                    for (int i = 1; i < itemsPerThread + 1; i++)
                    {
                        bag.Add(i);
                        int item;
                        Assert.True(bag.TryPeek(out item)); // ordering implementation detail that's not guaranteed
                        Assert.Equal(i, item);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default))).ToArray());
            }

            Assert.Equal(itemsPerThread * threadsCount, bag.Count);
        }

        [Fact]
        public static void AddOnOneThread_PeekOnAnother_EnsureWeCanTakeOnTheOriginal()
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(1, 5));

            Task.Factory.StartNew(() =>
            {
                int item;
                for (int i = 1; i <= 5; i++)
                {
                    Assert.True(bag.TryPeek(out item));
                    Assert.Equal(1, item);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();

            Assert.Equal(5, bag.Count);

            for (int i = 5; i > 0; i--)
            {
                int item;

                Assert.True(bag.TryPeek(out item));
                Assert.Equal(i, item); // ordering implementation detail that's not guaranteed

                Assert.Equal(i, bag.Count);
                Assert.True(bag.TryTake(out item));
                Assert.Equal(i - 1, bag.Count);
                Assert.Equal(i, item); // ordering implementation detail that's not guaranteed
            }
        }

        [Fact]
        public static void AddManyItems_ThenTakeOnDifferentThread_ItemsOutputInExpectedOrder()
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, 100000));
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 100000; i++)
                {
                    int item;
                    Assert.True(bag.TryTake(out item));
                    Assert.Equal(i, item); // Testing an implementation detail rather than guaranteed ordering
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(33)]
        public static void IterativelyAddOnOneThreadThenTakeOnAnother_OrderMaintained(int initialCount)
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, initialCount));

            const int Iterations = 100;
            using (AutoResetEvent itemConsumed = new AutoResetEvent(false), itemProduced = new AutoResetEvent(false))
            {
                Task t = Task.Run(() =>
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        itemProduced.WaitOne();
                        int item;
                        Assert.True(bag.TryTake(out item));
                        Assert.Equal(i, item); // Testing an implementation detail rather than guaranteed ordering
                        itemConsumed.Set();
                    }
                });

                for (int i = initialCount; i < Iterations + initialCount; i++)
                {
                    bag.Add(i);
                    itemProduced.Set();
                    itemConsumed.WaitOne();
                }

                t.GetAwaiter().GetResult();
            }

            Assert.Equal(initialCount, bag.Count);
        }

        [Fact]
        public static void CopyTo_TypeMismatch()
        {
            const int Size = 10;

            var c = new ConcurrentBag<Exception>(Enumerable.Range(0, Size).Select(_ => new Exception()));
            c.CopyTo(new Exception[Size], 0);
            Assert.Throws<InvalidCastException>(() => c.CopyTo(new InvalidOperationException[Size], 0));
        }

        [Fact]
        public static void ICollectionCopyTo_TypeMismatch()
        {
            const int Size = 10;
            ICollection c;

            c = new ConcurrentBag<Exception>(Enumerable.Range(0, Size).Select(_ => new Exception()));
            c.CopyTo(new Exception[Size], 0);
            Assert.Throws<InvalidCastException>(() => c.CopyTo(new InvalidOperationException[Size], 0));

            c = new ConcurrentBag<ArgumentException>(Enumerable.Range(0, Size).Select(_ => new ArgumentException()));
            c.CopyTo(new Exception[Size], 0);
            c.CopyTo(new ArgumentException[Size], 0);
            Assert.Throws<InvalidCastException>(() => c.CopyTo(new ArgumentNullException[Size], 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public static void ToArray_AddTakeDifferentThreads_ExpectedResultsAfterAddsAndTakes(int initialCount)
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, initialCount));
            int items = 20 + initialCount;

            for (int i = 0; i < items; i++)
            {
                bag.Add(i + initialCount);
                ThreadFactory.StartNew(() =>
                {
                    int item;
                    Assert.True(bag.TryTake(out item));
                    Assert.Equal(item, i);
                }).GetAwaiter().GetResult();
                Assert.Equal(Enumerable.Range(i + 1, initialCount).Reverse(), bag.ToArray());
            }
        }

        protected sealed class BagOracle : IProducerConsumerCollection<int>
        {
            private readonly Stack<int> _stack;
            public BagOracle(IEnumerable<int> collection) { _stack = new Stack<int>(collection); }
            public int Count => _stack.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => null;
            public void CopyTo(Array array, int index) => _stack.ToArray().CopyTo(array, index);
            public void CopyTo(int[] array, int index) => _stack.ToArray().CopyTo(array, index);
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
