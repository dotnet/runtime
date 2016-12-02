// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    /// <summary>The class that contains the unit tests of the LazyInit.</summary>
    public class ConcurrentBagTests : IEnumerable_Generic_Tests<int>
    {
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();
        protected override IEnumerable<int> GenericIEnumerableFactory(int count) => new ConcurrentBag<int>(Enumerable.Range(0, count));
        protected override int CreateT(int seed) => new Random(seed).Next();
        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;
        protected override bool ResetImplemented => true;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(1000)]
        public static void Ctor_InitializeFromCollection_ContainsExpectedItems(int numItems)
        {
            var expected = new HashSet<int>(Enumerable.Range(0, numItems));

            var bag = new ConcurrentBag<int>(expected);
            Assert.Equal(numItems == 0, bag.IsEmpty);
            Assert.Equal(expected.Count, bag.Count);

            int item;
            var actual = new HashSet<int>();
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected.Count - i, bag.Count);
                Assert.True(bag.TryTake(out item));
                actual.Add(item);
            }

            Assert.False(bag.TryTake(out item));
            Assert.Equal(0, item);
            Assert.True(bag.IsEmpty);
            AssertSetsEqual(expected, actual);
        }

        [Fact]
        public static void Ctor_InvalidArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>("collection", () => new ConcurrentBag<int>(null));
        }

        [Fact]
        public static void Add_TakeFromAnotherThread_ExpectedItemsTaken()
        {
            var cb = new ConcurrentBag<int>();
            Assert.True(cb.IsEmpty);
            Assert.Equal(0, cb.Count);

            const int NumItems = 100000;

            Task producer = Task.Run(() => Parallel.For(1, NumItems + 1, cb.Add));

            var hs = new HashSet<int>();
            while (hs.Count < NumItems)
            {
                int item;
                if (cb.TryTake(out item)) hs.Add(item);
            }

            producer.GetAwaiter().GetResult();

            Assert.True(cb.IsEmpty);
            Assert.Equal(0, cb.Count);
            AssertSetsEqual(new HashSet<int>(Enumerable.Range(1, NumItems)), hs);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(3, 100)]
        [InlineData(8, 1000)]
        public static void AddThenPeek_LatestLocalItemRetuned(int threadsCount, int itemsPerThread)
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
                        Assert.True(bag.TryPeek(out item));
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

        [Theory]
        [InlineData(100, 1, 100, true)]
        [InlineData(100, 4, 10, false)]
        [InlineData(1000, 11, 100, true)]
        [InlineData(100000, 2, 50000, true)]
        public static void Initialize_ThenTakeOrPeekInParallel_ItemsObtainedAsExpected(int numStartingItems, int threadsCount, int itemsPerThread, bool take)
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(1, numStartingItems));
            int successes = 0;

            using (var b = new Barrier(threadsCount))
            {
                WaitAllOrAnyFailed(Enumerable.Range(0, threadsCount).Select(threadNum => Task.Factory.StartNew(() =>
                {
                    b.SignalAndWait();
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        int data;
                        if (take ? bag.TryTake(out data) : bag.TryPeek(out data))
                        {
                            Interlocked.Increment(ref successes);
                            Assert.NotEqual(0, data); // shouldn't be default(T)
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray());
            }

            Assert.Equal(
                take ? numStartingItems : threadsCount * itemsPerThread,
                successes);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        public static void AddAndTake_ExpectedValuesTransferred(int threadsCount)
        {
            var bag = new ConcurrentBag<int>();

            int start = 0;
            int end = 10;

            Task[] threads = new Task[threadsCount];
            int[] validation = new int[(end - start) * threads.Length / 2];
            for (int i = 0; i < threads.Length; i += 2)
            {
                int localStart = start, localEnd = end;
                threads[i] = Task.Run(() => AddRange(bag, localStart, localEnd));
                threads[i + 1] = Task.Run(() => TakeRange(bag, localEnd - localStart - 1, validation));

                int step = end - start;
                start = end;
                end += step;
            }
            WaitAllOrAnyFailed(threads);

            //validation
            int value = -1;
            for (int i = 0; i < validation.Length; i++)
            {
                if (validation[i] == 0)
                {
                    Assert.True(bag.TryTake(out value));
                }
                else
                {
                    Assert.Equal(1, validation[i]);
                }
            }

            Assert.False(bag.Count > 0 || bag.TryTake(out value));
        }

        [Fact]
        public static void AddFromMultipleThreads_ItemsRemainAfterThreadsGoAway()
        {
            var bag = new ConcurrentBag<int>();

            for (int i = 0; i < 1000; i += 100)
            {
                // Create a thread that adds items to the bag
                Task.Factory.StartNew(() =>
                {
                    for (int j = i; j < i + 100; j++)
                    {
                        bag.Add(j);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();

                // Allow threads to be collected
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            AssertSetsEqual(new HashSet<int>(Enumerable.Range(0, 1000)), new HashSet<int>(bag));
        }

        [Fact]
        public static void AddManyItems_ThenTakeOnSameThread_ItemsOutputInExpectedOrder()
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, 100000));
            for (int i = 99999; i >= 0; --i)
            {
                int item;
                Assert.True(bag.TryTake(out item));
                Assert.Equal(i, item); // Testing an implementation detail rather than guaranteed ordering
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
        public static void Peek_SucceedsOnEmptyBagThatWasOnceNonEmpty()
        {
            var bag = new ConcurrentBag<int>();
            int item;

            Assert.False(bag.TryPeek(out item));
            Assert.Equal(0, item);

            bag.Add(42);
            for (int i = 0; i < 2; i++)
            {
                Assert.True(bag.TryPeek(out item));
                Assert.Equal(42, item);
            }

            Assert.True(bag.TryTake(out item));
            Assert.Equal(42, item);

            Assert.False(bag.TryPeek(out item));
            Assert.Equal(0, item);
        }

        [Fact]
        public static void CopyTo_Empty_NothingCopied()
        {
            var bag = new ConcurrentBag<int>();
            bag.CopyTo(new int[0], 0);
            bag.CopyTo(new int[10], 10);
        }

        [Fact]
        public static void CopyTo_ExpectedElementsCopied()
        {
            const int Size = 10;
            int[] dest;

            // Copy to array in which data fits perfectly
            dest = new int[Size];
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, Size));
            bag.CopyTo(dest, 0);
            Assert.Equal(Enumerable.Range(0, Size), dest.OrderBy(i => i));

            // Copy to non-0 index in array where the data fits
            dest = new int[Size + 3];
            bag = new ConcurrentBag<int>(Enumerable.Range(0, Size));
            bag.CopyTo(dest, 1);
            var results = new int[Size];
            Array.Copy(dest, 1, results, 0, results.Length);
            Assert.Equal(Enumerable.Range(0, Size), results.OrderBy(i => i));
        }

        [Fact]
        public static void CopyTo_InvalidArgs_Throws()
        {
            var bag = new ConcurrentBag<int>(Enumerable.Range(0, 10));
            int[] dest = new int[10];

            Assert.Throws<ArgumentNullException>("array", () => bag.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => bag.CopyTo(dest, -1));
            Assert.Throws<ArgumentException>(() => bag.CopyTo(dest, dest.Length));
            Assert.Throws<ArgumentException>(() => bag.CopyTo(dest, dest.Length - 2));
        }

        [Fact]
        public static void ICollectionCopyTo_ExpectedElementsCopied()
        {
            const int Size = 10;
            int[] dest;

            // Copy to array in which data fits perfectly
            dest = new int[Size];
            ICollection c = new ConcurrentBag<int>(Enumerable.Range(0, Size));
            c.CopyTo(dest, 0);
            Assert.Equal(Enumerable.Range(0, Size), dest.OrderBy(i => i));

            // Copy to non-0 index in array where the data fits
            dest = new int[Size + 3];
            c = new ConcurrentBag<int>(Enumerable.Range(0, Size));
            c.CopyTo(dest, 1);
            var results = new int[Size];
            Array.Copy(dest, 1, results, 0, results.Length);
            Assert.Equal(Enumerable.Range(0, Size), results.OrderBy(i => i));
        }

        [Fact]
        public static void ICollectionCopyTo_InvalidArgs_Throws()
        {
            ICollection bag = new ConcurrentBag<int>(Enumerable.Range(0, 10));
            Array dest = new int[10];

            Assert.Throws<ArgumentNullException>("array", () => bag.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => bag.CopyTo(dest, -1));
            Assert.Throws<ArgumentException>(() => bag.CopyTo(dest, dest.Length));
            Assert.Throws<ArgumentException>(() => bag.CopyTo(dest, dest.Length - 2));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(1, false)]
        [InlineData(10, true)]
        [InlineData(100, true)]
        [InlineData(100, false)]
        public static async Task GetEnumerator_Generic_ExpectedElementsYielded(int numItems, bool consumeFromSameThread)
        {
            var bag = new ConcurrentBag<int>();
            using (var e = bag.GetEnumerator())
            {
                Assert.False(e.MoveNext());
            }

            // Add, and validate enumeration after each item added
            for (int i = 1; i <= numItems; i++)
            {
                bag.Add(i);
                Assert.Equal(i, bag.Count);
                Assert.Equal(i, bag.Distinct().Count());
            }

            // Take, and validate enumerate after each item removed.
            Action consume = () =>
            {
                for (int i = 1; i <= numItems; i++)
                {
                    int item;
                    Assert.True(bag.TryTake(out item));
                    Assert.Equal(numItems - i, bag.Count);
                    Assert.Equal(numItems - i, bag.Distinct().Count());
                }
            };
            if (consumeFromSameThread)
            {
                consume();
            }
            else
            {
                await Task.Factory.StartNew(consume, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        [Fact]
        public static void GetEnumerator_NonGeneric()
        {
            var bag = new ConcurrentBag<int>();
            var ebag = (IEnumerable)bag;

            Assert.False(ebag.GetEnumerator().MoveNext());

            bag.Add(42);
            bag.Add(84);

            var hs = new HashSet<int>(ebag.Cast<int>());
            Assert.Equal(2, hs.Count);
            Assert.Contains(42, hs);
            Assert.Contains(84, hs);
        }

        [Fact]
        public static void GetEnumerator_EnumerationsAreSnapshots()
        {
            var bag = new ConcurrentBag<int>();
            Assert.Empty(bag);

            using (IEnumerator<int> e1 = bag.GetEnumerator())
            {
                bag.Add(1);
                using (IEnumerator<int> e2 = bag.GetEnumerator())
                {
                    bag.Add(2);
                    using (IEnumerator<int> e3 = bag.GetEnumerator())
                    {
                        int item;
                        Assert.True(bag.TryTake(out item));
                        using (IEnumerator<int> e4 = bag.GetEnumerator())
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

        [Theory]
        [InlineData(100, 1, 10)] 
        [InlineData(4, 100000, 10)]
        public static void BlockingCollection_WrappingBag_ExpectedElementsTransferred(int numThreadsPerConsumerProducer, int numItemsPerThread, int producerSpin)
        {
            var bc = new BlockingCollection<int>(new ConcurrentBag<int>());
            long dummy = 0;

            Task[] producers = Enumerable.Range(0, numThreadsPerConsumerProducer).Select(_ => Task.Factory.StartNew(() =>
            {
                for (int i = 1; i <= numItemsPerThread; i++)
                {
                    for (int j = 0; j < producerSpin; j++) dummy *= j; // spin a little
                    bc.Add(i);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

            Task[] consumers = Enumerable.Range(0, numThreadsPerConsumerProducer).Select(_ => Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < numItemsPerThread; i++)
                {
                    const int TimeoutMs = 100000;
                    int item;
                    Assert.True(bc.TryTake(out item, TimeoutMs), $"Couldn't get {i}th item after {TimeoutMs}ms");
                    Assert.NotEqual(0, item);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

            WaitAllOrAnyFailed(producers);
            WaitAllOrAnyFailed(consumers);
        }

        [Fact]
        public static void IProducerConsumerCollection_TryAdd_TryTake_ToArray()
        {
            IProducerConsumerCollection<int> bag = new ConcurrentBag<int>();

            Assert.True(bag.TryAdd(42));
            Assert.Equal(new[] { 42 }, bag.ToArray());

            Assert.True(bag.TryAdd(84));
            Assert.Equal(new[] { 42, 84 }, bag.ToArray().OrderBy(i => i));

            int item;

            Assert.True(bag.TryTake(out item));
            int remainingItem = item == 42 ? 84 : 42;
            Assert.Equal(new[] { remainingItem }, bag.ToArray());
            Assert.True(bag.TryTake(out item));
            Assert.Equal(remainingItem, item);
            Assert.Empty(bag.ToArray());
        }

        [Fact]
        public static void ICollection_IsSynchronized_SyncRoot()
        {
            ICollection bag = new ConcurrentBag<int>();
            Assert.False(bag.IsSynchronized);
            Assert.Throws<NotSupportedException>(() => bag.SyncRoot);
        }

        [Fact]
        public static void ToArray_ParallelInvocations_Succeed()
        {
            var bag = new ConcurrentBag<int>();
            Assert.Empty(bag.ToArray());

            const int NumItems = 10000;

            Parallel.For(0, NumItems, bag.Add);
            Assert.Equal(NumItems, bag.Count);

            Parallel.For(0, 10, i =>
            {
                var hs = new HashSet<int>(bag.ToArray());
                Assert.Equal(NumItems, hs.Count);
            });
        }

        [OuterLoop("Runs for several seconds")]
        [Fact]
        public static void ManyConcurrentAddsTakes_BagRemainsConsistent_LongRunning() =>
            ManyConcurrentAddsTakes_BagRemainsConsistent(3.0);

        [Theory]
        [InlineData(0.5)]
        public static void ManyConcurrentAddsTakes_BagRemainsConsistent(double seconds)
        {
            var bag = new ConcurrentBag<long>();

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            // Thread that adds
            Task<HashSet<long>> adds = Task.Run(() =>
            {
                var added = new HashSet<long>();
                long i = long.MinValue;
                while (DateTime.UtcNow < end)
                {
                    i++;
                    bag.Add(i);
                    added.Add(i);
                }
                return added;
            });

            // Thread that adds and takes
            Task<KeyValuePair<HashSet<long>,HashSet<long>>> addsAndTakes = Task.Run(() =>
            {
                var added = new HashSet<long>();
                var taken = new HashSet<long>();

                long i = 1; // avoid 0 as default(T), to detect accidentally reading a default value
                while (DateTime.UtcNow < end)
                {
                    i++;
                    bag.Add(i);
                    added.Add(i);

                    long item;
                    if (bag.TryTake(out item))
                    {
                        Assert.NotEqual(0, item);
                        taken.Add(item);
                    }
                }

                return new KeyValuePair<HashSet<long>, HashSet<long>>(added, taken);
            });

            // Thread that just takes
            Task<HashSet<long>> takes = Task.Run(() =>
            {
                var taken = new HashSet<long>();
                while (DateTime.UtcNow < end)
                {
                    long item;
                    if (bag.TryTake(out item))
                    {
                        Assert.NotEqual(0, item);
                        taken.Add(item);
                    }
                }
                return taken;
            });

            // Wait for them all to finish
            WaitAllOrAnyFailed(adds, addsAndTakes, takes);

            // Combine everything they added and remove everything they took
            var total = new HashSet<long>(adds.Result);
            total.UnionWith(addsAndTakes.Result.Key);
            total.ExceptWith(addsAndTakes.Result.Value);
            total.ExceptWith(takes.Result);

            // What's left should match what's in the bag
            Assert.Equal(total.OrderBy(i => i), bag.OrderBy(i => i));
        }

        [OuterLoop("Runs for several seconds")]
        [Fact]
        public static void ManyConcurrentAddsTakesPeeks_ForceContentionWithSteals_LongRunning() =>
            ManyConcurrentAddsTakesPeeks_ForceContentionWithSteals(3.0);

        [Theory]
        [InlineData(0.5)]
        public static void ManyConcurrentAddsTakesPeeks_ForceContentionWithSteals(double seconds)
        {
            var bag = new ConcurrentBag<int>();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task<long> addsTakes = Task.Factory.StartNew(() =>
            {
                long total = 0;
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        bag.Add(i);
                        total++;
                    }

                    int item;
                    if (bag.TryPeek(out item))
                    {
                        Assert.InRange(item, 1, MaxCount);
                    }

                    for (int i = 1; i <= MaxCount; i++)
                    {
                        if (bag.TryTake(out item))
                        {
                            total--;
                            Assert.InRange(item, 1, MaxCount);
                        }
                    }
                }
                return total;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Task<long> steals = Task.Factory.StartNew(() =>
            {
                long total = 0;
                int item;
                while (DateTime.UtcNow < end)
                {
                    if (bag.TryTake(out item))
                    {
                        total++;
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
                return total;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            WaitAllOrAnyFailed(addsTakes, steals);
            long remaining = addsTakes.Result - steals.Result;
            Assert.InRange(remaining, 0, long.MaxValue);
            Assert.Equal(remaining, bag.Count);
        }

        [OuterLoop("Runs for several seconds")]
        [Fact]
        public static void ManyConcurrentAddsTakesPeeks_ForceContentionWithStealingPeeks_LongRunning() =>
            ManyConcurrentAddsTakesPeeks_ForceContentionWithStealingPeeks(3.0);

        [Theory]
        [InlineData(0.5)]
        public static void ManyConcurrentAddsTakesPeeks_ForceContentionWithStealingPeeks(double seconds)
        {
            var bag = new ConcurrentBag<int>();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task<long> addsTakes = Task.Factory.StartNew(() =>
            {
                long total = 0;
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        bag.Add(i);
                        total++;
                    }

                    int item;
                    Assert.True(bag.TryPeek(out item));
                    Assert.Equal(MaxCount, item);

                    for (int i = 1; i <= MaxCount; i++)
                    {
                        if (bag.TryTake(out item))
                        {
                            total--;
                            Assert.InRange(item, 1, MaxCount);
                        }
                    }
                }
                return total;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Task steals = Task.Factory.StartNew(() =>
            {
                int item;
                while (DateTime.UtcNow < end)
                {
                    if (bag.TryPeek(out item))
                    {
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            WaitAllOrAnyFailed(addsTakes, steals);
            Assert.Equal(0, addsTakes.Result);
            Assert.Equal(0, bag.Count);
        }

        [OuterLoop("Runs for several seconds")]
        [Fact]
        public static void ManyConcurrentAddsTakes_ForceContentionWithFreezing_LongRunning() =>
            ManyConcurrentAddsTakes_ForceContentionWithFreezing(3.0);

        [Theory]
        [InlineData(0.5)]
        public static void ManyConcurrentAddsTakes_ForceContentionWithFreezing(double seconds)
        {
            var bag = new ConcurrentBag<int>();
            const int MaxCount = 4;

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);

            Task addsTakes = Task.Factory.StartNew(() =>
            {
                while (DateTime.UtcNow < end)
                {
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        bag.Add(i);
                    }
                    for (int i = 1; i <= MaxCount; i++)
                    {
                        int item;
                        Assert.True(bag.TryTake(out item));
                        Assert.InRange(item, 1, MaxCount);
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            while (DateTime.UtcNow < end)
            {
                int[] arr = bag.ToArray();
                Assert.InRange(arr.Length, 0, MaxCount);
                Assert.DoesNotContain(0, arr); // make sure we didn't get default(T)
            }

            addsTakes.GetAwaiter().GetResult();
            Assert.Equal(0, bag.Count);
        }

        [Fact]
        public static void ValidateDebuggerAttributes()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new ConcurrentBag<int>());
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(new ConcurrentBag<int>());

            DebuggerAttributes.ValidateDebuggerDisplayReferences(new ConcurrentBag<int>(Enumerable.Range(0, 10)));
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(new ConcurrentBag<int>(Enumerable.Range(0, 10)));
        }

        private static void AddRange(ConcurrentBag<int> bag, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                bag.Add(i);
            }
        }

        private static void TakeRange(ConcurrentBag<int> bag, int count, int[] validation)
        {
            for (int i = 0; i < count; i++)
            {
                int value;
                if (bag.TryTake(out value))
                {
                    Interlocked.Increment(ref validation[value]);
                }
            }
        }

        private static void AssertSetsEqual<T>(HashSet<T> expected, HashSet<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            Assert.Subset(expected, actual);
            Assert.Subset(actual, expected);
        }

        private static void WaitAllOrAnyFailed(params Task[] tasks)
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
    }
}
