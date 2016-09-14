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
    public class ConcurrentStackTests : IEnumerable_Generic_Tests<int>
    {
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();
        protected override IEnumerable<int> GenericIEnumerableFactory(int count) => new ConcurrentStack<int>(Enumerable.Range(0, count));
        protected override int CreateT(int seed) => new Random(seed).Next();
        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;
        protected override bool ResetImplemented => false;
        protected override bool IEnumerable_Generic_Enumerator_Current_EnumerationNotStarted_ThrowsInvalidOperationException => false;

        [Fact]
        public static void Test0_Empty()
        {
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            int item;
            Assert.False(s.TryPop(out item), "Test0_Empty:  TryPop returned true when the stack is empty");
            Assert.False(s.TryPeek(out item), "Test0_Empty:  TryPeek returned true when the stack is empty");
            Assert.True(s.TryPopRange(new int[1]) == 0, "Test0_Empty:  TryPopRange returned non zero when the stack is empty");

            int count = 15;
            for (int i = 0; i < count; i++)
                s.Push(i);

            Assert.Equal(count, s.Count);
            Assert.False(s.IsEmpty);
        }

        [Fact]
        public static void Test1_PushAndPop()
        {
            Test1_PushAndPop(0, 0);
            Test1_PushAndPop(9, 9);
        }

        [Fact]
        [OuterLoop]
        public static void Test1_PushAndPop01()
        {
            Test1_PushAndPop(3, 0);
            Test1_PushAndPop(1024, 512);
        }

        [Fact]
        public static void Test2_ConcPushAndPop()
        {
            Test2_ConcPushAndPop(3, 1024, 0);
        }

        [Fact]
        public static void Test2_ConcPushAndPop01()
        {
            Test2_ConcPushAndPop(8, 1024, 512);
        }

        [Fact]
        public static void Test3_Clear()
        {
            Test3_Clear(0);
            Test3_Clear(16);
            Test3_Clear(1024);
        }

        [Fact]
        public static void Test4_Enumerator()
        {
            Test4_Enumerator(0);
            Test4_Enumerator(16);
        }

        [Fact]
        [OuterLoop]
        public static void Test4_Enumerator01()
        {
            Test4_Enumerator(1024);
        }

        [Fact]
        public static void Test5_CtorAndCopyToAndToArray()
        {
            Test5_CtorAndCopyToAndToArray(0);
            Test5_CtorAndCopyToAndToArray(16);
        }

        [Fact]
        [OuterLoop]
        public static void Test5_CtorAndCopyToAndToArray01()
        {
            Test5_CtorAndCopyToAndToArray(1024);
        }

        [Fact]
        public static void Test6_PushRange()
        {
            Test6_PushRange(8, 10);
            Test6_PushRange(16, 100);
        }

        [Fact]
        public static void Test7_PopRange()
        {
            Test7_PopRange(8, 10);
            Test7_PopRange(16, 100);
        }

        [Fact]
        [OuterLoop]
        public static void Test_PushPopRange()
        {
            Test6_PushRange(128, 100);
            Test7_PopRange(128, 100);
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL stack on the side to validate contents are correctly maintained.
        private static void Test1_PushAndPop(int pushes, int pops)
        {
            // It utilized a random generator to do x number of pushes and
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

                    Assert.Equal(s.Count, s2.Count);
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

                    Assert.True(b0);
                    Assert.True(b1);

                    Assert.Equal(e0, e1);
                    Assert.Equal(e1, e2);
                    Assert.Equal(s.Count, s2.Count);
                }
            }

            Assert.Equal(pushes - pops, s.Count);
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen concurrently.
        private static void Test2_ConcPushAndPop(int threads, int pushes, int pops)
        {
            // It utilized a random generator to do x number of pushes and
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
            Assert.Equal(threads * (pushes - pops), s.Count);
        }

        // Just validates clearing the stack's contents.
        private static void Test3_Clear(int count)
        {
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            s.Clear();

            Assert.True(s.IsEmpty);
            Assert.Equal(0, s.Count);
        }

        // Just validates enumerating the stack.
        private static void Test4_Enumerator(int count)
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

                Assert.Equal(j, x);
                j--;
            }

            Assert.True(j <= 0, " > did not enumerate all elements in the stack");
        }

        // Instantiates the stack w/ the enumerator ctor and validates the resulting copyto & toarray.
        private static void Test5_CtorAndCopyToAndToArray(int count)
        {
            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentStack<int> s = new ConcurrentStack<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            Assert.Equal(arr.Length, sa1.Length);

            for (int i = 0; i < sa1.Length; i++)
            {
                Assert.Equal(arr[count - i - 1], sa1[i]);
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            Assert.Equal(arr.Length, sa2.Length);

            for (int i = 0; i < sa2.Length; i++)
            {
                Assert.Equal(arr[count - i - 1], sa2[i]);
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            Assert.Equal(arr.Length, sa3.Length);

            for (int i = 0; i < sa3.Length; i++)
            {
                Assert.Equal(arr[count - i - 1], (int)sa3[i]);
            }
        }

        //Tests COncurrentSTack.PushRange
        private static void Test6_PushRange(int NumOfThreads, int localArraySize)
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

                    Assert.True(stack.TryPop(out currentItem),
                        String.Format("* Test6_PushRange({0},{1})L TryPop returned false.", NumOfThreads, localArraySize));

                    Assert.True((lastItem <= -1) || lastItem - currentItem == 1,
                        String.Format("* Test6_PushRange({0},{1}): Failed {2} - {3} shouldn't be consecutive", NumOfThreads, localArraySize, lastItem, currentItem));

                    lastItem = currentItem;
                }
            }
        }

        //Tests ConcurrentStack.PopRange by pushing consecutive numbers and run n threads each thread tries to pop m itmes
        // the popped m items should be consecutive
        private static void Test7_PopRange(int NumOfThreads, int elementsPerThread)
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

                    int res = stack.TryPopRange(array, index, elementsPerThread);
                    Assert.Equal(elementsPerThread, res);
                }, i * elementsPerThread, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            Task.WaitAll(threads);

            // validation
            for (int i = 0; i < NumOfThreads; i++)
            {
                for (int j = 1; j < elementsPerThread; j++)
                {
                    int currentIndex = i * elementsPerThread + j;
                    Assert.Equal(array[currentIndex - 1], array[currentIndex] + 1);
                }
            }
        }

        [Fact]
        public static void Test8_Exceptions()
        {
            ConcurrentStack<int> stack = null;
            Assert.Throws<ArgumentNullException>(
               () => stack = new ConcurrentStack<int>((IEnumerable<int>)null));
               // "Test8_Exceptions:  The constructor didn't throw ANE when null collection passed");

            stack = new ConcurrentStack<int>();
            //CopyTo
            Assert.Throws<ArgumentNullException>(
               () => stack.CopyTo(null, 0));
               // "Test8_Exceptions:  CopyTo didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.CopyTo(new int[1], -1));
               // "Test8_Exceptions:  CopyTo didn't throw AORE when negative array index passed");

            //PushRange
            Assert.Throws<ArgumentNullException>(
               () => stack.PushRange(null));
               // "Test8_Exceptions:  PushRange didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], 0, -1));
               // "Test8_Exceptions:  PushRange didn't throw AORE when negative count passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], -1, 1));
               // "Test8_Exceptions:  PushRange didn't throw AORE when negative index passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.PushRange(new int[1], 2, 1));
               // "Test8_Exceptions:  PushRange didn't throw AORE when start index > array length");
            Assert.Throws<ArgumentException>(
               () => stack.PushRange(new int[1], 0, 10));
               // "Test8_Exceptions:  PushRange didn't throw AE when count + index > array length");

            //PopRange
            Assert.Throws<ArgumentNullException>(
               () => stack.TryPopRange(null));
               // "Test8_Exceptions:  TryPopRange didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], 0, -1));
               // "Test8_Exceptions:  TryPopRange didn't throw AORE when negative count passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], -1, 1));
               // "Test8_Exceptions:  TryPopRange didn't throw AORE when negative index passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => stack.TryPopRange(new int[1], 2, 1));
               // "Test8_Exceptions:  TryPopRange didn't throw AORE when start index > array length");
            Assert.Throws<ArgumentException>(
               () => stack.TryPopRange(new int[1], 0, 10));
               // "Test8_Exceptions:  TryPopRange didn't throw AE when count + index > array length");
        }

        [Fact]
        public static void Test9_Interfaces_Negative()
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
        public static void Test10_DebuggerAttributes()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new ConcurrentStack<int>());
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(new ConcurrentStack<int>());
        }

        [Fact]
        public static void Test9_Interfaces()
        {
            ConcurrentStack<int> stack = new ConcurrentStack<int>();
            IProducerConsumerCollection<int> ipcc = stack;

            Assert.Equal(0, ipcc.Count);
            int item;
            Assert.False(ipcc.TryTake(out item));
            Assert.True(ipcc.TryAdd(1));

            ICollection collection = stack;
            Assert.False(collection.IsSynchronized);

            stack.Push(1);
            int count = stack.Count;
            IEnumerable enumerable = stack;
            foreach (object o in enumerable)
                count--;

            Assert.Equal(0, count);
        }
    }
}
