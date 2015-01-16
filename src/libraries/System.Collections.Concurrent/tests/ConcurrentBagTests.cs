// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    /// <summary>The class that contains the unit tests of the LazyInit.</summary>
    public class ConcurrentBagTests
    {
        [Fact]
        public static void TestConcurrentBagBasic()
        {
            ConcurrentBag<int> cb = new ConcurrentBag<int>();
            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
                {
                    cb.Add(4);
                    cb.Add(5);
                    cb.Add(6);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            // Consume the items in the bag 
            tks[1] = Task.Factory.StartNew(() =>
                {
                    int item;
                    while (!cb.IsEmpty)
                    {
                        bool ret = cb.TryTake(out item);
                        Assert.True(ret);
                        // loose check
                        if (item != 4 && item != 5 && item != 6)
                        {
                            Assert.False(true, "Expected: 4|5|6; actual: " + item.ToString());
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest1_Ctor()
        {
            RunConcurrentBagTest1_Ctor(new int[] { 1, 2, 3 }, null);
            RunConcurrentBagTest1_Ctor(null, typeof(ArgumentNullException));
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest2_Add()
        {
            RunConcurrentBagTest2_Add(1, 10);
            RunConcurrentBagTest2_Add(64, 100);
            RunConcurrentBagTest2_Add(128, 1000);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest3_TakeOrPeek()
        {
            ConcurrentBag<int> bag = CreateBag(100);
            RunConcurrentBagTest3_TakeOrPeek(bag, 1, 100, true);

            bag = CreateBag(100);
            RunConcurrentBagTest3_TakeOrPeek(bag, 64, 10, false);

            bag = CreateBag(1000);
            RunConcurrentBagTest3_TakeOrPeek(bag, 128, 100, true);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest4_AddAndTake()
        {
            RunConcurrentBagTest4_AddAndTake(64);
            RunConcurrentBagTest4_AddAndTake(128);
            RunConcurrentBagTest4_AddAndTake(256);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest5_CopyTo()
        {
            ConcurrentBag<int> bag = CreateBag(10);

            RunConcurrentBagTest5_CopyTo(bag, new int[10], 0, null, true);

            RunConcurrentBagTest5_CopyTo(bag, new int[10], 0, null, false);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest5_CopyToExceptions()
        {
            ConcurrentBag<int> bag = CreateBag(10);
            RunConcurrentBagTest5_CopyTo(bag, null, 0, typeof(ArgumentNullException), true);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], -1, typeof(ArgumentOutOfRangeException), true);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], 10, typeof(ArgumentException), true);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], 8, typeof(ArgumentException), true);

            RunConcurrentBagTest5_CopyTo(bag, null, 0, typeof(ArgumentNullException), false);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], -1, typeof(ArgumentOutOfRangeException), false);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], 10, typeof(ArgumentException), false);
            RunConcurrentBagTest5_CopyTo(bag, new int[10], 8, typeof(ArgumentException), false);

            RunConcurrentBagTest5_CopyTo(bag, new int[10, 5], 8, typeof(ArgumentException), true);
        }

        /// <summary>
        /// Test bag constructor
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="exceptionType"></param>
        /// <param name="shouldThrow"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunConcurrentBagTest1_Ctor(int[] collection, Type exceptionType)
        {
            bool thrown = false;
            try
            {
                ConcurrentBag<int> bag = new ConcurrentBag<int>(collection);
                if (bag.IsEmpty != (collection.Length == 0))
                {
                    Assert.False(true, "RunConcurrentBagTest1_Ctor:  Constructor failed, IsEmpty doesn't match the given collection count.");
                }

                if (bag.Count != collection.Length)
                {
                    Assert.False(true, "RunConcurrentBagTest1_Ctor:  Constructor failed, the bag count doesn't match the given collection count.");
                }
            }
            catch (Exception e)
            {
                if (exceptionType != null && !e.GetType().Equals(exceptionType))
                {
                    Assert.False(true, "RunConcurrentBagTest1_Ctor:  Constructor failed, exceptions type do not match");
                }
                else if (exceptionType == null)
                {
                    Assert.False(true, "RunConcurrentBagTest1_Ctor:  Constructor failed, it threw un expected exception");
                }
                thrown = true;
            }
            if (exceptionType != null && !thrown)
            {
                Assert.False(true, "RunConcurrentBagTest1_Ctor:  Constructor failed, it didn't throw the expected exception");
            }
        }

        /// <summary>
        /// Test bag addition
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunConcurrentBagTest2_Add(int threadsCount, int itemsPerThread)
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

            if (failures > 0)
            {
                Console.WriteLine("* RunConcurrentBagTest1_Add(" + threadsCount + "," + itemsPerThread + ")");
                Assert.False(true, "Add failed, " + failures + " threads threw  unexpected exceptions");
            }
            if (bag.Count != itemsPerThread * threadsCount)
            {
                Console.WriteLine("* RunConcurrentBagTest1_Add(" + threadsCount + "," + itemsPerThread + ")");
                Assert.False(true, "Add failed, the bag count doesn't match the expected count");
            }
        }

        /// <summary>
        /// Test bag Take and Peek operations
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <param name="take"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunConcurrentBagTest3_TakeOrPeek(ConcurrentBag<int> bag, int threadsCount, int itemsPerThread, bool take)
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
                        try
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
                        catch
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                });
            }

            Task.WaitAll(threads);

            if (take)
            {
                if (bag.Count != bagCount - succeeded)
                {
                    Console.WriteLine("* RunConcurrentBagTest3_TakeOrPeek(" + threadsCount + "," + itemsPerThread + ")");
                    Assert.False(true, "TryTake failed, the remaining count doesn't match the expected count");
                }
            }
            else if (failures > 0)
            {
                Console.WriteLine("* RunConcurrentBagTest3_TakeOrPeek(" + threadsCount + "," + itemsPerThread + ")");
                Assert.False(true, "TryPeek failed, Unexpected exceptions has been thrown");
            }
        }

        /// <summary>
        /// Test parallel Add/Take, insert uniqe elements in the bag, and each element should be removed once
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunConcurrentBagTest4_AddAndTake(int threadsCount)
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

            int valu = -1;

            //validation
            for (int i = 0; i < validation.Length; i++)
            {
                if (validation[i] > 1)
                {
                    Console.WriteLine("* RunConcurrentBagTest4_AddAndTake(" + threadsCount + " )");
                    Assert.False(true, "Add/Take failed, item " + i + " has been taken more than one");
                }
                else if (validation[i] == 0)
                {
                    if (!bag.TryTake(out valu))
                    {
                        Console.WriteLine("* RunConcurrentBagTest4_AddAndTake(" + threadsCount + " )");
                        Assert.False(true, "Add/Take failed, the list is not empty and TryTake returned false");
                    }

                }
            }

            if (bag.Count > 0 || bag.TryTake(out valu))
            {
                Console.WriteLine("* RunConcurrentBagTest4_AddAndTake(" + threadsCount + " )");
                Assert.False(true, "Add/Take failed, this list is not empty after all remove operations");
            }
        }

        /// <summary>
        /// Test copyTo method
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="exceptionType"></param>
        /// <param name="shouldThrow"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunConcurrentBagTest5_CopyTo(ConcurrentBag<int> bag, Array array, int index, Type exceptionType, bool useICollection)
        {
            bool thrown = false;
            try
            {
                if (useICollection)
                {
                    ICollection collection = bag as ICollection;
                    collection.CopyTo(array, index);
                }
                else
                    bag.CopyTo((int[])array, index);
            }
            catch (Exception e)
            {
                if (exceptionType != null && !e.GetType().Equals(exceptionType))
                {
                    Console.WriteLine("* RunConcurrentBagTest5_CopyTo(" + array + " )");
                    Assert.False(true, "CopyTo failed, exceptions types do not match.");
                }
                else if (exceptionType == null)
                {
                    Console.WriteLine("* RunConcurrentBagTest5_CopyTo(" + array + " )");
                    Assert.False(true, "CopyTo failed, it threw unexpected exception." + e);
                }
                thrown = true;
            }

            if (exceptionType != null && !thrown)
            {
                Console.WriteLine("* RunConcurrentBagTest5_CopyTo(" + array + " )");
                Assert.False(true, "CopyTo failed, it didn't throw the expected exception.");
            }
        }

        /// <summary>
        /// Test enumeration
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest6_GetEnumerator()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            foreach (int x in bag)
            {
                Assert.False(true, "RunConcurrentBagTest6_GetEnumerator:  GetEnumeration failed, returned items when the bag is empty");
            }

            for (int i = 0; i < 100; i++)
            {
                bag.Add(i);
            }

            try
            {
                int count = 0;
                foreach (int x in bag)
                {
                    count++;
                }
                if (count != bag.Count)
                {
                    Assert.False(true, "RunConcurrentBagTest6_GetEnumerator:  GetEnumeration failed, the enumeration count doesn't match the bag count");
                }
            }
            catch
            {
                Assert.False(true, "RunConcurrentBagTest6_GetEnumerator:  GetEnumeration failed, it threw unexpected exception");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest7_BugFix575975()
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

            if (!succeeded)
                Assert.False(true, String.Format("RunConcurrentBagTest7_BugFix575975: {0}", succeeded ? "succeeded" : "failed"));
        }

        /// <summary>
        /// Test IPCC implementation
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest8_Interfaces()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            //IPCC
            IProducerConsumerCollection<int> ipcc = bag as IProducerConsumerCollection<int>;
            Assert.False(ipcc == null, "RunConcurrentBagTest8_Interfaces:  ConcurrentBag<T> doesn't implement IPCC<T>");
            Assert.True(ipcc.TryAdd(1), "RunConcurrentBagTest8_Interfaces:  IPCC<T>.TryAdd failed");
            Assert.True(1 == bag.Count, String.Format("RunConcurrentBagTest8_Interfaces:  The count doesn't match, expected 1, actual {0}", bag.Count));

            int result = -1;
            Assert.True(ipcc.TryTake(out result), "RunConcurrentBagTest8_Interfaces:  IPCC<T>.TryTake failed");
            Assert.True(1 == result, "RunConcurrentBagTest8_Interfaces:  IPCC<T>.TryTake failed");
            Assert.True(0 == bag.Count, String.Format("RunConcurrentBagTest8_Interfaces:  The count doesn't match, expected 1, actual {0}", bag.Count));

            //ICollection
            ICollection collection = bag as ICollection;
            Assert.False(collection == null, "RunConcurrentBagTest8_Interfaces:  ConcurrentBag<T> doesn't implement ICollection");
            Assert.False(collection.IsSynchronized, "RunConcurrentBagTest8_Interfaces:  IsSynchronized returned true");

            //IEnumerable
            IEnumerable enumerable = bag as IEnumerable;
            Assert.False(enumerable == null, "RunConcurrentBagTest8_Interfaces:  ConcurrentBag<T> doesn't implement IEnumerable");
            foreach (object o in enumerable)
            {
                Assert.True(false, "RunConcurrentBagTest8_Interfaces:  Enumerable returned items when the bag is empty");
            }
        }

        /// <summary>
        /// Test IPCC implementation
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest8_Interfaces_Negative()
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            //IPCC
            IProducerConsumerCollection<int> ipcc = bag as IProducerConsumerCollection<int>;
            ICollection collection = bag as ICollection;
            Assert.Throws<NotSupportedException>(
               () => { object obj = collection.SyncRoot; });
            // "RunConcurrentBagTest8_Interfaces:  SyncRoot didn't throw NotSupportedException");
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentBagTest9_ToArray()
        {
            var bag = new ConcurrentBag<int>();
            Assert.False(bag.ToArray() == null, "RunConcurrentBagTest9_ToArray:  Empty bag returned a null array");
            Assert.True(0 == bag.ToArray().Length, "RunConcurrentBagTest9_ToArray:  Empty bag returned a non empty array");

            int[] allItems = new int[10000];
            for (int i = 0; i < allItems.Length; i++)
                allItems[i] = i;

            bag = new ConcurrentBag<int>(allItems);
            int failCount = 0;
            Task[] tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                    {
                        int[] array = bag.ToArray();
                        if (array == null || array.Length != 10000)
                            Interlocked.Increment(ref failCount);
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            Task.WaitAll(tasks);
            Assert.True(0 == failCount, "RunConcurrentBagTest9_ToArray:  One or more thread failed to get the correct bag items from ToArray");
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
                int valu = -1;

                if (bag.TryTake(out valu) && validation != null)
                {
                    Interlocked.Increment(ref validation[valu]);
                }
            }
        }

        #endregion
    }
}
