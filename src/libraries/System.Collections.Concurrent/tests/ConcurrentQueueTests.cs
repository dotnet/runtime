// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentQueueTests
    {
        [Fact]
        public static void TestBasicScenarios()
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
        public static void Test7_Exceptions()
        {
            ConcurrentQueue<int> queue = null;
            Assert.Throws<ArgumentNullException>(
               () => queue = new ConcurrentQueue<int>((IEnumerable<int>)null));
            // "Test7_Exceptions:  The constructor didn't throw ANE when null collection passed");

            queue = new ConcurrentQueue<int>();
            //CopyTo
            Assert.Throws<ArgumentNullException>( () => queue.CopyTo(null, 0));
            // "Test7_Exceptions:  CopyTo didn't throw ANE when null array passed");
            Assert.Throws<ArgumentOutOfRangeException>( () => queue.CopyTo(new int[1], -1));
            // "Test7_Exceptions:  CopyTo didn't throw AORE when negative array index passed");
        }

        [Fact]
        public static void Test6_Interfaces()
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            int item;

            IProducerConsumerCollection<int> ipcc = queue;
            Assert.Equal(0, ipcc.Count);
            Assert.False(ipcc.TryTake(out item), "TestIPCC:  IPCC.TryTake returned true when the collection is empty");
            Assert.True(ipcc.TryAdd(1));

            ICollection collection = queue;
            Assert.False(collection.IsSynchronized);

            queue.Enqueue(1);
            int count = queue.Count;
            IEnumerable enumerable = queue;
            foreach (object o in enumerable)
                count--;

            Assert.Equal(0, count);
        }

        [Fact]
        public static void Test6_Interfaces_Negative()
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
        public static void TestBigFix792038()
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
                Console.WriteLine("* GetEnumerator should take snapshot at time of invoke not when MoveNext() is called");
                Assert.False(true, "Failed! GetEnumerator should take snapshot at the time of invoke ");
            }
        }

        /// <summary>
        /// Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value
        /// enumerator sometimes returns null
        /// </summary>
        /// <returns></returns>
        /// <remarks>to stress test this bug fix: wrap task t1 and t2 with while (true), but DO NOT CHECKIN!
        /// </remarks>
        [Fact]
        public static void TestBugFix570046()
        {
            var q = new ConcurrentQueue<int?>();

            var t1 = Task.Factory.StartNew(
             () =>
             {
                 for (int i = 0; i < 1000000; i++)
                 {
                     q.Enqueue(i);
                     int? o;
                     
                     Assert.True(q.TryDequeue(out o), "TryDequeue should never return false in this test");
                 }
             }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            var t2 = Task.Factory.StartNew(
             () =>
             {
                 foreach (var item in q)
                 {
                     Assert.NotNull(item);
                 }
             }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            t2.Wait();
        }

        public static readonly ConcurrentQueue<object> m_queue = new ConcurrentQueue<object>();
        /// <summary>
        /// .TryPeek returns true but no real object returned
        /// </summary>
        // [Fact]
        [OuterLoop]
        public static void TestBugFix484295()
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
        public static void TestBugFix891778()
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
            Assert.True(Finalizable.finalized, "Memory leak in ConcurrentQueue: stale entry is not finalized.");
        }

        [Fact]
        public static void Test0_Empty()
        {
            ConcurrentQueue<int> q = new ConcurrentQueue<int>();
            Assert.Equal(0, q.Count);
            Assert.True(q.IsEmpty);

            int count = 16;
            for (int i = 0; i < count; i++)
                q.Enqueue(i);

            Assert.Equal(count, q.Count);
            Assert.False(q.IsEmpty);
        }

        [Fact]
        public static void Test1_EnqAndDeq()
        {
            Test1_EnqAndDeq(0, 0);
            Test1_EnqAndDeq(7, 7);
        }

        [Fact]
        public static void Test1_EnqAndDeq01()
        {
            Test1_EnqAndDeq(5, 0);
            Test1_EnqAndDeq(512, 256);
        }

        [Fact]
        public static void Test1b_TryPeek()
        {
            Test1b_TryPeek(256);
        }

        [Fact]
        public static void Test2_ConcEnqAndDeq()
        {
            Test2_ConcEnqAndDeq(4, 1024, 0);
        }

        [Fact]
        [OuterLoop]
        public static void Test2_ConcEnqAndDeq01()
        {
            Test2_ConcEnqAndDeq(6, 1024, 512);
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
            Test5_CtorAndCopyToAndToArray(8);
        }

        [Fact]
        [OuterLoop]
        public static void Test5_CtorAndCopyToAndToArray01()
        {
            Test5_CtorAndCopyToAndToArray(512);
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL queue on the side to validate contents are correctly maintained.
        private static void Test1_EnqAndDeq(int pushes, int pops)
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

                    Assert.Equal(s.Count, s2.Count);
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

                    Assert.True(b1,
                        String.Format("* Test1_EnqAndDeq(pushes={0}, pops={1}): queue was unexpectedly empty, wanted #{2} ", pushes, pops, e2));

                    Assert.Equal(e1, e2);
                    Assert.Equal(s.Count, s2.Count);
                }
            }

            Assert.Equal(pushes - pops, s.Count);
        }

        // Just pushes and pops, ensuring try peek is always accurate.
        private static void Test1b_TryPeek(int pushes)
        {
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            int[] arr = new int[pushes];
            for (int i = 0; i < pushes; i++)
                arr[i] = i;

            // should be empty.
            int y;
            Assert.False(s.TryPeek(out y), String.Format("* Test1b_TryPeek(pushes={0}): queue should be empty! TryPeek returned true with value {0}", pushes, y));

            for (int i = 0; i < arr.Length; i++)
            {
                s.Enqueue(arr[i]);

                // Validate the front is still returned.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    Assert.True(s.TryPeek(out x));
                    Assert.Equal(arr[0], x);
                }
            }

            for (int i = 0; i < arr.Length; i++)
            {
                // Validate the element about to be returned is correct.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    Assert.True(s.TryPeek(out x));
                    Assert.Equal(arr[i], x);
                }

                s.TryDequeue(out x);
            }

            // should be empty.
            int z;
            Assert.False(s.TryPeek(out z), String.Format("* Test1b_TryPeek(pushes={0}): queue should be empty! TryPeek returned true with value {0}", pushes, z));
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen concurrently.
        private static void Test2_ConcEnqAndDeq(int threads, int pushes, int pops)
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
            Assert.Equal(threads * (pushes - pops), s.Count);
        }

        // Just validates enumerating the stack.
        private static void Test4_Enumerator(int count)
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

                Assert.Equal(x, j);

                j++;
            }

            Assert.Equal(count, j);
        }

        // Instantiates the queue w/ the enumerator ctor and validates the resulting copyto & toarray.
        public static void Test5_CtorAndCopyToAndToArray(int count)
        {
            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentQueue<int> s = new ConcurrentQueue<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            Assert.Equal(arr.Length, sa1.Length);

            for (int i = 0; i < sa1.Length; i++)
            {
                Assert.Equal(arr[i], sa1[i]);
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            Assert.Equal(arr.Length, sa2.Length);

            for (int i = 0; i < sa2.Length; i++)
            {
                Assert.Equal(arr[i], sa2[i]);
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            Assert.Equal(arr.Length, sa3.Length);

            for (int i = 0; i < sa3.Length; i++)
            {
                Assert.Equal(arr[i], (int)sa3[i]);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Memory leak in ConcurrentQueue due to ConcurrentQueue.Segment.TryRemove() method
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
