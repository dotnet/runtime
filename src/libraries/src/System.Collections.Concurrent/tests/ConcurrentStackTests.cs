// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class ConcurrentStackTests
    {
        [Fact]
        public static void TestConcurrentStackBasic()
        {
            ConcurrentStack<int> cs = new ConcurrentStack<int>();
            cs.Push(1);

            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                cs.Push(2);
                cs.Push(3);
                cs.Push(4);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            tks[1] = Task.Factory.StartNew(() =>
            {
                int item1, item2;
                var ret1 = cs.TryPop(out item1);
                // at least one item
                Assert.True(ret1);
                var ret2 = cs.TryPop(out item2);
                // two item
                if (ret2)
                {
                    Assert.True(item1 > item2, String.Format("{0} should greater than {1}", item1, item2));
                }
                else // one item
                {
                    Assert.Equal(1, item1);
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest0_Empty()
        {
            RunConcurrentStackTest0_Empty(0);
            RunConcurrentStackTest0_Empty(16);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest1_PushAndPop()
        {
            RunConcurrentStackTest1_PushAndPop(0, 0);
            RunConcurrentStackTest1_PushAndPop(5, 0);
            RunConcurrentStackTest1_PushAndPop(5, 2);
            RunConcurrentStackTest1_PushAndPop(5, 5);
            RunConcurrentStackTest1_PushAndPop(1024, 512);
            RunConcurrentStackTest1_PushAndPop(1024, 1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest2_ConcPushAndPop()
        {
            RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 0);
            RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 1024 * 512);
            RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 1024 * 1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest3_Clear()
        {
            RunConcurrentStackTest3_Clear(0);
            RunConcurrentStackTest3_Clear(16);
            RunConcurrentStackTest3_Clear(1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest4_Enumerator()
        {
            RunConcurrentStackTest4_Enumerator(0);
            RunConcurrentStackTest4_Enumerator(16);
            RunConcurrentStackTest4_Enumerator(1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest5_CtorAndCopyToAndToArray()
        {
            RunConcurrentStackTest5_CtorAndCopyToAndToArray(0);
            RunConcurrentStackTest5_CtorAndCopyToAndToArray(16);
            RunConcurrentStackTest5_CtorAndCopyToAndToArray(1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest6_PushRange()
        {
            RunConcurrentStackTest6_PushRange(8, 10);
            RunConcurrentStackTest6_PushRange(16, 100);
            RunConcurrentStackTest6_PushRange(128, 100);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest7_PopRange()
        {
            RunConcurrentStackTest7_PopRange(8, 10);
            RunConcurrentStackTest7_PopRange(16, 100);
            RunConcurrentStackTest7_PopRange(128, 100);
        }

        // Just validates the stack correctly reports that it's empty.
        private static void RunConcurrentStackTest0_Empty(int count)
        {
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            int item;
            Assert.False(s.TryPop(out item), "RunConcurrentStackTest0_Empty:  TryPop returned true when the stack is empty");
            Assert.False(s.TryPeek(out item), "RunConcurrentStackTest0_Empty:  TryPeek returned true when the stack is empty");
            Assert.True(s.TryPopRange(new int[1]) == 0, "RunConcurrentStackTest0_Empty:  TryPopRange returned non zero when the stack is empty");

            for (int i = 0; i < count; i++)
                s.Push(i);

            bool isEmpty = s.IsEmpty;
            int sawCount = s.Count;

            bool result = (isEmpty == (count == 0) && sawCount == count);
            if (!result)
                Assert.False(true, String.Format(
                    "RunConcurrentStackTest0_Empty:  FAILED. IsEmpty={0} (expect {1}), Count={2} (expect {3})", isEmpty, count == 0, sawCount, count));
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL stack on the side to validate contents are correctly maintained.
        private static void RunConcurrentStackTest1_PushAndPop(int pushes, int pops)
        {
            // It utilised a random generator to do x number of pushes and
            // y number of pops where x = random, y = random.  Removed it
            // because it used System.Runtime.Extensions.

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            Stack<int> s2 = new Stack<int>();

            int donePushes = 0, donePops = 0;
            while (donePushes < pushes || donePops < pops)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (donePushes == pushes)
                        break;

                    int val = i;
                    s.Push(val);
                    s2.Push(val);
                    donePushes++;

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        Console.WriteLine("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c));
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    if (donePops == pops)
                        break;
                    if ((donePushes - donePops) <= 0)
                        break;

                    int e0, e1, e2;
                    bool b0 = s.TryPeek(out e0);
                    bool b1 = s.TryPop(out e1);
                    e2 = s2.Pop();
                    donePops++;

                    if (!b0 || !b1)
                    {
                        Console.WriteLine("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > stack was unexpectedly empty, wanted #{0}  (peek={1}, pop={2})", e2, b0, b1));
                    }

                    if (e0 != e1 || e1 != e2)
                    {
                        Console.WriteLine("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > stack contents differ, got #{0} (peek)/{1} (pop) but expected #{2}", e0, e1, e2));
                    }

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        Console.WriteLine("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c));
                    }
                }
            }

            int expected = pushes - pops;
            int endCount = s.Count;
            if (expected != endCount)
            {
                Console.WriteLine("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);
                Assert.False(true, String.Format("  > FAILED: expected = {0}, real = {1}", expected, endCount));
            }
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sconcurrently.
        private static void RunConcurrentStackTest2_ConcPushAndPop(int threads, int pushes, int pops)
        {
            // It utilised a random generator to do x number of pushes and
            // y number of pops where x = random, y = random.  Removed it
            // because it used System.Runtime.Extensions.

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            ManualResetEvent mre = new ManualResetEvent(false);
            Task[] tt = new Task[threads];

            // Create all threads.
            for (int k = 0; k < tt.Length; k++)
            {
                tt[k] = Task.Run(delegate()
                {
                    mre.WaitOne();

                    int donePushes = 0, donePops = 0;
                    while (donePushes < pushes || donePops < pops)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (donePushes == pushes)
                                break;

                            s.Push(i);
                            donePushes++;
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            if (donePops == pops)
                                break;
                            if ((donePushes - donePops) <= 0)
                                break;

                            int e;
                            if (s.TryPop(out e))
                                donePops++;
                        }
                    }
                });
            }

            // Kick 'em off and wait for them to finish.
            mre.Set();
            Task.WaitAll(tt);

            // Validate the count.
            int expected = threads * (pushes - pops);
            int endCount = s.Count;
            if (expected != endCount)
            {
                Console.WriteLine("* RunConcurrentStackTest2_ConcPushAndPop(threads={0}, pushes={1}, pops={2})", threads, pushes, pops);
                Assert.False(true, String.Format("  > expected = {0}, real = {1}", expected, endCount));
            }
        }

        // Just validates clearing the stack's contents.
        private static void RunConcurrentStackTest3_Clear(int count)
        {
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            s.Clear();

            bool isEmpty = s.IsEmpty;
            int sawCount = s.Count;

            bool passed = isEmpty && sawCount == 0;
            if (!passed)
                Assert.False(true, String.Format("RunConcurrentStackTest3_Clear:  > IsEmpty={0}, Count={1}", isEmpty, sawCount));
        }

        // Just validates enumerating the stack.
        private static void RunConcurrentStackTest4_Enumerator(int count)
        {
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            // Test enumerator.
            int j = count - 1;
            foreach (int x in s)
            {
                // Clear the stack to ensure concurrent modifications are dealt w/.
                if (x == count - 1)
                {
                    int e;
                    while (s.TryPop(out e)) ;
                }
                if (x != j)
                {
                    Assert.False(true, String.Format("RunConcurrentStackTest4_Enumerator:  > expected #{0}, but saw #{1}", j, x));
                }
                j--;
            }

            if (j > 0)
            {
                Assert.False(true, "RunConcurrentStackTest4_Enumerator:  > did not enumerate all elements in the stack");
            }
        }

        // Instantiates the stack w/ the enumerator ctor and validates the resulting copyto & toarray.
        private static void RunConcurrentStackTest5_CtorAndCopyToAndToArray(int count)
        {
            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentStack<int> s = new ConcurrentStack<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            if (sa1.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > ToArray resulting array is diff length: got {0}, wanted {1}",
                    sa1.Length, arr.Length));
            }
            for (int i = 0; i < sa1.Length; i++)
            {
                if (sa1[i] != arr[count - i - 1])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > ToArray returned an array w/ diff contents: got {0}, wanted {1}",
                        sa1[i], arr[count - i - 1]));
                }
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            if (sa2.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > CopyTo(int[]) resulting array is diff length: got {0}, wanted {1}",
                    sa2.Length, arr.Length));
            }
            for (int i = 0; i < sa2.Length; i++)
            {
                if (sa2[i] != arr[count - i - 1])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > CopyTo(int[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa2[i], arr[count - i - 1]));
                }
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            if (sa3.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > CopyTo(object[]) resulting array is diff length: got {0}, wanted {1}",
                    sa3.Length, arr.Length));
            }
            for (int i = 0; i < sa3.Length; i++)
            {
                if ((int)sa3[i] != arr[count - i - 1])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentStackTest5_CtorAndCopyToAndToArray:  > CopyTo(object[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa3[i], arr[count - i - 1]));
                }
            }
        }

        //Tests COncurrentSTack.PushRange
        private static void RunConcurrentStackTest6_PushRange(int NumOfThreads, int localArraySize)
        {
            ConcurrentStack<int> stack = new ConcurrentStack<int>();

            Task[] threads = new Task[NumOfThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Factory.StartNew((obj) =>
                {
                    int index = (int)obj;
                    int[] array = new int[localArraySize];
                    for (int j = 0; j < localArraySize; j++)
                    {
                        array[j] = index + j;
                    }

                    stack.PushRange(array);
                }, i * localArraySize, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            Task.WaitAll(threads);

            //validation
            for (int i = 0; i < threads.Length; i++)
            {
                int lastItem = -1;
                for (int j = 0; j < localArraySize; j++)
                {
                    int currentItem = 0;
                    if (!stack.TryPop(out currentItem))
                    {
                        Console.WriteLine("* RunConcurrentStackTest6_PushRange({0},{1})", NumOfThreads, localArraySize);
                        Assert.False(true, " > Failed, TryPop returned false");
                    }
                    if (lastItem > -1 && lastItem - currentItem != 1)
                    {
                        Console.WriteLine("* RunConcurrentStackTest6_PushRange({0},{1})", NumOfThreads, localArraySize);
                        Assert.False(true, String.Format(" > Failed {0} - {1} shouldn't be consecutive", lastItem, currentItem));
                    }

                    lastItem = currentItem;

                }
            }
        }

        //Tests ConcurrentStack.PopRange by pushing consecutove numbers and run n threads each thread tries to pop m itmes
        // the popped m items should be consecutive
        private static void RunConcurrentStackTest7_PopRange(int NumOfThreads, int elementsPerThread)
        {
            int lastValue = NumOfThreads * elementsPerThread;
            List<int> allValues = new List<int>();
            for (int i = 1; i <= lastValue; i++)
                allValues.Add(i);

            ConcurrentStack<int> stack = new ConcurrentStack<int>(allValues);

            Task[] threads = new Task[NumOfThreads];

            int[] array = new int[threads.Length * elementsPerThread];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Factory.StartNew((obj) =>
                {
                    int index = (int)obj;

                    int res;
                    if ((res = stack.TryPopRange(array, index, elementsPerThread)) != elementsPerThread)
                    {
                        Console.WriteLine("* RunConcurrentStackTest7_PopRange({0},{1})", NumOfThreads, elementsPerThread);
                        Assert.False(true, " > Failed TryPopRange didn't return the full range ");
                    }
                }, i * elementsPerThread, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            Task.WaitAll(threads);

            // validation
            for (int i = 0; i < NumOfThreads; i++)
            {
                for (int j = 1; j < elementsPerThread; j++)
                {
                    int currentIndex = i * elementsPerThread + j;
                    if (array[currentIndex - 1] - array[currentIndex] != 1)
                    {
                        Console.WriteLine("* RunConcurrentStackTest7_PopRange({0},{1})", NumOfThreads, elementsPerThread);
                        Assert.False(true, String.Format(" > Failed {0} - {1} shouldn't be consecutive", array[currentIndex - 1], array[currentIndex]));
                    }
                }
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest8_Exceptions()
        {
            ConcurrentStack<int> stack = null;
            Assert.Throws<ArgumentNullException>(
               () => stack = new ConcurrentStack<int>((IEnumerable<int>)null));
               // "RunConcurrentStackTest8_Exceptions:  The constructor didn't throw ANE when null collection passed");

            stack = new ConcurrentStack<int>();
            //CopyTo
            Assert.Throws<ArgumentNullException>(
               () => stack.CopyTo(null, 0));
               // "RunConcurrentStackTest8_Exceptions:  CopyTo didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.CopyTo(new int[1], -1));
               // "RunConcurrentStackTest8_Exceptions:  CopyTo didn't throw AORE when negative array index passed");

            //PushRange
            Assert.Throws<ArgumentNullException>(
               () => stack.PushRange(null));
               // "RunConcurrentStackTest8_Exceptions:  PushRange didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], 0, -1));
               // "RunConcurrentStackTest8_Exceptions:  PushRange didn't throw AORE when negative count passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], -1, 1));
               // "RunConcurrentStackTest8_Exceptions:  PushRange didn't throw AORE when negative index passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], 2, 1));
               // "RunConcurrentStackTest8_Exceptions:  PushRange didn't throw AORE when start index > array length");
            Assert.Throws<ArgumentException>(
               () => stack.PushRange(new int[1], 0, 10));
               // "RunConcurrentStackTest8_Exceptions:  PushRange didn't throw AE when count + index > array length");

            //PopRange
            Assert.Throws<ArgumentNullException>(
               () => stack.TryPopRange(null));
               // "RunConcurrentStackTest8_Exceptions:  TryPopRange didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], 0, -1));
               // "RunConcurrentStackTest8_Exceptions:  TryPopRange didn't throw AORE when negative count passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], -1, 1));
               // "RunConcurrentStackTest8_Exceptions:  TryPopRange didn't throw AORE when negative index passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], 2, 1));
               // "RunConcurrentStackTest8_Exceptions:  TryPopRange didn't throw AORE when start index > array length");
            Assert.Throws<ArgumentException>(
               () => stack.TryPopRange(new int[1], 0, 10));
               // "RunConcurrentStackTest8_Exceptions:  TryPopRange didn't throw AE when count + index > array length");
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest9_Interfaces_Negative()
        {
            ConcurrentStack<int> stack = new ConcurrentStack<int>();

            ICollection collection = stack;
            Assert.Throws<ArgumentNullException>(
               () => collection.CopyTo(null, 0));
               // "TestICollection:  ICollection.CopyTo didn't throw ANE when null collection passed for collection type: ConcurrentStack");
            Assert.Throws<NotSupportedException>(
               () => { object obj = collection.SyncRoot; });
               // "TestICollection:  ICollection.SyncRoot didn't throw NotSupportedException! for collection type: ConcurrentStack");
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentStackTest9_Interfaces()
        {
            ConcurrentStack<int> stack = new ConcurrentStack<int>();
            string collectionName = "ConcurrentStack";
            int item;

            IProducerConsumerCollection<int> ipcc = stack;
            
            Assert.True(ipcc.Count == 0,
               "TestIPCC:  The collection is not empty, this test expects an empty IPCC for collection type: " + collectionName);
            Assert.False(ipcc.TryTake(out item),
               "TestIPCC:  IPCC.TryTake returned true when the collection is empty for collection type: " + collectionName);
            Assert.True(ipcc.TryAdd(1),
               "TestIPCC:  IPCC.TryAdd returned false! for collection type: " + collectionName);
            ICollection collection = stack;
            Assert.False(collection.IsSynchronized,
               "ICollection.IsSynchronized returned true! for collection type: " + collectionName);
            stack.Push(1);
            int count = stack.Count;
            IEnumerable enumerable = stack;
            foreach (object o in enumerable)
                count--;
            Assert.True(count == 0, "IEnumerable.GetEnumerator didn't return all items! for collection type: " + collectionName);
        }
    }
}
