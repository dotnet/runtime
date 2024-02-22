// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// #define StressTest // set to raise the amount of time spent in concurrency tests that stress the collections

using System;
using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public abstract class ProducerConsumerCollectionTests : IEnumerable_Generic_Tests<int>
    {
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();
        protected override int CreateT(int seed) => new Random(seed).Next();
        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;
        protected override IEnumerable<int> GenericIEnumerableFactory(int count) => CreateProducerConsumerCollection(count);
        protected IProducerConsumerCollection<int> CreateProducerConsumerCollection() => CreateProducerConsumerCollection<int>();
        protected IProducerConsumerCollection<int> CreateProducerConsumerCollection(int count) => CreateProducerConsumerCollection(Enumerable.Range(0, count));

        protected abstract IProducerConsumerCollection<T> CreateProducerConsumerCollection<T>();
        protected abstract IProducerConsumerCollection<int> CreateProducerConsumerCollection(IEnumerable<int> collection);
        protected abstract bool IsEmpty(IProducerConsumerCollection<int> pcc);
        protected abstract bool TryPeek<T>(IProducerConsumerCollection<T> pcc, out T result);
        protected virtual IProducerConsumerCollection<int> CreateOracle() => CreateOracle(Enumerable.Empty<int>());
        protected abstract IProducerConsumerCollection<int> CreateOracle(IEnumerable<int> collection);

        protected static TaskFactory ThreadFactory { get; } = new TaskFactory(
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskContinuationOptions.LongRunning, TaskScheduler.Default);
        private const double ConcurrencyTestSeconds =
#if StressTest
            8.0;
#else
            1.0;
#endif

        protected virtual string CopyToNoLengthParamName => "destinationArray";

        [Fact]
        public void Ctor_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("collection", () => CreateProducerConsumerCollection(null));
        }

        [Fact]
        public void Ctor_NoArg_ItemsAndCountMatch()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.Equal(0, c.Count);
            Assert.True(IsEmpty(c));
            Assert.Equal(Enumerable.Empty<int>(), c);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public void Ctor_Collection_ItemsAndCountMatch(int count)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(Enumerable.Range(1, count));
            IProducerConsumerCollection<int> oracle = CreateOracle(Enumerable.Range(1, count));

            Assert.Equal(oracle.Count == 0, IsEmpty(c));
            Assert.Equal(oracle.Count, c.Count);
            Assert.Equal<int>(oracle, c);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(1000)]
        public void Ctor_InitializeFromCollection_ContainsExpectedItems(int numItems)
        {
            var expected = new HashSet<int>(Enumerable.Range(0, numItems));

            IProducerConsumerCollection<int> pcc = CreateProducerConsumerCollection(expected);
            Assert.Equal(expected.Count, pcc.Count);

            int item;
            var actual = new HashSet<int>();
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected.Count - i, pcc.Count);
                Assert.True(pcc.TryTake(out item));
                actual.Add(item);
            }

            Assert.False(pcc.TryTake(out item));
            Assert.Equal(0, item);
            AssertSetsEqual(expected, actual);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void Add_TakeFromAnotherThread_ExpectedItemsTaken()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.True(IsEmpty(c));
            Assert.Equal(0, c.Count);

            const int NumItems = 100000;

            Task producer = Task.Run(() => Parallel.For(1, NumItems + 1, i => Assert.True(c.TryAdd(i))));

            var hs = new HashSet<int>();
            while (hs.Count < NumItems)
            {
                int item;
                if (c.TryTake(out item)) hs.Add(item);
            }

            producer.GetAwaiter().GetResult();

            Assert.True(IsEmpty(c));
            Assert.Equal(0, c.Count);
            AssertSetsEqual(new HashSet<int>(Enumerable.Range(1, NumItems)), hs);
        }

        [Fact]
        public void AddSome_ThenInterleaveAddsAndTakes_MatchesOracle()
        {
            IProducerConsumerCollection<int> c = CreateOracle();
            IProducerConsumerCollection<int> oracle = CreateProducerConsumerCollection();

            Action take = () =>
            {
                int item1;
                Assert.True(c.TryTake(out item1));
                int item2;
                Assert.True(oracle.TryTake(out item2));
                Assert.Equal(item1, item2);
                Assert.Equal(c.Count, oracle.Count);
                Assert.Equal<int>(c, oracle);
            };

            for (int i = 0; i < 100; i++)
            {
                Assert.True(oracle.TryAdd(i));
                Assert.True(c.TryAdd(i));
                Assert.Equal(c.Count, oracle.Count);
                Assert.Equal<int>(c, oracle);

                // Start taking some after we've added some
                if (i > 50)
                {
                    take();
                }
            }

            // Take the rest
            while (c.Count > 0)
            {
                take();
            }
        }

        [Fact]
        public void AddTake_RandomChangesMatchOracle()
        {
            const int Iters = 1000;
            var r = new Random(42);

            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            IProducerConsumerCollection<int> oracle = CreateOracle();

            for (int i = 0; i < Iters; i++)
            {
                if (r.NextDouble() < .5)
                {
                    Assert.Equal(oracle.TryAdd(i), c.TryAdd(i));
                }
                else
                {
                    int expected, actual;
                    Assert.Equal(oracle.TryTake(out expected), c.TryTake(out actual));
                    Assert.Equal(expected, actual);
                }
            }

            Assert.Equal(oracle.Count, c.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 4, 10, false)]
        [InlineData(1000, 11, 100, true)]
        [InlineData(100000, 2, 50000, true)]
        public void Initialize_ThenTakeOrPeekInParallel_ItemsObtainedAsExpected(int numStartingItems, int threadsCount, int itemsPerThread, bool take)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(Enumerable.Range(1, numStartingItems));
            int successes = 0;

            using (var b = new Barrier(threadsCount))
            {
                WaitAllOrAnyFailed(Enumerable.Range(0, threadsCount).Select(threadNum => ThreadFactory.StartNew(() =>
                {
                    b.SignalAndWait();
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        int data;
                        if (take ? c.TryTake(out data) : TryPeek(c, out data))
                        {
                            Interlocked.Increment(ref successes);
                            Assert.NotEqual(0, data); // shouldn't be default(T)
                        }
                    }
                })).ToArray());
            }

            Assert.Equal(
                take ? numStartingItems : threadsCount * itemsPerThread,
                successes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public void ToArray_AddAllItemsThenEnumerate_ItemsAndCountMatch(int count)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            IProducerConsumerCollection<int> oracle = CreateOracle();

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(oracle.TryAdd(i + count), c.TryAdd(i + count));
            }

            Assert.Equal(oracle.ToArray(), c.ToArray());

            if (count > 0)
            {
                int expected, actual;
                Assert.Equal(oracle.TryTake(out expected), c.TryTake(out actual));
                Assert.Equal(expected, actual);
                Assert.Equal(oracle.ToArray(), c.ToArray());
            }
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 10)]
        [InlineData(10, 10)]
        [InlineData(100, 10)]
        public void ToArray_AddAndTakeItemsIntermixedWithEnumeration_ItemsAndCountMatch(int initialCount, int iters)
        {
            IEnumerable<int> initialItems = Enumerable.Range(1, initialCount);
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(initialItems);
            IProducerConsumerCollection<int> oracle = CreateOracle(initialItems);

            for (int i = 0; i < iters; i++)
            {
                Assert.Equal<int>(oracle, c);

                Assert.Equal(oracle.TryAdd(initialCount + i), c.TryAdd(initialCount + i));
                Assert.Equal<int>(oracle, c);

                int expected, actual;
                Assert.Equal(oracle.TryTake(out expected), c.TryTake(out actual));
                Assert.Equal(expected, actual);
                Assert.Equal<int>(oracle, c);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void AddFromMultipleThreads_ItemsRemainAfterThreadsGoAway()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();

            for (int i = 0; i < 1000; i += 100)
            {
                // Create a thread that adds items to the collection
                ThreadFactory.StartNew(() =>
                {
                    for (int j = i; j < i + 100; j++)
                    {
                        Assert.True(c.TryAdd(j));
                    }
                }).GetAwaiter().GetResult();

                // Allow threads to be collected
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            AssertSetsEqual(new HashSet<int>(Enumerable.Range(0, 1000)), new HashSet<int>(c));
        }

        [Fact]
        public void AddManyItems_ThenTakeOnSameThread_ItemsOutputInExpectedOrder()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(Enumerable.Range(0, 100000));
            IProducerConsumerCollection<int> oracle = CreateOracle(Enumerable.Range(0, 100000));

            for (int i = 99999; i >= 0; --i)
            {
                int expected, actual;
                Assert.Equal(oracle.TryTake(out expected), c.TryTake(out actual));
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void TryPeek_SucceedsOnEmptyCollectionThatWasOnceNonEmpty()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            int item;

            for (int i = 0; i < 2; i++)
            {
                Assert.False(TryPeek(c, out item));
                Assert.Equal(0, item);
            }

            Assert.True(c.TryAdd(42));

            for (int i = 0; i < 2; i++)
            {
                Assert.True(TryPeek(c, out item));
                Assert.Equal(42, item);
            }

            Assert.True(c.TryTake(out item));
            Assert.Equal(42, item);

            Assert.False(TryPeek(c, out item));
            Assert.Equal(0, item);
            Assert.False(c.TryTake(out item));
            Assert.Equal(0, item);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void AddTakeWithAtLeastOneElementInCollection_IsEmpty_AlwaysFalse()
        {
            int items = 1000;

            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.True(c.TryAdd(0)); // make sure it's never empty
            var cts = new CancellationTokenSource();

            // Consumer repeatedly calls IsEmpty until it's told to stop
            Task consumer = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested) Assert.False(IsEmpty(c));
            });

            // Producer adds/takes a bunch of items, then tells the consumer to stop
            Task producer = Task.Run(() =>
            {
                int ignored;
                for (int i = 1; i <= items; i++)
                {
                    Assert.True(c.TryAdd(i));
                    Assert.True(c.TryTake(out ignored));
                }
                cts.Cancel();
            });

            Task.WaitAll(producer, consumer);
        }

        [Fact]
        public void CopyTo_Empty_NothingCopied()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            c.CopyTo(new int[0], 0);
            c.CopyTo(new int[10], 10);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CopyTo_SzArray_ZeroIndex_ExpectedElementsCopied(int size)
        {
            IEnumerable<int> initialItems = Enumerable.Range(1, size);

            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(initialItems);
            int[] actual = new int[size];
            c.CopyTo(actual, 0);

            IProducerConsumerCollection<int> oracle = CreateOracle(initialItems);
            int[] expected = new int[size];
            oracle.CopyTo(expected, 0);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CopyTo_SzArray_NonZeroIndex_ExpectedElementsCopied(int size)
        {
            IEnumerable<int> initialItems = Enumerable.Range(1, size);

            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(initialItems);
            int[] actual = new int[size + 2];
            c.CopyTo(actual, 1);

            IProducerConsumerCollection<int> oracle = CreateOracle(initialItems);
            int[] expected = new int[size + 2];
            oracle.CopyTo(expected, 1);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void CopyTo_ArrayZeroLowerBound_ZeroIndex_ExpectedElementsCopied(int size)
        {
            IEnumerable<int> initialItems = Enumerable.Range(1, size);

            ICollection c = CreateProducerConsumerCollection(initialItems);
            Array actual = Array.CreateInstance(typeof(int), new int[] { size }, new int[] { 0 });
            c.CopyTo(actual, 0);

            ICollection oracle = CreateOracle(initialItems);
            Array expected = Array.CreateInstance(typeof(int), new int[] { size }, new int[] { 0 });
            oracle.CopyTo(expected, 0);

            Assert.Equal(expected.Cast<int>(), actual.Cast<int>());
        }

        [Fact]
        public void CopyTo_ArrayNonZeroLowerBound_ExpectedElementsCopied()
        {
            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            int[] initialItems = Enumerable.Range(1, 10).ToArray();

            const int LowerBound = 1;
            ICollection c = CreateProducerConsumerCollection(initialItems);
            Array actual = Array.CreateInstance(typeof(int), new int[] { initialItems.Length }, new int[] { LowerBound });
            c.CopyTo(actual, LowerBound);

            ICollection oracle = CreateOracle(initialItems);
            int[] expected = new int[initialItems.Length];
            oracle.CopyTo(expected, 0);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual.GetValue(i + LowerBound));
            }
        }

        [Fact]
        public void CopyTo_InvalidArgs_Throws()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(Enumerable.Range(0, 10));
            int[] dest = new int[10];

            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(dest, -1));
            AssertExtensions.Throws<ArgumentException>(CopyToNoLengthParamName, "", () => c.CopyTo(dest, dest.Length));
            AssertExtensions.Throws<ArgumentException>(CopyToNoLengthParamName, "", () => c.CopyTo(dest, dest.Length - 2));

            AssertExtensions.Throws<ArgumentException>(null, () => c.CopyTo(new int[7, 7], 0));
        }

        [Fact]
        public void ICollectionCopyTo_InvalidArgs_Throws()
        {
            ICollection c = CreateProducerConsumerCollection(Enumerable.Range(0, 10));
            Array dest = new int[10];

            AssertExtensions.Throws<ArgumentNullException>("array", () => c.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.CopyTo(dest, -1));
            AssertExtensions.Throws<ArgumentException>(CopyToNoLengthParamName, "", () => c.CopyTo(dest, dest.Length));
            AssertExtensions.Throws<ArgumentException>(CopyToNoLengthParamName, "", () => c.CopyTo(dest, dest.Length - 2));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(100, 1, 10)]
        [InlineData(4, 100000, 10)]
        public void BlockingCollection_WrappingCollection_ExpectedElementsTransferred(int numThreadsPerConsumerProducer, int numItemsPerThread, int producerSpin)
        {
            var bc = new BlockingCollection<int>(CreateProducerConsumerCollection());
            long dummy = 0;

            Task[] producers = Enumerable.Range(0, numThreadsPerConsumerProducer).Select(_ => ThreadFactory.StartNew(() =>
            {
                for (int i = 1; i <= numItemsPerThread; i++)
                {
                    for (int j = 0; j < producerSpin; j++) dummy *= j; // spin a little
                    bc.Add(i);
                }
            })).ToArray();

            Task[] consumers = Enumerable.Range(0, numThreadsPerConsumerProducer).Select(_ => ThreadFactory.StartNew(() =>
            {
                for (int i = 0; i < numItemsPerThread; i++)
                {
                    const int TimeoutMs = 100000;
                    int item;
                    Assert.True(bc.TryTake(out item, TimeoutMs), $"Couldn't get {i}th item after {TimeoutMs}ms");
                    Assert.NotEqual(0, item);
                }
            })).ToArray();

            WaitAllOrAnyFailed(producers);
            WaitAllOrAnyFailed(consumers);
        }

        [Fact]
        public void GetEnumerator_NonGeneric()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            IEnumerable e = c;

            Assert.False(e.GetEnumerator().MoveNext());

            Assert.True(c.TryAdd(42));
            Assert.True(c.TryAdd(84));

            var hs = new HashSet<int>(e.Cast<int>());
            Assert.Equal(2, hs.Count);
            Assert.Contains(42, hs);
            Assert.Contains(84, hs);
        }

        [Fact]
        public void GetEnumerator_EnumerationsAreSnapshots()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.Empty(c);

            using (IEnumerator<int> e1 = c.GetEnumerator())
            {
                Assert.True(c.TryAdd(1));
                using (IEnumerator<int> e2 = c.GetEnumerator())
                {
                    Assert.True(c.TryAdd(2));
                    using (IEnumerator<int> e3 = c.GetEnumerator())
                    {
                        int item;
                        Assert.True(c.TryTake(out item));
                        using (IEnumerator<int> e4 = c.GetEnumerator())
                        {
                            Assert.False(e1.MoveNext());

                            Assert.True(e2.MoveNext());
                            Assert.False(e2.MoveNext());

                            Assert.True(e3.MoveNext());
                            Assert.True(e3.MoveNext());
                            Assert.False(e3.MoveNext());

                            Assert.True(e4.MoveNext());
                            Assert.False(e4.MoveNext());
                        }
                    }
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(10, true)]
        [InlineData(100, true)]
        [InlineData(100, false)]
        public async Task GetEnumerator_Generic_ExpectedElementsYielded(int numItems, bool consumeFromSameThread)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            using (var e = c.GetEnumerator())
            {
                Assert.False(e.MoveNext());
            }

            // Add, and validate enumeration after each item added
            for (int i = 1; i <= numItems; i++)
            {
                Assert.True(c.TryAdd(i));
                Assert.Equal(i, c.Count);
                Assert.Equal(i, c.Distinct().Count());
            }

            // Take, and validate enumerate after each item removed.
            Action consume = () =>
            {
                for (int i = 1; i <= numItems; i++)
                {
                    int item;
                    Assert.True(c.TryTake(out item));
                    Assert.Equal(numItems - i, c.Count);
                    Assert.Equal(numItems - i, c.Distinct().Count());
                }
            };
            if (consumeFromSameThread)
            {
                consume();
            }
            else
            {
                await ThreadFactory.StartNew(consume);
            }
        }

        [Fact]
        public void TryAdd_TryTake_ToArray()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();

            Assert.True(c.TryAdd(42));
            Assert.Equal(new[] { 42 }, c.ToArray());

            Assert.True(c.TryAdd(84));
            Assert.Equal(new[] { 42, 84 }, c.ToArray().OrderBy(i => i));

            int item;

            Assert.True(c.TryTake(out item));
            int remainingItem = item == 42 ? 84 : 42;
            Assert.Equal(new[] { remainingItem }, c.ToArray());
            Assert.True(c.TryTake(out item));
            Assert.Equal(remainingItem, item);
            Assert.Empty(c.ToArray());
        }

        [Fact]
        public void ICollection_IsSynchronized_SyncRoot()
        {
            ICollection c = CreateProducerConsumerCollection();
            Assert.False(c.IsSynchronized);
            Assert.Throws<NotSupportedException>(() => c.SyncRoot);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void ToArray_ParallelInvocations_Succeed()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.Empty(c.ToArray());

            const int NumItems = 10000;

            Parallel.For(0, NumItems, i => Assert.True(c.TryAdd(i)));
            Assert.Equal(NumItems, c.Count);

            Parallel.For(0, 10, i =>
            {
                var hs = new HashSet<int>(c.ToArray());
                Assert.Equal(NumItems, hs.Count);
            });
        }

        [Fact]
        public void ToArray_AddTakeSameThread_ExpectedResultsAfterAddsAndTakes()
        {
            const int Items = 20;

            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            IProducerConsumerCollection<int> oracle = CreateOracle();

            for (int i = 0; i < Items; i++)
            {
                Assert.True(c.TryAdd(i));
                Assert.True(oracle.TryAdd(i));
                Assert.Equal(oracle.ToArray(), c.ToArray());
            }

            for (int i = Items - 1; i >= 0; i--)
            {
                int expected, actual;
                Assert.Equal(oracle.TryTake(out expected), c.TryTake(out actual));
                Assert.Equal(expected, actual);
                Assert.Equal(oracle.ToArray(), c.ToArray());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public void GetEnumerator_ParallelInvocations_Succeed()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Assert.Empty(c.ToArray());

            const int NumItems = 10000;

            Parallel.For(0, NumItems, i => Assert.True(c.TryAdd(i)));
            Assert.Equal(NumItems, c.Count);

            Parallel.For(0, 10, i =>
            {
                var hs = new HashSet<int>(c);
                Assert.Equal(NumItems, hs.Count);
            });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(1, ConcurrencyTestSeconds / 2)]
        [InlineData(4, ConcurrencyTestSeconds / 2)]
        public void ManyConcurrentAddsTakes_EnsureTrackedCountsMatchResultingCollection(int threadsPerProc, double seconds)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();

            DateTime end = default(DateTime);
            using (var b = new Barrier(Environment.ProcessorCount * threadsPerProc, _ => end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds)))
            {
                Task<int>[] tasks = Enumerable.Range(0, b.ParticipantCount).Select(_ => ThreadFactory.StartNew(() =>
                {
                    b.SignalAndWait();

                    int count = 0;
                    var rand = new Random();

                    while (DateTime.UtcNow < end)
                    {
                        if (rand.NextDouble() < .5)
                        {
                            Assert.True(c.TryAdd(rand.Next()));
                            count++;
                        }
                        else
                        {
                            int item;
                            if (c.TryTake(out item)) count--;
                        }
                    }

                    return count;
                })).ToArray();
                Task.WaitAll(tasks);
                Assert.Equal(tasks.Sum(t => t.Result), c.Count);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public void ManyConcurrentAddsTakes_CollectionRemainsConsistent()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();

            const int operations = 30000;
            Action addAndRemove = () =>
            {
                for (int i = 1; i < operations; i++)
                {
                    int addCount = new Random(12354).Next(1, 100);
                    int item;
                    for (int j = 0; j < addCount; j++)
                        Assert.True(c.TryAdd(i));
                    for (int j = 0; j < addCount; j++)
                        Assert.True(c.TryTake(out item));
                }
            };

            const int numberOfThreads = 3;
            var tasks = new Task[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++)
                tasks[i] = ThreadFactory.StartNew(addAndRemove);

            // Wait for them all to finish
            WaitAllOrAnyFailed(tasks);

            Assert.Empty(c);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        [InlineData(ConcurrencyTestSeconds)]
        public void ManyConcurrentAddsTakesPeeks_ForceContentionWithOtherThreadsTaking(double seconds)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task<long> addsTakes = ThreadFactory.StartNew(() =>
            {
                long total = 0;
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        Assert.True(c.TryAdd(i));
                        total++;
                    }

                    int item;
                    if (TryPeek(c, out item))
                    {
                        Assert.InRange(item, 1, MaxCount);
                    }

                    for (int i = 1; i <= MaxCount; i++)
                    {
                        if (c.TryTake(out item))
                        {
                            total--;
                            Assert.InRange(item, 1, MaxCount);
                        }
                    }
                }
                return total;
            });

            Task<long> takesFromOtherThread = ThreadFactory.StartNew(() =>
            {
                long total = 0;
                int item;
                while (DateTime.UtcNow < end)
                {
                    if (c.TryTake(out item))
                    {
                        total++;
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
                return total;
            });

            WaitAllOrAnyFailed(addsTakes, takesFromOtherThread);
            long remaining = addsTakes.Result - takesFromOtherThread.Result;
            Assert.InRange(remaining, 0, long.MaxValue);
            Assert.Equal(remaining, c.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(ConcurrencyTestSeconds)]
        public void ManyConcurrentAddsTakesPeeks_ForceContentionWithOtherThreadsPeeking(double seconds)
        {
            IProducerConsumerCollection<LargeStruct> c = CreateProducerConsumerCollection<LargeStruct>();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task<long> addsTakes = ThreadFactory.StartNew(() =>
            {
                long total = 0;
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        Assert.True(c.TryAdd(new LargeStruct(i)));
                        total++;
                    }

                    LargeStruct item;
                    Assert.True(TryPeek(c, out item));
                    Assert.InRange(item.Value, 1, MaxCount);

                    for (int i = 1; i <= MaxCount; i++)
                    {
                        if (c.TryTake(out item))
                        {
                            total--;
                            Assert.InRange(item.Value, 1, MaxCount);
                        }
                    }
                }
                return total;
            });

            Task peeksFromOtherThread = ThreadFactory.StartNew(() =>
            {
                LargeStruct item;
                while (DateTime.UtcNow < end)
                {
                    if (TryPeek(c, out item))
                    {
                        Assert.InRange(item.Value, 1, MaxCount);
                    }
                }
            });

            WaitAllOrAnyFailed(addsTakes, peeksFromOtherThread);
            Assert.Equal(0, addsTakes.Result);
            Assert.Equal(0, c.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(ConcurrencyTestSeconds)]
        public void ManyConcurrentAddsTakes_ForceContentionWithToArray(double seconds)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task addsTakes = ThreadFactory.StartNew(() =>
            {
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        Assert.True(c.TryAdd(i));
                    }
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        int item;
                        Assert.True(c.TryTake(out item));
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
            });

            while (DateTime.UtcNow < end)
            {
                int[] arr = c.ToArray();
                Assert.InRange(arr.Length, 0, MaxCount);
                Assert.DoesNotContain(0, arr); // make sure we didn't get default(T)
            }

            addsTakes.GetAwaiter().GetResult();
            Assert.Equal(0, c.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(0, ConcurrencyTestSeconds / 2)]
        [InlineData(1, ConcurrencyTestSeconds / 2)]
        public void ManyConcurrentAddsTakes_ForceContentionWithGetEnumerator(int initialCount, double seconds)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(Enumerable.Range(1, initialCount));
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task addsTakes = ThreadFactory.StartNew(() =>
            {
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        Assert.True(c.TryAdd(i));
                    }
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        int item;
                        Assert.True(c.TryTake(out item));
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
            });

            while (DateTime.UtcNow < end)
            {
                int[] arr = c.Select(i => i).ToArray();
                Assert.InRange(arr.Length, initialCount, MaxCount + initialCount);
                Assert.DoesNotContain(0, arr); // make sure we didn't get default(T)
            }

            addsTakes.GetAwaiter().GetResult();
            Assert.Equal(initialCount, c.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [InlineData(0)]
        [InlineData(10)]
        public void DebuggerAttributes_Success(int count)
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection(count);
            DebuggerAttributes.ValidateDebuggerDisplayReferences(c);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(c);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Array items = itemProperty.GetValue(info.Instance) as Array;
            Assert.Equal(c, items.Cast<int>());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        public void DebuggerTypeProxy_Ctor_NullArgument_Throws()
        {
            IProducerConsumerCollection<int> c = CreateProducerConsumerCollection();
            Type proxyType = DebuggerAttributes.GetProxyType(c);
            var tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, new object[] { null }));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        protected static void AssertSetsEqual<T>(HashSet<T> expected, HashSet<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            Assert.Subset(expected, actual);
            Assert.Subset(actual, expected);
        }

        protected static void WaitAllOrAnyFailed(params Task[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            int remaining = tasks.Length;
            var mres = new ManualResetEventSlim();

            foreach (Task task in tasks)
            {
                task.ContinueWith(t =>
                {
                    if (Interlocked.Decrement(ref remaining) == 0 || t.IsFaulted)
                    {
                        mres.Set();
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            mres.Wait();

            // Either all tasks are completed or at least one failed
            foreach (Task t in tasks)
            {
                if (t.IsFaulted)
                {
                    t.GetAwaiter().GetResult(); // propagate for the first one that failed
                }
            }
        }

        private struct LargeStruct // used to help validate that values aren't torn while being read
        {
            private readonly long _a, _b, _c, _d, _e, _f, _g, _h;

            public LargeStruct(long value) { _a = _b = _c = _d = _e = _f = _g = _h = value; }

            public long Value
            {
                get
                {
                    if (_a != _b || _a != _c || _a != _d || _a != _e || _a != _f || _a != _g || _a != _h)
                    {
                        throw new Exception($"Inconsistent {nameof(LargeStruct)}: {_a} {_b} {_c} {_d} {_e} {_f} {_g} {_h}");
                    }
                    return _a;
                }
            }
        }
    }
}
