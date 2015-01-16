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
    public class ConcurrentDictionaryTests
    {
        [Fact]
        public static void TestConcurrentDictionaryBasic()
        {
            ConcurrentDictionary<int, int> cd = new ConcurrentDictionary<int, int>();

            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                var ret = cd.TryAdd(1, 11);
                if (!ret)
                {
                    ret = cd.TryUpdate(1, 11, 111);
                    Assert.True(ret);
                }

                ret = cd.TryAdd(2, 22);
                if (!ret)
                {
                    ret = cd.TryUpdate(2, 22, 222);
                    Assert.True(ret);
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            tks[1] = Task.Factory.StartNew(() =>
            {
                var ret = cd.TryAdd(2, 222);
                if (!ret)
                {
                    ret = cd.TryUpdate(2, 222, 22);
                    Assert.True(ret);
                }

                ret = cd.TryAdd(1, 111);
                if (!ret)
                {
                    ret = cd.TryUpdate(1, 111, 11);
                    Assert.True(ret);
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Add1()
        {
            RunDictionaryTest_Add1(1, 1, 1, 100000);
            RunDictionaryTest_Add1(5, 1, 1, 100000);
            RunDictionaryTest_Add1(1, 1, 2, 50000);
            RunDictionaryTest_Add1(1, 1, 5, 20000);
            RunDictionaryTest_Add1(4, 0, 4, 20000);
            RunDictionaryTest_Add1(16, 31, 4, 20000);
            RunDictionaryTest_Add1(64, 5, 5, 50000);
            RunDictionaryTest_Add1(5, 5, 5, 250000);
        }

        private static void RunDictionaryTest_Add1(int cLevel, int initSize, int threads, int addsPerThread)
        {
            ConcurrentDictionary<int, int> dictConcurrent = new ConcurrentDictionary<int, int>(cLevel, 1);
            IDictionary<int, int> dict = dictConcurrent;

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < addsPerThread; j++)
                            {
                                dict.Add(j + ii * addsPerThread, -(j + ii * addsPerThread));
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in dict)
            {
                if (pair.Key != -pair.Value)
                {
                    Console.WriteLine("* RunDictionaryTest_Add1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})", cLevel, initSize, threads, addsPerThread);
                    Assert.False(true, "  > Invalid value for some key in the dictionary.");
                }
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict)
                gotKeys.Add(pair.Key);

            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            int itemCount = threads * addsPerThread;
            for (int i = 0; i < itemCount; i++)
                expectKeys.Add(i);
            if (gotKeys.Count != expectKeys.Count)
            {
                Console.WriteLine("* RunDictionaryTest_Add1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})", cLevel, initSize, threads, addsPerThread);
                Assert.False(true, "  > gotKeys is not the same length as expectedKeys");
            }

            for (int i = 0; i < expectKeys.Count; i++)
            {
                if (!(expectKeys[i].Equals(gotKeys[i])))
                {
                    Console.WriteLine("* RunDictionaryTest_Add1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})", cLevel, initSize, threads, addsPerThread);
                    Assert.False(true, "  > The set of keys in the dictionary is are not the same as the expected.");
                }
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = threads * addsPerThread;
            if (dict.Count != expectedCount || dictConcurrent.ToArray().Length != expectedCount)
            {
                Console.WriteLine("* RunDictionaryTest_Add1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})", cLevel, initSize, threads, addsPerThread);
                Assert.False(true, "  > Incorrect count of elements reported for the dictionary.");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Update1()
        {
            RunDictionaryTest_Update1(1, 1, 100000);
            RunDictionaryTest_Update1(5, 1, 100000);
            RunDictionaryTest_Update1(1, 2, 50000);
            RunDictionaryTest_Update1(1, 5, 20001);
            RunDictionaryTest_Update1(4, 4, 20001);
            RunDictionaryTest_Update1(15, 5, 20001);
            RunDictionaryTest_Update1(64, 5, 50000);
            RunDictionaryTest_Update1(5, 5, 250000);
        }

        private static void RunDictionaryTest_Update1(int cLevel, int threads, int updatesPerThread)
        {
            IDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            for (int i = 1; i <= updatesPerThread; i++) dict[i] = i;

            int running = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 1; j <= updatesPerThread; j++)
                            {
                                dict[j] = (ii + 2) * j;
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in dict)
            {
                var div = pair.Value / pair.Key;
                var rem = pair.Value % pair.Key;
                if (rem != 0 || div < 2 || div > threads + 1)
                {
                    Console.WriteLine("* RunDictionaryTest_Update1(cLevel={0}, threads={1}, updatesPerThread={2})", cLevel, threads, updatesPerThread);
                    Assert.False(true, "  > Invalid value for some key in the dictionary.");
                }
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict)
                gotKeys.Add(pair.Key);
            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            for (int i = 1; i <= updatesPerThread; i++)
                expectKeys.Add(i);

            if (gotKeys.Count != expectKeys.Count)
            {
                Console.WriteLine("* RunDictionaryTest_Update1(cLevel={0}, threads={1}, updatesPerThread={2})", cLevel, threads, updatesPerThread);
                Assert.False(true, "  > gotKeys is not the same length as expectedKeys");
            }

            for (int i = 0; i < expectKeys.Count; i++)
            {
                if (!(expectKeys[i].Equals(gotKeys[i])))
                {
                    Console.WriteLine("* RunDictionaryTest_Update1(cLevel={0}, threads={1}, updatesPerThread={2})", cLevel, threads, updatesPerThread);
                    Assert.False(true, "  > The set of keys in the dictionary is are not the same as the expected.");
                }
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Read1()
        {
            RunDictionaryTest_Read1(1, 1, 100000);
            RunDictionaryTest_Read1(5, 1, 100000);
            RunDictionaryTest_Read1(1, 2, 50000);
            RunDictionaryTest_Read1(1, 5, 20001);
            RunDictionaryTest_Read1(4, 4, 20001);
            RunDictionaryTest_Read1(15, 5, 20001);
            RunDictionaryTest_Read1(64, 5, 50000);
            RunDictionaryTest_Read1(5, 5, 250000);
        }

        private static void RunDictionaryTest_Read1(int cLevel, int threads, int readsPerThread)
        {
            IDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            for (int i = 0; i < readsPerThread; i += 2) dict[i] = i;

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < readsPerThread; j++)
                            {
                                int val = 0;
                                if (dict.TryGetValue(j, out val))
                                {
                                    if (j % 2 == 1 || j != val)
                                    {
                                        Console.WriteLine("* RunDictionaryTest_Read1(cLevel={0}, threads={1}, readsPerThread={2})", cLevel, threads, readsPerThread);
                                        Assert.False(true, "  > FAILED. Invalid element in the dictionary.");
                                    }
                                }
                                else
                                {
                                    if (j % 2 == 0)
                                    {
                                        Console.WriteLine("* RunDictionaryTest_Read1(cLevel={0}, threads={1}, readsPerThread={2})", cLevel, threads, readsPerThread);
                                        Assert.False(true, "  > FAILED. Element missing from the dictionary");
                                    }
                                }
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Remove1()
        {
            RunDictionaryTest_Remove1(1, 1, 100000);
            RunDictionaryTest_Remove1(5, 1, 10000);
            RunDictionaryTest_Remove1(1, 5, 20001);
            RunDictionaryTest_Remove1(4, 4, 20001);
            RunDictionaryTest_Remove1(15, 5, 20001);
            RunDictionaryTest_Remove1(64, 5, 50000);
        }

        private static void RunDictionaryTest_Remove1(int cLevel, int threads, int removesPerThread)
        {
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);
            string methodparameters = string.Format("* RunDictionaryTest_Remove1(cLevel={0}, threads={1}, removesPerThread={2})", cLevel, threads, removesPerThread);
            int N = 2 * threads * removesPerThread;

            for (int i = 0; i < N; i++) dict[i] = -i;

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys

            int running = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < removesPerThread; j++)
                            {
                                int value;
                                int key = 2 * (ii + j * threads);
                                if (!dict.TryRemove(key, out value))
                                {
                                    Console.WriteLine(methodparameters);
                                    Assert.False(true, "  > Failed to remove an element, which should be in the dictionary.");
                                }

                                if (value != -key)
                                {
                                    Console.WriteLine(methodparameters);
                                    Assert.False(true, "  > Invalid value for some key in the dictionary.");
                                }
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in dict)
            {
                if (pair.Key != -pair.Value)
                {
                    Console.WriteLine(methodparameters);
                    Assert.False(true, "  > Invalid value for some key in the dictionary.");
                }
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict)
                gotKeys.Add(pair.Key);
            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            for (int i = 0; i < (threads * removesPerThread); i++)
                expectKeys.Add(2 * i + 1);

            if (gotKeys.Count != expectKeys.Count)
            {
                Console.WriteLine(methodparameters);
                Assert.False(true, "  > gotKeys is not the same length as expectedKeys");
            }

            for (int i = 0; i < expectKeys.Count; i++)
            {
                if (!(expectKeys[i].Equals(gotKeys[i])))
                {
                    Console.WriteLine(methodparameters);
                    Assert.False(true, "  > The set of keys in the dictionary is are not the same as the expected.");
                }
            }

            // Finally, let's verify that the count is reported correctly.
            if (dict.Count != expectKeys.Count || dict.ToArray().Length != expectKeys.Count)
            {
                Console.WriteLine(methodparameters);
                Assert.False(true, "  > Incorrect count of elements reported for the dictionary.");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Remove2()
        {
            RunDictionaryTest_Remove2(1);
            RunDictionaryTest_Remove2(10);
            RunDictionaryTest_Remove2(50000);
        }

        private static void RunDictionaryTest_Remove2(int removesPerThread)
        {
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>();

            for (int i = 0; i < removesPerThread; i++) dict[i] = -i;

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys.

            int running = 2;

            bool[][] seen = new bool[2][];
            for (int i = 0; i < 2; i++) seen[i] = new bool[removesPerThread];

            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int t = 0; t < 2; t++)
                {
                    int thread = t;
                    Task.Run(
                        () =>
                        {
                            for (int key = 0; key < removesPerThread; key++)
                            {
                                int value;
                                if (dict.TryRemove(key, out value))
                                {
                                    seen[thread][key] = true;

                                    if (value != -key)
                                    {
                                        Console.WriteLine("* RunDictionaryTest_Remove2(removesPerThread={0})", removesPerThread);
                                        Assert.False(true, "  > FAILED. Invalid value for some key in the dictionary.");
                                    }
                                }
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            if (dict.Count != 0)
            {
                Console.WriteLine("* RunDictionaryTest_Remove2(removesPerThread={0})", removesPerThread);
                Assert.False(true, "  > FAILED. Expected the dictionary to be empty.");
            }

            for (int i = 0; i < removesPerThread; i++)
            {
                if (seen[0][i] == seen[1][i])
                {
                    Console.WriteLine("* RunDictionaryTest_Remove2(removesPerThread={0})", removesPerThread);
                    Assert.False(true, "  > FAILED. Two threads appear to have removed the same element.");
                }
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Remove3()
        {
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>();

            dict[99] = -99;

            ICollection<KeyValuePair<int, int>> col = dict;

            // Make sure we cannot "remove" a key/value pair which is not in the dictionary
            for (int i = 0; i < 1000; i++)
            {
                if (i != 99)
                {
                    if (col.Remove(new KeyValuePair<int, int>(i, -99)) || col.Remove(new KeyValuePair<int, int>(99, -i)))
                    {
                        Assert.False(true, "RunDictionaryTest_Remove3:  FAILED.  > Removed a key/value pair which was not supposed to be in the dictionary.");
                    }
                }
            }

            // Can we remove a key/value pair successfully?
            if (!col.Remove(new KeyValuePair<int, int>(99, -99)))
            {
                Assert.False(true, "RunDictionaryTest_Remove3:  FAILED.  > Failed to remove a key/value pair which was supposed to be in the dictionary.");
            }

            // Make sure the key/value pair is gone
            if (col.Remove(new KeyValuePair<int, int>(99, -99)))
            {
                Assert.False(true, "RunDictionaryTest_Remove3:  FAILED.  > Removed a key/value pair which was not supposed to be in the dictionary.");
            }

            // And that the dictionary is empty. We will check the count in a few different ways:
            if (dict.Count != 0 || dict.ToArray().Length != 0)
            {
                Assert.False(true, "RunDictionaryTest_Remove3:  FAILED.  > Incorrect count of elements reported for the dictionary.");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest()
        {
            RunDictionaryTest(1, 1, 1, 100000, Method.GetOrAdd);
            RunDictionaryTest(5, 1, 1, 100000, Method.GetOrAdd);
            RunDictionaryTest(1, 1, 2, 50000, Method.GetOrAdd);
            RunDictionaryTest(1, 1, 5, 20000, Method.GetOrAdd);
            RunDictionaryTest(4, 0, 4, 20000, Method.GetOrAdd);
            RunDictionaryTest(16, 31, 4, 20000, Method.GetOrAdd);
            RunDictionaryTest(64, 5, 5, 50000, Method.GetOrAdd);
            RunDictionaryTest(5, 5, 5, 250000, Method.GetOrAdd);

            RunDictionaryTest(1, 1, 1, 100000, Method.AddOrUpdate);
            RunDictionaryTest(5, 1, 1, 100000, Method.AddOrUpdate);
            RunDictionaryTest(1, 1, 2, 50000, Method.AddOrUpdate);
            RunDictionaryTest(1, 1, 5, 20000, Method.AddOrUpdate);
            RunDictionaryTest(4, 0, 4, 20000, Method.AddOrUpdate);
            RunDictionaryTest(16, 31, 4, 20000, Method.AddOrUpdate);
            RunDictionaryTest(64, 5, 5, 50000, Method.AddOrUpdate);
            RunDictionaryTest(5, 5, 5, 250000, Method.AddOrUpdate);
        }

        private static void RunDictionaryTest(int cLevel, int initSize, int threads, int addsPerThread, Method testMethod)
        {
            string methodparameters = string.Format("* RunDictionaryTest_{0}, Level={1}, initSize={2}, threads={3}, addsPerThread={4})",
                PrintTestMethod(testMethod), cLevel, initSize, threads, addsPerThread);

            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    Task.Run(
                        () =>
                        {
                            for (int j = 0; j < addsPerThread; j++)
                            {
                                if (testMethod == Method.GetOrAdd)
                                {
                                    //call either of the two overloads of GetOrAdd
                                    if (j + ii % 2 == 0)
                                    {
                                        dict.GetOrAdd(j, -j);
                                    }
                                    else
                                    {
                                        dict.GetOrAdd(j, x => -x);
                                    }
                                }
                                else if (testMethod == Method.AddOrUpdate)
                                {
                                    if (j + ii % 2 == 0)
                                    {
                                        dict.AddOrUpdate(j, -j, (k, v) => -j);
                                    }
                                    else
                                    {
                                        dict.AddOrUpdate(j, (k) => -k, (k, v) => -k);
                                    }
                                }
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            foreach (var pair in dict)
            {
                if (pair.Key != -pair.Value)
                {
                    Assert.False(true, methodparameters + "  > FAILED.  Invalid value for some key in the dictionary.");
                }
            }

            List<int> gotKeys = new List<int>();
            foreach (var pair in dict)
                gotKeys.Add(pair.Key);
            gotKeys.Sort();

            List<int> expectKeys = new List<int>();
            for (int i = 0; i < addsPerThread; i++)
                expectKeys.Add(i);

            if (gotKeys.Count != expectKeys.Count)
            {
                Assert.False(true, methodparameters + "  > FAILED.  gotKeys is not the same length as expectedKeys");
            }

            for (int i = 0; i < expectKeys.Count; i++)
            {
                if (!(expectKeys[i].Equals(gotKeys[i])))
                {
                    Assert.False(true, methodparameters + "  > FAILED.  The set of keys in the dictionary is are not the same as the expected.");
                }
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = addsPerThread;
            int count1 = dict.Count;
            int count2 = dict.ToArray().Length;
            if (count1 != expectedCount || count2 != expectedCount)
            {
                Assert.False(true, String.Format(
                    methodparameters + "  > FAILED.  Incorrect count of elements reported for the dictionary. Expected {0}, Dict.Count {1}, ToArray.Length {2}",
                    expectedCount, count1, count2));
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_BugFix669376()
        {
            var cd = new ConcurrentDictionary<string, int>(new OrdinalStringComparer());
            cd["test"] = 10;
            if (!cd.ContainsKey("TEST"))
            {
                Assert.False(true, "RunDictionaryTest_BugFix669376:  > Customized comparer didn't work");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_BadComparer()
        {
            RunDictionaryTest_BadComparer(-1);
            RunDictionaryTest_BadComparer(1024);
        }

        /// <summary>
        /// Uses Reflection to test Comparer
        /// </summary>
        private static void RunDictionaryTest_BadComparer(int lockCount)
        {
            ConcurrentDictionary<int, int> cd = (lockCount < 0)
                ? new ConcurrentDictionary<int, int>(new BadComparer())
                : new ConcurrentDictionary<int, int>(lockCount, 31, new BadComparer());
            for (int i = 0; i < 1024; i++) cd[i] = i;

            if (cd.Count != 1024)
            {
                Assert.False(true, "* RunDictionaryTest_BadComparer(LockCount=" + lockCount + ")" + "  > ConcurrentDictionary contained a wrong number of elements.");
            }

        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Constructor()
        {
            var dictionary = new ConcurrentDictionary<int, int>(new[] { new KeyValuePair<int, int>(1, 1) });
            Assert.False(dictionary.IsEmpty,
               "RunDictionaryTest_Constructor:  FAILED.  IsEmpty returned true when the dictionary has items");
            Assert.True(dictionary.Keys.Count == 1,
               "RunDictionaryTest_Constructor:  FAILED.  Keys.Count doesn't match the number of keys");
            Assert.True(dictionary.Values.Count == 1,
               "RunDictionaryTest_Constructor:  FAILED.  Values.Count doesn't match the number of valus");
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Constructor_Negative()
        {
            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>((ICollection<KeyValuePair<int, int>>)null));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null collection is passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>((IEqualityComparer<int>)null));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null IEqualityComparer is passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>((ICollection<KeyValuePair<int, int>>)null, EqualityComparer<int>.Default));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null collection and non null IEqualityComparer passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>(new[] { new KeyValuePair<int, int>(1, 1) }, null));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when non null collection and null IEqualityComparer passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<string, int>(new[] { new KeyValuePair<string, int>(null, 1) }));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when collection has null key passed");
            Assert.Throws<ArgumentException>(
               () => new ConcurrentDictionary<int, int>(new[] { new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(1, 2) }));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw AE when collection has duplicate keys passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>(1, null, EqualityComparer<int>.Default));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null collection is passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>(1, new[] { new KeyValuePair<int, int>(1, 1) }, null));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null comparer is passed");

            Assert.Throws<ArgumentNullException>(
               () => new ConcurrentDictionary<int, int>(1, 1, null));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw ANE when null comparer is passed");

            Assert.Throws<ArgumentOutOfRangeException>(
               () => new ConcurrentDictionary<int, int>(0, 10));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw AORE when <1 concurrencyLevel passed");

            Assert.Throws<ArgumentOutOfRangeException>(
               () => new ConcurrentDictionary<int, int>(-1, 0));
            // "RunDictionaryTest_Constructor:  FAILED.  Constructor didn't throw AORE when < 0 capacity passed");
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Exceptions()
        {
            var dictionary = new ConcurrentDictionary<string, int>();
            Assert.Throws<ArgumentNullException>(
               () => dictionary.TryAdd(null, 0));
            //  "RunDictionaryTest_Exceptions:  FAILED.  TryAdd didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentNullException>(
               () => dictionary.ContainsKey(null));
            // "RunDictionaryTest_Exceptions:  FAILED.  Contains didn't throw ANE when null key is passed");

            int item;
            Assert.Throws<ArgumentNullException>(
               () => dictionary.TryRemove(null, out item));
            //  "RunDictionaryTest_Exceptions:  FAILED.  TryRmove didn't throw ANE when null key is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary.TryGetValue(null, out item));
            // "RunDictionaryTest_Exceptions:  FAILED.  TryGetValue didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentNullException>(
               () => { var x = dictionary[null]; });
            // "RunDictionaryTest_Exceptions:  FAILED.  this[] didn't throw ANE when null key is passed");
            Assert.Throws<KeyNotFoundException>(
               () => { var x = dictionary["1"]; });
            // "RunDictionaryTest_Exceptions:  FAILED.  this[] TryGetValue didn't throw KeyNotFoundException!");

            Assert.Throws<ArgumentNullException>(
               () => dictionary[null] = 1);
            // "RunDictionaryTest_Exceptions:  FAILED.  this[] didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentNullException>(
               () => dictionary.GetOrAdd(null, (k) => 0));
            // "RunDictionaryTest_Exceptions:  FAILED.  GetOrAdd didn't throw ANE when null key is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary.GetOrAdd("1", null));
            // "RunDictionaryTest_Exceptions:  FAILED.  GetOrAdd didn't throw ANE when null valueFactory is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary.GetOrAdd(null, 0));
            // "RunDictionaryTest_Exceptions:  FAILED.  GetOrAdd didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentNullException>(
               () => dictionary.AddOrUpdate(null, (k) => 0, (k, v) => 0));
            // "RunDictionaryTest_Exceptions:  FAILED.  AddOrUpdate didn't throw ANE when null key is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary.AddOrUpdate("1", null, (k, v) => 0));
            // "RunDictionaryTest_Exceptions:  FAILED.  AddOrUpdate didn't throw ANE when null updateFactory is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary.AddOrUpdate(null, (k) => 0, null));
            // "RunDictionaryTest_Exceptions:  FAILED.  AddOrUpdate didn't throw ANE when null addFactory is passed");

            dictionary.TryAdd("1", 1);
            Assert.Throws<ArgumentException>(
               () => ((IDictionary<string, int>)dictionary).Add("1", 2));
            // "RunDictionaryTest_Exceptions:  FAILED.  IDictionary didn't throw AE when duplicate key is passed");
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_IDictionary()
        {
            IDictionary dictionary = new ConcurrentDictionary<string, int>();
            Assert.False(dictionary.IsReadOnly,
               "RunDictionaryTest_IDictionary:  FAILED.  IsReadOnly returned true!");

            foreach (var entry in dictionary)
            {
                Assert.False(true, "RunDictionaryTest_IDictionary:  FAILED.  GetEnumerator retuned items when the dictionary is empty");
            }

            for (int i = 0; i < 10; i++)
                dictionary.Add(i.ToString(), i);

            Assert.True(dictionary.Count == 10, "RunDictionaryTest_IDictionary:  FAILED.  Count returned a wrong value after Add");

            //test contains
            Assert.False(dictionary.Contains(1), "RunDictionaryTest_IDictionary:  FAILED.  Contain retuned true for incorrect key type");
            Assert.False(dictionary.Contains("100"), "RunDictionaryTest_IDictionary:  FAILED.  Contain retuned true for incorrect key");
            Assert.True(dictionary.Contains("1"), "RunDictionaryTest_IDictionary:  FAILED.  Contain retuned false for correct key");

            //test GetEnumerator
            int count = 0;
            foreach (var obj in dictionary)
            {
                DictionaryEntry entry = (DictionaryEntry)obj;
                string key = (string)entry.Key;
                int value = (int)entry.Value;
                int expectedValue = int.Parse(key);
                Assert.True(value == expectedValue,
                    String.Format("RunDictionaryTest_IDictionary:  FAILED.  Unexpected value returned from GetEnumerator, expected {0}, actual {1}", value, expectedValue));
                count++;
            }
            Assert.True(count == 10, "RunDictionaryTest_IDictionary:  FAILED.  GetEnumerator didn't return the correct items");

            Assert.True(dictionary.Keys.Count == 10, "RunDictionaryTest_IDictionary:  FAILED.  Keys property didn't return the correct keys count");
            Assert.True(dictionary.Values.Count == 10, "RunDictionaryTest_IDictionary:  FAILED.  Values property didn't return the correct values count");

            //Test Remove
            dictionary.Remove("9");
            Assert.True(dictionary.Count == 9, "Count returned a wrong value after Remove");

            //Test this[]
            for (int i = 0; i < dictionary.Count; i++)
                Assert.True((int)dictionary[i.ToString()] == i, "RunDictionaryTest_IDictionary:  FAILED.  this[] getter returned a wrong value");

            dictionary["1"] = 100; // try a valid setter
            Assert.True((int)dictionary["1"] == 100, "RunDictionaryTest_IDictionary:  FAILED.  this[] getter returned a wrong value");

            //nonsexist key
            Assert.True(dictionary["NotAKey"] == null, "RunDictionaryTest_IDictionary:  FAILED.  this[] getter returned a non null value for invalid key");

        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_IDictionary_Negative()
        {
            IDictionary dictionary = new ConcurrentDictionary<string, int>();
            Assert.Throws<ArgumentNullException>(
               () => dictionary.Add(null, 1));
            // "RunDictionaryTest_IDictionary:  FAILED.  Add didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentException>(
               () => dictionary.Add(1, 1));
            // "RunDictionaryTest_IDictionary:  FAILED.  Add didn't throw AE when incorrect key type is passed");

            Assert.Throws<ArgumentException>(
               () => dictionary.Add("1", "1"));
            // "RunDictionaryTest_IDictionary:  FAILED.  Add didn't throw AE when incorrect value type is passed");

            Assert.Throws<ArgumentNullException>(
               () => dictionary.Contains(null));
            // "RunDictionaryTest_IDictionary:  FAILED.  Contain didn't throw ANE when null key is passed");

            //Test Remove
            Assert.Throws<ArgumentNullException>(
               () => dictionary.Remove(null));
            // "RunDictionaryTest_IDictionary:  FAILED.  Remove didn't throw ANE when null key is passed");

            //Test this[]
            Assert.Throws<ArgumentNullException>(
               () => { object val = dictionary[null]; });
            // "RunDictionaryTest_IDictionary:  FAILED.  this[] getter didn't throw ANE when null key is passed");
            Assert.Throws<ArgumentNullException>(
               () => dictionary[null] = 0);
            // "RunDictionaryTest_IDictionary:  FAILED.  this[] setter didn't throw ANE when null key is passed");

            Assert.Throws<ArgumentException>(
               () => dictionary[1] = 0);
            // "RunDictionaryTest_IDictionary:  FAILED.  this[] setter didn't throw AE when invalid key type is passed");

            Assert.Throws<ArgumentException>(
               () => dictionary["1"] = "0");
            // "RunDictionaryTest_IDictionary:  FAILED.  this[] setter didn't throw AE when invalid value type is passed");
        }

        [Fact]
        public static void RunDictionaryTest_ICollection()
        {
            ICollection dictionary = new ConcurrentDictionary<int, int>();
            Assert.False(dictionary.IsSynchronized, "RunDictionaryTest_ICollection:  FAILED.  IsSynchronized returned true!");

            //add one item to the dictionary
            ((ConcurrentDictionary<int, int>)dictionary).TryAdd(1, 1);

            var objectArray = new Object[1];
            dictionary.CopyTo(objectArray, 0);
            Assert.True(((KeyValuePair<int, int>)objectArray[0]).Key == 1,
               "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, Key doesn't match");
            Assert.True(((KeyValuePair<int, int>)objectArray[0]).Value == 1, "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, value doesn't match");

            var keyValueArray = new KeyValuePair<int, int>[1];
            dictionary.CopyTo(keyValueArray, 0);
            Assert.True(keyValueArray[0].Key == 1, "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, Key doesn't match");
            Assert.True(keyValueArray[0].Value == 1, "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, value doesn't match");

            var entryArray = new DictionaryEntry[1];
            dictionary.CopyTo(entryArray, 0);
            Assert.True((int)entryArray[0].Key == 1, "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, Key doesn't match");
            Assert.True((int)entryArray[0].Value == 1, "RunDictionaryTest_ICollection:  FAILED.  CopyTo returned incorrect result, value doesn't match");
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_ICollection_Negative()
        {
            ICollection dictionary = new ConcurrentDictionary<int, int>();
            Assert.False(dictionary.IsSynchronized, "RunDictionaryTest_ICollection:  FAILED.  IsSynchronized returned true!");

            Assert.Throws<NotSupportedException>(() => { var obj = dictionary.SyncRoot; });
            // "RunDictionaryTest_ICollection:  FAILED.  SyncRoot property didn't throw");
            Assert.Throws<ArgumentNullException>(() => dictionary.CopyTo(null, 0));
            // "RunDictionaryTest_ICollection:  FAILED.  CopyTo didn't throw ANE when null Array is passed");
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.CopyTo(new object[] { }, -1));
            // "RunDictionaryTest_ICollection:  FAILED.  CopyTo didn't throw AORE when negative index passed");

            //add one item to the dictionary
            ((ConcurrentDictionary<int, int>)dictionary).TryAdd(1, 1);
            Assert.Throws<ArgumentException>(() => dictionary.CopyTo(new object[] { }, 0));
            // "RunDictionaryTest_ICollection:  FAILED.  CopyTo didn't throw AE when the Array size is smaller than the dictionary count");
        }

        // [Fact] - Require TaskCreationOptions.LongRunning which will cause deadlock when running by xUnit
        public static void RunDictionaryTest_TryUpdate()
        {
            var dictionary = new ConcurrentDictionary<string, int>();
            Assert.Throws<ArgumentNullException>(
               () => dictionary.TryUpdate(null, 0, 0));
            // "RunDictionaryTest_TryUpdate:  FAILED.  TryUpdte didn't throw ANE when null key is passed");

            for (int i = 0; i < 10; i++)
                dictionary.TryAdd(i.ToString(), i);

            for (int i = 0; i < 10; i++)
            {
                Assert.True(dictionary.TryUpdate(i.ToString(), i + 1, i), "RunDictionaryTest_TryUpdate:  FAILED.  TryUpdate failed!");
                Assert.Equal(i + 1, dictionary[i.ToString()]);
            }

            //test TryUpdate concurrently
            dictionary.Clear();
            for (int i = 0; i < 1000; i++)
                dictionary.TryAdd(i.ToString(), i);

            var mres = new ManualResetEventSlim();
            Task[] tasks = new Task[10];
            ThreadLocal<ThreadData> updatedKeys = new ThreadLocal<ThreadData>(true);
            for (int i = 0; i < tasks.Length; i++)
            {
                // We are creating the Task using TaskCreationOptions.LongRunning because...
                // there is no guarantee that the Task will be created on another thread.
                // There is also no guarantee that using this TaskCreationOption will force
                // it to be run on another thread.
                tasks[i] = Task.Factory.StartNew((obj) =>
                {
                    mres.Wait();
                    int index = (((int)obj) + 1) + 1000;
                    updatedKeys.Value = new ThreadData();
                    updatedKeys.Value.ThreadIndex = index;

                    for (int j = 0; j < dictionary.Count; j++)
                    {
                        if (dictionary.TryUpdate(j.ToString(), index, j))
                        {
                            if (dictionary[j.ToString()] != index)
                            {
                                updatedKeys.Value.Succeeded = false;
                                return;
                            }
                            updatedKeys.Value.Keys.Add(j.ToString());
                        }
                    }
                }, i, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            mres.Set();
            Task.WaitAll(tasks);

            int numberSucceeded = 0;
            int totalKeysUpdated = 0;
            foreach (var threadData in updatedKeys.Values)
            {
                totalKeysUpdated += threadData.Keys.Count;
                if (threadData.Succeeded)
                    numberSucceeded++;
            }

            Assert.True(numberSucceeded == tasks.Length, "One or more threads failed!");
            Assert.True(totalKeysUpdated == dictionary.Count,
               String.Format("RunDictionaryTest_TryUpdate:  FAILED.  The updated keys count doesn't match the dictionary count, expected {0}, actual {1}", dictionary.Count, totalKeysUpdated));
            foreach (var value in updatedKeys.Values)
            {
                for (int i = 0; i < value.Keys.Count; i++)
                    Assert.True(dictionary[value.Keys[i]] == value.ThreadIndex,
                       String.Format("RunDictionaryTest_TryUpdate:  FAILED.  The updated value doesn't match the thread index, expected {0} actual {1}", value.ThreadIndex, dictionary[value.Keys[i]]));
            }

            //test TryUpdate with non atomic values (intPtr > 8)
            var dict = new ConcurrentDictionary<int, Struct16>();
            dict.TryAdd(1, new Struct16(1, -1));
            Assert.True(dict.TryUpdate(1, new Struct16(2, -2), new Struct16(1, -1)), "RunDictionaryTest_TryUpdate:  FAILED.  TryUpdte failed for non atomic values ( > 8 bytes)");
        }

        [Fact]
        [OuterLoop]
        public static void RunDictionaryTest_Clear()
        {
            var dictionary = new ConcurrentDictionary<int, int>();
            for (int i = 0; i < 10; i++)
                dictionary.TryAdd(i, i);

            Assert.True(10 == dictionary.Count, "RunDictionaryTest_Clear: FAILED.  Count returned wrong value before Clear");
            dictionary.Clear();
            Assert.True(0 == dictionary.Count, "RunDictionaryTest_Clear: FAILED.  Count returned wrong value after Clear");
            int item;
            Assert.False(dictionary.TryRemove(1, out item), "RunDictionaryTest_Clear: FAILED.  TryRemove succeeded after Clear");
            Assert.True(dictionary.IsEmpty, "RunDictionaryTest_Clear: FAILED.  IsEmpty returned false after Clear");
        }

        #region Helper Classes and Methods

        private enum Method
        {
            GetOrAdd,
            AddOrUpdate
        }

        private class ThreadData
        {
            public int ThreadIndex;
            public bool Succeeded = true;
            public List<string> Keys = new List<string>();
        }

        private struct Struct16 : IEqualityComparer<Struct16>
        {
            public long L1, L2;
            public Struct16(long l1, long l2)
            {
                L1 = l1;
                L2 = l2;
            }

            public bool Equals(Struct16 x, Struct16 y)
            {
                return x.L1 == y.L1 && x.L2 == y.L2;
            }

            public int GetHashCode(Struct16 obj)
            {
                return (int)L1;
            }
        }

        private static string PrintTestMethod(Method testMethod)
        {
            switch (testMethod)
            {
                case (Method.GetOrAdd):
                    return "GetOrAdd";
                case (Method.AddOrUpdate):
                    return "AddOrUpdate";
                default:
                    return "";
            }
        }

        private class BadComparer : IEqualityComparer<int>
        {
            public int GetHashCode(int value)
            {
                return 0;
            }

            public bool Equals(int a, int b)
            {
                return a == b;
            }
        }

        private class OrdinalStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                var xlower = x.ToLowerInvariant();
                var ylower = y.ToLowerInvariant();
                return string.CompareOrdinal(xlower, ylower) == 0;
            }

            public int GetHashCode(string obj)
            {
                return 0;
            }
        }

        #endregion
    }
}
