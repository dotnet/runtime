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

        [Fact]
        public static void TestBasicScenarios()
        {
            ConcurrentBag<int> cb = new ConcurrentBag<int>();
            Assert.True(cb.IsEmpty);
            Task[] tks = new Task[2];
            tks[0] = Task.Run(() =>
                {
                    cb.Add(4);
                    cb.Add(5);
                    cb.Add(6);
                });

            // Consume the items in the bag 
            tks[1] = Task.Run(() =>
                {
                    int item;
                    while (!cb.IsEmpty)
                    {
                        bool ret = cb.TryTake(out item);
                        Assert.True(ret);
                        // loose check
                        Assert.Contains(item, new[] { 4, 5, 6 });
                    }
                });

            Task.WaitAll(tks);
        }

        [Fact]
        public static void RTest1_Ctor()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>(new int[] { 1, 2, 3 });
            Assert.False(bag.IsEmpty);
            Assert.Equal(3, bag.Count);

            Assert.Throws<ArgumentNullException>( () => {bag = new ConcurrentBag<int>(null);} );
        }

        [Fact]
        public static void RTest2_Add()
        {
            RTest2_Add(1, 10);
            RTest2_Add(3, 100);
        }

        [Fact]
        [OuterLoop]
        public static void RTest2_Add01()
        {
            RTest2_Add(8, 1000);
        }

        [Fact]
        public static void RTest3_TakeOrPeek()
        {
            ConcurrentBag<int> bag = CreateBag(100);
            RTest3_TakeOrPeek(bag, 1, 100, true);

            bag = CreateBag(100);
            RTest3_TakeOrPeek(bag, 4, 10, false);

            bag = CreateBag(1000);
            RTest3_TakeOrPeek(bag, 11, 100, true);
        }

        [Fact]
        public static void RTest4_AddAndTake()
        {
            RTest4_AddAndTake(8);
            RTest4_AddAndTake(16);
        }

        [Fact]
        public static void RTest5_CopyTo()
        {
            const int SIZE = 10;
            Array array = new int[SIZE];
            int index = 0;

            ConcurrentBag<int> bag = CreateBag(SIZE);
            bag.CopyTo((int[])array, index);

            Assert.Throws<ArgumentNullException>(() => bag.CopyTo(null, index));
            Assert.Throws<ArgumentOutOfRangeException>(() => bag.CopyTo((int[]) array, -1));
            Assert.Throws<ArgumentException>(() => bag.CopyTo((int[])array, SIZE));
            Assert.Throws<ArgumentException>(() => bag.CopyTo((int[])array, SIZE-2));
        }

        [Fact]
        public static void RTest5_ICollectionCopyTo()
        {
            const int SIZE = 10;
            Array array = new int[SIZE];
            int index = 0;

            ConcurrentBag<int> bag = CreateBag(SIZE);
            ICollection collection = bag as ICollection;
            Assert.NotNull(collection);
            collection.CopyTo(array, index);

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, index));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo((int[])array, -1));
            Assert.Throws<ArgumentException>(() => collection.CopyTo((int[])array, SIZE));
            Assert.Throws<ArgumentException>(() => collection.CopyTo((int[])array, SIZE - 2));

            Array array2 = new int[SIZE, 5];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array2, 0));
        }

        /// <summary>
        /// Test bag addition
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RTest2_Add(int threadsCount, int itemsPerThread)
        {
            int failures = 0;
            ConcurrentBag<int> bag = new ConcurrentBag<int>();

            Task[] threads = new Task[threadsCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        try
                        {
                            bag.Add(j);
                            int item;
                            if (!bag.TryPeek(out item) || item != j)
                            {
                                Interlocked.Increment(ref failures);
                            }
                        }
                        catch
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                });
            }

            Task.WaitAll(threads);

            Assert.Equal(0, failures);
            Assert.Equal(itemsPerThread * threadsCount, bag.Count);
        }

        /// <summary>
        /// Test bag Take and Peek operations
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <param name="take"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RTest3_TakeOrPeek(ConcurrentBag<int> bag, int threadsCount, int itemsPerThread, bool take)
        {
            int bagCount = bag.Count;
            int succeeded = 0;
            int failures = 0;
            Task[] threads = new Task[threadsCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        int data;
                        bool result = false;
                        if (take)
                        {
                            result = bag.TryTake(out data);
                        }
                        else
                        {
                            result = bag.TryPeek(out data);
                        }

                        if (result)
                        {
                            Interlocked.Increment(ref succeeded);
                        }
                        else
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                });
            }

            Task.WaitAll(threads);

            if (take)
            {
                Assert.Equal(bagCount - succeeded, bag.Count);
            }
            else
            {
                Assert.Equal(0, failures);
            }
        }

        /// <summary>
        /// Test parallel Add/Take, insert unique elements in the bag, and each element should be removed once
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RTest4_AddAndTake(int threadsCount)
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();

            Task[] threads = new Task[threadsCount];
            int start = 0;
            int end = 10;

            int[] validation = new int[(end - start) * threads.Length / 2];
            for (int i = 0; i < threads.Length; i += 2)
            {
                Interval v = new Interval(start, end);
                threads[i] = Task.Factory.StartNew(
                    (o) =>
                    {
                        Interval n = (Interval)o;
                        Add(bag, n.m_start, n.m_end);
                    }, v, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                threads[i + 1] = Task.Run(() => Take(bag, end - start - 1, validation));

                int step = end - start;
                start = end;
                end += step;
            }

            Task.WaitAll(threads);

            int value = -1;

            //validation
            for (int i = 0; i < validation.Length; i++)
            {
                if (validation[i] == 0)
                {
                    Assert.True(bag.TryTake(out value), String.Format("Add/Take failed, the list is not empty and TryTake returned false; thread count={0}", threadsCount));
                }
                else
                {
                    Assert.Equal(1, validation[i]);
                }
            }

            Assert.False(bag.Count > 0 || bag.TryTake(out value), String.Format("Add/Take failed, this list is not empty after all remove operations; thread count={0}", threadsCount));
        }

        [Fact]
        public static void RTest6_GetEnumerator()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();

            // Empty bag should not enumerate
            Assert.Empty(bag);

            for (int i = 0; i < 100; i++)
            {
                bag.Add(i);
            }

            int count = 0;
            foreach (int x in bag)
            {
                count++;
            }

            Assert.Equal(count, bag.Count);
        }

        [Fact]
        public static void RTest7_BugFix575975()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(new ConcurrentBag<int>());
            bool succeeded = true;
            Task[] threads = new Task[4];
            for (int t = 0; t < threads.Length; t++)
            {
                threads[t] = Task.Factory.StartNew((obj) =>
                {
                    int index = (int)obj;
                    for (int i = 0; i < 100000; i++)
                    {
                        if (index < threads.Length / 2)
                        {
                            int k = 0;
                            for (int j = 0; j < 1000; j++)
                                k++;
                            bc.Add(i);
                        }
                        else
                        {
                            try
                            {
                                bc.Take();
                            }
                            catch // Take must not fail
                            {
                                succeeded = false;
                                break;
                            }
                        }
                    }

                }, t, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            Task.WaitAll(threads);
            Assert.True(succeeded);
        }

        [Fact]
        public static void RTest8_Interfaces()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            //IPCC
            IProducerConsumerCollection<int> ipcc = bag as IProducerConsumerCollection<int>;
            Assert.False(ipcc == null, "RTest8_Interfaces:  ConcurrentBag<T> doesn't implement IPCC<T>");
            Assert.True(ipcc.TryAdd(1), "RTest8_Interfaces:  IPCC<T>.TryAdd failed");
            Assert.Equal(1, bag.Count);

            int result = -1;
            Assert.True(ipcc.TryTake(out result), "RTest8_Interfaces:  IPCC<T>.TryTake failed");
            Assert.True(1 == result, "RTest8_Interfaces:  IPCC<T>.TryTake failed");
            Assert.Equal(0, bag.Count);

            //ICollection
            ICollection collection = bag as ICollection;
            Assert.False(collection == null, "RTest8_Interfaces:  ConcurrentBag<T> doesn't implement ICollection");
            Assert.False(collection.IsSynchronized, "RTest8_Interfaces:  IsSynchronized returned true");

            //IEnumerable
            IEnumerable enumerable = bag as IEnumerable;
            Assert.False(enumerable == null, "RTest8_Interfaces:  ConcurrentBag<T> doesn't implement IEnumerable");
            // Empty bag shouldn't enumerate.
            Assert.Empty(enumerable);
        }

        [Fact]
        public static void RTest8_Interfaces_Negative()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            //IPCC
            IProducerConsumerCollection<int> ipcc = bag as IProducerConsumerCollection<int>;
            ICollection collection = bag as ICollection;
            Assert.Throws<NotSupportedException>(() => { object obj = collection.SyncRoot; });
        }

        [Fact]
        public static void RTest9_ToArray()
        {
            var bag = new ConcurrentBag<int>();

            Assert.NotNull(bag.ToArray());
            Assert.Equal(0, bag.ToArray().Length);

            int[] allItems = new int[10000];
            for (int i = 0; i < allItems.Length; i++)
                allItems[i] = i;

            bag = new ConcurrentBag<int>(allItems);
            int failCount = 0;
            Task[] tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                    {
                        int[] array = bag.ToArray();
                        if (array == null || array.Length != 10000)
                            Interlocked.Increment(ref failCount);
                    });
            }

            Task.WaitAll(tasks);
            Assert.True(0 == failCount, "RTest9_ToArray:  One or more thread failed to get the correct bag items from ToArray");
        }

        [Fact]
        public static void RTest10_DebuggerAttributes()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new ConcurrentBag<int>());
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(new ConcurrentBag<int>());
        }

        #region Helper Methods / Classes

        private struct Interval
        {
            public Interval(int start, int end)
            {
                m_start = start;
                m_end = end;
            }
            internal int m_start;
            internal int m_end;
        }

        /// <summary>
        /// Create a ComcurrentBag object
        /// </summary>
        /// <param name="numbers">number of the elements in the bag</param>
        /// <returns>The bag object</returns>
        private static ConcurrentBag<int> CreateBag(int numbers)
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            for (int i = 0; i < numbers; i++)
            {
                bag.Add(i);
            }
            return bag;
        }

        private static void Add(ConcurrentBag<int> bag, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                bag.Add(i);
            }
        }

        private static void Take(ConcurrentBag<int> bag, int count, int[] validation)
        {
            for (int i = 0; i < count; i++)
            {
                int value = -1;

                if (bag.TryTake(out value) && validation != null)
                {
                    Interlocked.Increment(ref validation[value]);
                }
            }
        }

        #endregion
    }
}
