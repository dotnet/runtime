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
    public class ConcurrentQueueTests
    {
        [Fact]
        public static void TestConcurrentQueueBasic()
        {
            ConcurrentQueue<int> cq = new ConcurrentQueue<int>();
            cq.Enqueue(1);

            Task[] tks = new Task[2];
            tks[0] = Task.Factory.StartNew(() =>
            {
                cq.Enqueue(2);
                cq.Enqueue(3);
                cq.Enqueue(4);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            tks[1] = Task.Factory.StartNew(() =>
            {
                int item1, item2;
                var ret1 = cq.TryDequeue(out item1);
                // at least one item
                Assert.True(ret1);

                var ret2 = cq.TryDequeue(out item2);
                // two item
                if (ret2)
                {
                    Assert.True(item1 < item2, String.Format("{0} should less than {1}", item1, item2));
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
        public static void RunConcurrentQueueTest7_Exceptions()
        {
            ConcurrentQueue<int> queue = null;
            Assert.Throws<ArgumentNullException>(
               () => queue = new ConcurrentQueue<int>((IEnumerable<int>)null));
            // "RunConcurrentQueueTest7_Exceptions:  The constructor didn't throw ANE when null collection passed");

            queue = new ConcurrentQueue<int>();
            //CopyTo
            Assert.Throws<ArgumentNullException>(
               () => queue.CopyTo(null, 0));
            // "RunConcurrentQueueTest7_Exceptions:  CopyTo didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>(
               () => queue.CopyTo(new int[1], -1));
            // "RunConcurrentQueueTest7_Exceptions:  CopyTo didn't throw AORE when negative array index passed");
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest6_Interfaces()
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            string collectionName = "ConcurrentQueue";

            int item;

            IProducerConsumerCollection<int> ipcc = queue;
            Assert.True(ipcc.Count == 0,
               "TestIPCC:  The collection is not empty, this test expects an empty IPCC for collection type: " + collectionName);
            Assert.False(ipcc.TryTake(out item),
               "TestIPCC:  IPCC.TryTake returned true when the collection is empty for collection type: " + collectionName);
            Assert.True(ipcc.TryAdd(1),
               "TestIPCC:  IPCC.TryAdd returned false! for collection type: " + collectionName);

            ICollection collection = queue;
            Assert.False(collection.IsSynchronized,
               "ICollection.IsSynchronized returned true! for collection type: " + collectionName);

            queue.Enqueue(1);
            int count = queue.Count;
            IEnumerable enumerable = queue;
            foreach (object o in enumerable)
                count--;

            Assert.True(count == 0, "IEnumerable.GetEnumerator didn't return all items! for collection type: " + collectionName);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest6_Interfaces_Negative()
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            ICollection collection = queue;

            Assert.Throws<ArgumentNullException>(
               () => collection.CopyTo(null, 0));
            // "TestICollection:  ICollection.CopyTo didn't throw ANE when null collection passed for collection type: ConcurrentQueue");
            Assert.Throws<NotSupportedException>(
               () => { object obj = collection.SyncRoot; });
            // "TestICollection:  ICollection.SyncRoot didn't throw NotSupportedException! for collection type: ConcurrentQueue");
        }

        [Fact]
        [OuterLoop]
        public static void RunBigFix792038()
        {
            List<int> allItems = new List<int>();
            for (int i = 0; i < 10; i++)
                allItems.Add(i);

            ConcurrentQueue<int> queue = new ConcurrentQueue<int>(allItems);
            var e = queue.GetEnumerator();
            queue.Enqueue(11);

            int count = 0;
            while (e.MoveNext())
            {
                count++;
            }

            if (count != 10)
            {
                Console.WriteLine("* RunBugFix792038: GetEnumerator should take snapshot at time of invoke not when MoveNext() is called");
                Assert.False(true, "Failed! GetEnumerator should take snapshot at the time of invoke ");
            }
        }

        /// <summary>
        /// Bug fix 570046: Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value
        /// enumerator sometimes returns null
        /// </summary>
        /// <returns></returns>
        /// <remarks>to stress test this bug fix: wrap task t1 and t2 with while (true), but DO NOT CHECKIN!
        /// </remarks>
        [Fact]
        [OuterLoop]
        public static void RunBugFix570046()
        {
            var q = new ConcurrentQueue<int?>();

            var t1 = Task.Factory.StartNew(
             () =>
             {
                 for (int i = 0; i < 1000000; i++)
                 {
                     q.Enqueue(i);
                     int? o;
                     if (!q.TryDequeue(out o))
                     {
                         Console.WriteLine("* RunBugFix570046:  Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value");
                         Assert.False(true, "Failed! TryDequeue should never return false in this test");
                     }
                 }
             }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            var t2 = Task.Factory.StartNew(
             () =>
             {
                 foreach (var item in q)
                 {
                     if (item == null)
                     {
                         Console.WriteLine("* RunBugFix570046:  Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value");
                         Assert.False(true, "Failed! Enumerating should never return null value");
                     }
                 }
             }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            t2.Wait();
        }

        public static readonly ConcurrentQueue<object> m_queue = new ConcurrentQueue<object>();
        /// <summary>
        /// Regression test for bug 484295
        /// Compat: Bug in ConcurrentQueue in .NET 4.5 (.TryPeek returns true but no real object returned)
        /// http://vstfdevdiv:8080/DevDiv2/DevDiv/_workItems#id=484295
        /// </summary>
        // [Fact] Task too long
        public static void RunBugFix484295()
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            Task<InvalidPeekException> peepTask = Task.Factory
                .StartNew(() => TryPeek(cts.Token), cts.Token)
                .ContinueWith(task => HandleExceptions(task));

            Task queueDequeueTask = Task.Factory.StartNew(
                () => QueueDequeue(cts.Token),
                cts.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Console.WriteLine("Waiting 15 seconds for both Queue/Dequeue and TryPeek tasks..");
            Task.WaitAll(peepTask, queueDequeueTask);
            Console.WriteLine("Finished waiting...");

            if (peepTask.Result != null)
            {
                // means that the bug still exists.
                Assert.False(true, "ERROR: " + peepTask.Result.Message);
            }
        }

        private static void QueueDequeue(CancellationToken token)
        {
            object o = new object();

            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                m_queue.Enqueue(o);
                object val;
                m_queue.TryDequeue(out val);
            }
        }

        private static void TryPeek(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                object value;
                if (m_queue.TryPeek(out value))
                {
                    if (value == null)
                    {
                        throw new InvalidPeekException("value should not have been null after returning true for TryPeek.");
                    }
                }
            }
        }

        private static InvalidPeekException HandleExceptions(Task task)
        {
            InvalidPeekException exception = null;

            if (task.Exception != null)
            {
                task.Exception.Handle(e =>
                {
                    if (e is InvalidPeekException)
                    {
                        exception = (InvalidPeekException)e;
                        return true;
                    }
                    return false;
                });
            }

            return exception;
        }

        /// <summary>
        /// Memory leak bug
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void RunBugFix891778()
        {
            int iterations = 31; //any number <32 will do
            ConcurrentQueue<Finalizable> s_queue = new ConcurrentQueue<Finalizable>();

            for (int i = 0; i < iterations; i++)
            {
                s_queue.Enqueue(new Finalizable());
            }

            for (int i = 0; i < iterations; i++)
            {
                Finalizable temp;
                s_queue.TryDequeue(out temp);
                temp = null;
            }

            //call garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //we have to keep the queue object alive in order to catch daggling objects.
            GC.KeepAlive(s_queue);
            if (!Finalizable.finalized)
            {
                Console.WriteLine("RunBugFix891778: Memory leak in ConcurrentQueue.");
                Assert.False(true, "  > test failed : stale entry is not finalized");
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest0_Empty()
        {
            RunConcurrentQueueTest0_Empty(0);
            RunConcurrentQueueTest0_Empty(16);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest1_EnqAndDeq()
        {
            RunConcurrentQueueTest1_EnqAndDeq(0, 0);
            RunConcurrentQueueTest1_EnqAndDeq(5, 0);
            RunConcurrentQueueTest1_EnqAndDeq(5, 2);
            RunConcurrentQueueTest1_EnqAndDeq(5, 5);
            RunConcurrentQueueTest1_EnqAndDeq(1024, 512);
            RunConcurrentQueueTest1_EnqAndDeq(1024, 1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest1b_TryPeek()
        {
            RunConcurrentQueueTest1b_TryPeek(512);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest2_ConcEnqAndDeq()
        {
            RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 0);
            RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 1024 * 512);
            RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 1024 * 1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest4_Enumerator()
        {
            RunConcurrentQueueTest4_Enumerator(0);
            RunConcurrentQueueTest4_Enumerator(16);
            RunConcurrentQueueTest4_Enumerator(1024);
        }

        [Fact]
        [OuterLoop]
        public static void RunConcurrentQueueTest5_CtorAndCopyToAndToArray()
        {
            RunConcurrentQueueTest5_CtorAndCopyToAndToArray(0);
            RunConcurrentQueueTest5_CtorAndCopyToAndToArray(16);
            RunConcurrentQueueTest5_CtorAndCopyToAndToArray(1024);
        }

        // Just validates the queue correctly reports that it's empty.
        public static void RunConcurrentQueueTest0_Empty(int count)
        {
            ConcurrentQueue<int> q = new ConcurrentQueue<int>();
            for (int i = 0; i < count; i++)
                q.Enqueue(i);

            bool isEmpty = q.IsEmpty;
            int sawCount = q.Count;

            bool passed = isEmpty == (count == 0) && sawCount == count;
            if (!passed)
                Assert.False(true, String.Format(
                    "RunConcurrentQueueTest0_Empty:  > IsEmpty={0} (expect {1}), Count={2} (expect {3})", isEmpty, count == 0, sawCount, count));
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL queue on the side to validate contents are correctly maintained.
        private static void RunConcurrentQueueTest1_EnqAndDeq(int pushes, int pops)
        {
            // It utilised a random generator to do x number of queues and enqueues.
            // Removed because it used System.Runtime.Extensions.
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            Queue<int> s2 = new Queue<int>();

            int donePushes = 0, donePops = 0;
            while (donePushes < pushes || donePops < pops)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (donePushes == pushes)
                        break;

                    int val = i;
                    s.Enqueue(val);
                    s2.Enqueue(val);
                    donePushes++;

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c));
                    }
                }
                for (int i = 0; i < 10; i++)
                {
                    if (donePops == pops)
                        break;
                    if ((donePushes - donePops) <= 0)
                        break;

                    int e1, e2;
                    bool b1 = s.TryDequeue(out e1);
                    e2 = s2.Dequeue();
                    donePops++;

                    if (!b1)
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > queue was unexpectedly empty, wanted #{0}  (pop={1})", e2, b1));
                    }

                    if (e1 != e2)
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > queue contents differ, got #{0} but expected #{1}", e1, e2));
                    }

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);
                        Assert.False(true, String.Format("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c));
                    }
                }
            }

            int expected = pushes - pops;
            int endCount = s.Count;
            if (endCount != expected)
            {
                Console.WriteLine("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);
                Assert.False(true, String.Format("  > expected = {0}, real = {1}: passed? {2}", expected, endCount));
            }
        }

        // Just pushes and pops, ensuring try peek is always accurate.
        public static void RunConcurrentQueueTest1b_TryPeek(int pushes)
        {
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            int[] arr = new int[pushes];
            for (int i = 0; i < pushes; i++)
                arr[i] = i;

            // should be empty.
            int y;
            if (s.TryPeek(out y))
            {
                Console.WriteLine("* RunConcurrentQueueTest1b_TryPeek(pushes={0})", pushes);
                Assert.False(true, String.Format("    > queue should be empty!  TryPeek returned true {0}", y));
            }

            for (int i = 0; i < arr.Length; i++)
            {
                s.Enqueue(arr[i]);

                // Validate the front is still returned.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    if (!s.TryPeek(out x) || x != arr[0])
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1b_TryPeek(pushes={0})", pushes);
                        Assert.False(true, String.Format("    > peek after enqueue didn't return expected element: {0} instead of {1}", x, arr[0]));
                    }
                }
            }

            for (int i = 0; i < arr.Length; i++)
            {
                // Validate the element about to be returned is correct.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    if (!s.TryPeek(out x) || x != arr[i])
                    {
                        Console.WriteLine("* RunConcurrentQueueTest1b_TryPeek(pushes={0})", pushes);
                        Assert.False(true, String.Format("    > peek after enqueue didn't return expected element: {0} instead of {1}", x, arr[i]));
                    }
                }

                s.TryDequeue(out x);
            }

            // should be empty.
            int z;
            if (s.TryPeek(out z))
            {
                Console.WriteLine("* RunConcurrentQueueTest1b_TryPeek(pushes={0})", pushes);
                Assert.False(true, String.Format("    > queue should be empty!  TryPeek returned true {0}", y));
            }
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen concurrently.
        public static void RunConcurrentQueueTest2_ConcEnqAndDeq(int threads, int pushes, int pops)
        {
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            ManualResetEvent mre = new ManualResetEvent(false);
            Task[] tt = new Task[threads];

            // Create all threads.
            for (int k = 0; k < tt.Length; k++)
            {
                tt[k] = Task.Run(delegate()
                {
                    // It utilised a random generator to do x number of queues and enqueues.
                    // Removed because it used System.Runtime.Extensions.
                    mre.WaitOne();

                    int donePushes = 0, donePops = 0;
                    while (donePushes < pushes || donePops < pops)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (donePushes == pushes)
                                break;

                            s.Enqueue(i);
                            donePushes++;
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            if (donePops == pops)
                                break;
                            if ((donePushes - donePops) <= 0)
                                break;

                            int e;
                            if (s.TryDequeue(out e))
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
                Console.WriteLine("* RunConcurrentQueueTest2_ConcEnqAndDeq(threads={0}, pushes={1}, pops={2})", threads, pushes, pops);
                Assert.False(true, String.Format("  > FAILED: expected = {0}, real = {1}", expected, endCount));
            }
        }

        // Just validates enumerating the stack.
        public static void RunConcurrentQueueTest4_Enumerator(int count)
        {
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            for (int i = 0; i < count; i++)
                s.Enqueue(i);

            // Test enumerator.
            int j = 0;
            foreach (int x in s)
            {
                // Clear the stack to ensure concurrent modifications are dealt w/.
                if (x == count - 1)
                {
                    int e;
                    while (s.TryDequeue(out e)) ;
                }
                if (x != j)
                {
                    Assert.False(true, String.Format("RunConcurrentQueueTest4_Enumerator:  FAILED  > expected #{0}, but saw #{1}", j, x));
                }
                j++;
            }

            if (j != count)
            {
                Assert.False(true, "RunConcurrentQueueTest4_Enumerator:  FAILED  > did not enumerate all elements in the stack");
            }

        }

        // Instantiates the queue w/ the enumerator ctor and validates the resulting copyto & toarray.
        public static void RunConcurrentQueueTest5_CtorAndCopyToAndToArray(int count)
        {
            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentQueue<int> s = new ConcurrentQueue<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            if (sa1.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > ToArray resulting array is diff length: got {0}, wanted {1}",
                    sa1.Length, arr.Length));
            }
            for (int i = 0; i < sa1.Length; i++)
            {
                if (sa1[i] != arr[i])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > ToArray returned an array w/ diff contents: got {0}, wanted {1}",
                        sa1[i], arr[i]));
                }
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            if (sa2.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > CopyTo(int[]) resulting array is diff length: got {0}, wanted {1}",
                    sa2.Length, arr.Length));
            }
            for (int i = 0; i < sa2.Length; i++)
            {
                if (sa2[i] != arr[i])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > CopyTo(int[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa2[i], arr[i]));
                }
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            if (sa3.Length != arr.Length)
            {
                Assert.False(true, String.Format(
                    "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > CopyTo(object[]) resulting array is diff length: got {0}, wanted {1}",
                    sa3.Length, arr.Length));
            }
            for (int i = 0; i < sa3.Length; i++)
            {
                if ((int)sa3[i] != arr[i])
                {
                    Assert.False(true, String.Format(
                        "RunConcurrentQueueTest5_CtorAndCopyToAndToArray: FAILED  > CopyTo(object[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa3[i], arr[i]));
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Bug 891778: memory leak in ConcurrentQueue due to ConcurrentQueue.Segment.TryRemove() method
        /// didn't set the removed entries to be null. This only happens when the queue is only enqueued 
        /// no more than 31 times (since segment size is 32). 
        /// This test construct such a scenario: a queue is enqueued 31 times then dequeued 31 times. The
        /// queue is empty, but we keep the queue object alive in the GC, and its first segment is also 
        /// alive. Before the bug fix, the first segment still holds on to the references of its deleted 
        /// entries, so the entries cannot be GCed. After the fix they should be GCed.
        /// </summary>
        /// <returns></returns>
        private class Finalizable
        {
            internal static volatile bool finalized = false;
            ~Finalizable() { finalized = true; }
        }

        private class InvalidPeekException : Exception
        {
            internal InvalidPeekException(string message)
                : base(message)
            { }
        }

        #endregion
    }
}
