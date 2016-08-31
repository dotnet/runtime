// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentQueueTests : IEnumerable_Generic_Tests<int>
    {
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();
        protected override IEnumerable<int> GenericIEnumerableFactory(int count) => new ConcurrentQueue<int>(Enumerable.Range(0, count));
        protected override int CreateT(int seed) => new Random(seed).Next();
        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;
        protected override bool ResetImplemented => false;
        protected override bool IEnumerable_Generic_Enumerator_Current_EnumerationNotStarted_ThrowsInvalidOperationException => false;

        [Fact]
        public void Ctor_NoArg_ItemsAndCountMatch()
        {
            var q = new ConcurrentQueue<int>();
            Assert.True(q.IsEmpty);
            Assert.Equal(0, q.Count);
            Assert.Equal(Enumerable.Empty<int>(), q);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public void Ctor_Collection_ItemsAndCountMatch(int count)
        {
            var q = new ConcurrentQueue<int>(Enumerable.Range(1, count));
            Assert.Equal(count == 0, q.IsEmpty);
            Assert.Equal(count, q.Count);
            Assert.Equal(Enumerable.Range(1, count), q);
        }

        [Fact]
        public void Ctor_NullEnumerable_Throws()
        {
            Assert.Throws<ArgumentNullException>("collection", () => new ConcurrentQueue<int>(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public static void ToArray_ItemsAndCountMatch(int count)
        {
            ConcurrentQueue<int> q = new ConcurrentQueue<int>(Enumerable.Range(42, count));
            Assert.Equal(Enumerable.Range(42, count), q.ToArray());

            if (count > 0)
            {
                int item;
                Assert.True(q.TryDequeue(out item));
                Assert.Equal(42, item);
                Assert.Equal(Enumerable.Range(43, count - 1), q.ToArray());
            }
        }

        [Fact]
        public void DebuggerAttributes_Success()
        {
            var q = new ConcurrentQueue<int>(Enumerable.Range(0, 10));
            DebuggerAttributes.ValidateDebuggerDisplayReferences(q);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(q);
        }

        [Fact]
        public void Enqueue_TryDequeue_MatchesQueue()
        {
            var q = new Queue<int>();
            var cq = new ConcurrentQueue<int>();

            Action dequeue = () =>
            {
                int item1 = q.Dequeue();
                int item2;
                Assert.True(cq.TryDequeue(out item2));
                Assert.Equal(item1, item2);
                Assert.Equal(q.Count, cq.Count);
                Assert.Equal(q, cq);
            };

            for (int i = 0; i < 100; i++)
            {
                cq.Enqueue(i);
                q.Enqueue(i);
                Assert.Equal(q.Count, cq.Count);
                Assert.Equal(q, cq);

                // Start dequeueing some after we've added some
                if (i > 50)
                {
                    dequeue();
                }
            }

            // Dequeue the rest
            while (q.Count > 0)
            {
                dequeue();
            }
        }

        [Fact]
        public void TryPeek_Idempotent()
        {
            var cq = new ConcurrentQueue<int>();
            int item;

            Assert.False(cq.TryPeek(out item));
            Assert.Equal(0, item);
            Assert.False(cq.TryPeek(out item));
            Assert.Equal(0, item);

            cq.Enqueue(42);

            Assert.True(cq.TryPeek(out item));
            Assert.Equal(42, item);
            Assert.True(cq.TryPeek(out item));
            Assert.Equal(42, item);

            Assert.True(cq.TryDequeue(out item));
            Assert.Equal(42, item);
            Assert.False(cq.TryPeek(out item));
            Assert.Equal(0, item);
        }

        [Fact]
        public void Concurrent_Enqueue_TryDequeue_AllItemsReceived()
        {
            int items = 1000;

            var q = new ConcurrentQueue<int>();

            // Consumer dequeues items until it gets as many as it expects
            Task consumer = Task.Run(() =>
            {
                int lastReceived = 0;
                int item;
                while (true)
                {
                    if (q.TryDequeue(out item))
                    {
                        Assert.Equal(lastReceived + 1, item);
                        lastReceived = item;
                        if (item == items) break;
                    }
                    else
                    {
                        Assert.Equal(0, item);
                    }
                }
            });

            // Producer queues the expected number of items
            Task producer = Task.Run(() =>
            {
                for (int i = 1; i <= items; i++) q.Enqueue(i);
            });

            Task.WaitAll(producer, consumer);
        }

        [Fact]
        public void Concurrent_Enqueue_TryPeek_TryDequeue_AllItemsSeen()
        {
            int items = 1000;

            var q = new ConcurrentQueue<int>();

            // Consumer peeks and then dequeues the expected number of items
            Task consumer = Task.Run(() =>
            {
                int lastReceived = 0;
                int item;
                while (true)
                {
                    if (q.TryPeek(out item))
                    {
                        Assert.Equal(lastReceived + 1, item);
                        Assert.True(q.TryDequeue(out item));
                        Assert.Equal(lastReceived + 1, item);
                        lastReceived = item;
                        if (item == items) break;
                    }
                }
            });

            // Producer queues the expected number of items
            Task producer = Task.Run(() =>
            {
                for (int i = 1; i <= items; i++) q.Enqueue(i);
            });

            Task.WaitAll(producer, consumer);
        }

        [Fact]
        public void Concurrent_EnqueueDequeue_IsEmpty_AlwaysFalse()
        {
            int items = 1000;

            var q = new ConcurrentQueue<int>();
            q.Enqueue(0); // make sure it's never empty
            var cts = new CancellationTokenSource();

            // Consumer repeatedly calls IsEmpty until it's told to stop
            Task consumer = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested) Assert.False(q.IsEmpty);
            });

            // Producer enqueues/dequeues a bunch of items, then tells the consumer to stop
            Task producer = Task.Run(() =>
            {
                int ignored;
                for (int i = 1; i <= items; i++)
                {
                    q.Enqueue(i);
                    Assert.True(q.TryDequeue(out ignored));
                }
                cts.Cancel();
            });

            Task.WaitAll(producer, consumer);
        }

        [Fact]
        public void Concurrent_EnqueueObjects_Enumerate_NeverEmptyOrNull()
        {
            int items = 1000;
            object obj = new object();

            var q = new ConcurrentQueue<object>();
            q.Enqueue(obj); // ensure always at least one item

            var cts = new CancellationTokenSource();

            // Consumer repeatedly iterates the collection until it's told to stop
            Task consumer = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    bool gotOne = false;
                    foreach (object o in q)
                    {
                        gotOne = true;
                        Assert.NotNull(o);
                    }
                    Assert.True(gotOne);
                }
            });

            // Producer enqueues and dequeues a bunch of items, then tells consumer to stop
            Task producer = Task.Run(() =>
            {
                for (int iters = 0; iters < 3; iters++)
                {
                    for (int i = 1; i <= items; i++) q.Enqueue(i);
                    object item;
                    for (int i = 1; i <= items; i++) Assert.True(q.TryDequeue(out item));
                }
                cts.Cancel();
            });

            Task.WaitAll(producer, consumer);
        }

        [Theory]
        [InlineData(1, 4, 1024)]
        [InlineData(4, 1, 1024)]
        [InlineData(3, 3, 1024)]
        public void MultipleProducerConsumer_AllItemsTransferred(int producers, int consumers, int itemsPerProducer)
        {
            var cq = new ConcurrentQueue<int>();
            var tasks = new List<Task>();

            int totalItems = producers * itemsPerProducer;
            int remainingItems = totalItems;
            int sum = 0;

            for (int i = 0; i < consumers; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (Volatile.Read(ref remainingItems) > 0)
                    {
                        int item;
                        if (cq.TryDequeue(out item))
                        {
                            Interlocked.Add(ref sum, item);
                            Interlocked.Decrement(ref remainingItems);
                        }
                    }
                }));
            }

            for (int i = 0; i < producers; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int item = 1; item <= itemsPerProducer; item++)
                    {
                        cq.Enqueue(item);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.Equal(producers * (itemsPerProducer * (itemsPerProducer + 1) / 2), sum);
        }

        [Fact]
        public void CopyTo_InvalidArgs_Throws()
        {
            Assert.Throws<ArgumentNullException>("array", () => new ConcurrentQueue<int>().CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentQueue<int>().CopyTo(new int[1], -1));
            Assert.Throws<ArgumentException>(() => new ConcurrentQueue<int>().CopyTo(new int[1], 2));
        }

        [Fact]
        public void CopyTo_Empty_Success()
        {
            var q = new ConcurrentQueue<int>();
            q.CopyTo(Array.Empty<int>(), 0);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(100, false)]
        [InlineData(512, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
        [InlineData(512, true)]
        public void CopyTo_AllItemsCopiedAtCorrectLocation(int count, bool viaInterface)
        {
            var q = new ConcurrentQueue<int>(Enumerable.Range(0, count));
            var c = (ICollection)q;
            int[] arr = new int[count];

            if (viaInterface)
            {
                c.CopyTo(arr, 0);
            }
            else
            {
                q.CopyTo(arr, 0);
            }
            Assert.Equal(q, arr);

            if (count > 1)
            {
                int toRemove = count / 2;
                int remaining = count - toRemove;
                for (int i = 0; i < toRemove; i++)
                {
                    int item;
                    Assert.True(q.TryDequeue(out item));
                    Assert.Equal(i, item);
                }

                if (viaInterface)
                {
                    c.CopyTo(arr, 1);
                }
                else
                {
                    q.CopyTo(arr, 1);
                }

                Assert.Equal(0, arr[0]);
                for (int i = 0; i < remaining; i++)
                {
                    Assert.Equal(arr[1 + i], i + toRemove);
                }
            }
        }

        [Fact]
        public void ICollection_Count_Success()
        {
            ICollection c = new ConcurrentQueue<int>(Enumerable.Range(0, 5));
            Assert.Equal(5, c.Count);
        }

        [Fact]
        public void ICollection_IsSynchronized_AlwaysFalse()
        {
            ICollection c = new ConcurrentQueue<int>(Enumerable.Range(0, 5));
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void ICollection_SyncRoot_AlwaysNull()
        {
            ICollection c = new ConcurrentQueue<int>(Enumerable.Range(0, 5));
            Assert.Throws<NotSupportedException>(() => c.SyncRoot);
        }

        [Fact]
        public void ICollection_CopyTo_InvalidArg_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ICollection)new ConcurrentQueue<int>()).CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ICollection)new ConcurrentQueue<int>()).CopyTo(new int[0], -1));
            Assert.Throws<ArgumentException>(() => ((ICollection)new ConcurrentQueue<int>()).CopyTo(new int[0], 1));
        }

        [Fact]
        public void IProducerConsumerCollection_TryAddTryTake_Success()
        {
            IProducerConsumerCollection<int> pcc = new ConcurrentQueue<int>();

            Assert.True(pcc.TryAdd(1));
            Assert.True(pcc.TryAdd(2));

            int item;
            Assert.True(pcc.TryTake(out item));
            Assert.Equal(1, item);
            Assert.True(pcc.TryTake(out item));
            Assert.Equal(2, item);
        }

        [Fact]
        public void IEnumerable_GetAllExpectedItems()
        {
            IEnumerable<int> enumerable1 = new ConcurrentQueue<int>(Enumerable.Range(1, 5));
            IEnumerable enumerable2 = enumerable1;

            int expectedNext = 1;
            using (IEnumerator<int> enumerator1 = enumerable1.GetEnumerator())
            {
                IEnumerator enumerator2 = enumerable2.GetEnumerator();
                while (enumerator1.MoveNext())
                {
                    Assert.True(enumerator2.MoveNext());

                    Assert.Equal(expectedNext, enumerator1.Current);
                    Assert.Equal(expectedNext, enumerator2.Current);

                    expectedNext++;
                }
                Assert.False(enumerator2.MoveNext());
                Assert.Equal(expectedNext, 6);
            }
        }

        [Fact]
        public void ReferenceTypes_NulledAfterDequeue()
        {
            int iterations = 10; // any number <32 will do
            var mres = new ManualResetEventSlim[iterations];

            // Separated out into another method to ensure that even in debug
            // the JIT doesn't force anything to be kept alive for longer than we need.
            var queue = ((Func<ConcurrentQueue<Finalizable>>)(() =>
            {
                var q = new ConcurrentQueue<Finalizable>();

                for (int i = 0; i < iterations; i++)
                {
                    mres[i] = new ManualResetEventSlim();
                    q.Enqueue(new Finalizable(mres[i]));
                }

                for (int i = 0; i < iterations; i++)
                {
                    Finalizable temp;
                    Assert.True(q.TryDequeue(out temp));
                }

                return q;
            }))();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.True(Array.TrueForAll(mres, e => e.IsSet));

            GC.KeepAlive(queue);
        }

        /// <summary>Sets an event when finalized.</summary>
        private sealed class Finalizable
        {
            private ManualResetEventSlim _mres;

            public Finalizable(ManualResetEventSlim mres) { _mres = mres; }

            ~Finalizable() { _mres.Set(); }
        }
        
    }
}
