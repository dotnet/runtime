// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentBagTests : ProducerConsumerCollectionTests
    {
        protected override IProducerConsumerCollection<T> CreateProducerConsumerCollection<T>() => new ConcurrentBag<T>();
        protected override IProducerConsumerCollection<int> CreateProducerConsumerCollection(IEnumerable<int> collection) => new ConcurrentBag<int>(collection);
        protected override bool IsEmpty(IProducerConsumerCollection<int> pcc) => ((ConcurrentBag<int>)pcc).IsEmpty;
        protected override bool TryPeek<T>(IProducerConsumerCollection<T> pcc, out T result) => ((ConcurrentBag<T>)pcc).TryPeek(out result);
        protected override IProducerConsumerCollection<int> CreateOracle(IEnumerable<int> collection) => new BagOracle(collection);

        protected override string CopyToNoLengthParamName => "index";

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public static void SingleProducerAdding_MultiConsumerTaking_SemaphoreThrottling_AllTakesSucceed()
        {
            var bag = new ConcurrentBag<int>();
            var s = new SemaphoreSlim(0);
            CountdownEvent ce = null;
            const int ItemCount = 200_000;

            int producerNextValue = 0;
            Action producer = null;
            producer = delegate
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    bag.Add(producerNextValue++);
                    s.Release();
                    if (producerNextValue < ItemCount)
                    {
                        producer();
                    }
                    else
                    {
                        ce.Signal();
                    }
                });
            };

            int consumed = 0;
            Action consumer = null;
            consumer = delegate
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    if (s.Wait(0))
                    {
                        Assert.True(bag.TryTake(out _), "There's an item available, but we couldn't take it.");
                        Interlocked.Increment(ref consumed);
                    }
                    else if (Volatile.Read(ref consumed) >= ItemCount)
                    {
                        ce.Signal();
                        return;
                    }

                    consumer();
                });
            };

            // one producer, two consumers
            ce = new CountdownEvent(3);
            producer();
            consumer();
            consumer();
            ce.Wait();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 20)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        [InlineData(true, 20)]
        public static void Clear_AddItemsToThisAndOtherThreads_EmptyAfterClear(bool addToLocalThread, int otherThreads)
        {
            var bag = new ConcurrentBag<int>();

            const int ItemsPerThread = 100;

            for (int repeat = 0; repeat < 2; repeat++)
            {
                // If desired, add items on other threads
                if (addToLocalThread)
                {
                    for (int i = 0; i < ItemsPerThread; i++) bag.Add(i);
                }

                // If desired, add items on other threads
                int origThreadId = Environment.CurrentManagedThreadId;
                Task.WaitAll((from _ in Enumerable.Range(0, otherThreads)
                              select Task.Factory.StartNew(() =>
                              {
                                  Assert.NotEqual(origThreadId, Environment.CurrentManagedThreadId);
                                  for (int i = 0; i < ItemsPerThread; i++) bag.Add(i);
                              }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());

                // Make sure we got the expected number of items, then clear, and make sure it's empty
                Assert.Equal((ItemsPerThread * otherThreads) + (addToLocalThread ? ItemsPerThread : 0), bag.Count);
                bag.Clear();
                Assert.Equal(0, bag.Count);
            }
        }

        [Fact]
        public static void Clear_DuringEnumeration_DoesntAffectEnumeration()
        {
            const int ExpectedCount = 100;
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, ExpectedCount));
            using (IEnumerator<int> e = bag.GetEnumerator())
            {
                bag.Clear();
                int count = 0;
                while (e.MoveNext()) count++;
                Assert.Equal(ExpectedCount, count);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [InlineData(1, 10)]
        [InlineData(3, 100)]
        [InlineData(8, 1000)]
        public static void Clear_ConcurrentUsage_NoExceptions(int threadsCount, int itemsPerThread)
        {
            var bag = new ConcurrentBag<int>();
            Task.WaitAll((from i in Enumerable.Range(0, threadsCount) select Task.Factory.StartNew(() =>
            {
                var random = new Random();
                for (int j = 0; j < itemsPerThread; j++)
                {
                    int item;
                    switch (random.Next(5))
                    {
                        case 0: bag.Add(j); break;
                        case 1: bag.TryPeek(out item); break;
                        case 2: bag.TryTake(out item); break;
                        case 3: bag.Clear(); break;
                        case 4: bag.ToArray(); break;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
        }
    }
}
