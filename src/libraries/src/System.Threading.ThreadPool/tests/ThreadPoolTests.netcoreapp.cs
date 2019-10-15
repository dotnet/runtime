// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.ThreadPools.Tests
{
    public partial class ThreadPoolTests
    {
        public static IEnumerable<object[]> OneBool() =>
            from b in new[] { true, false }
            select new object[] { b };

        public static IEnumerable<object[]> TwoBools() =>
            from b1 in new[] { true, false }
            from b2 in new[] { true, false }
            select new object[] { b1, b2 };

        [Theory]
        [MemberData(nameof(TwoBools))]
        public void QueueUserWorkItem_PreferLocal_InvalidArguments_Throws(bool preferLocal, bool useUnsafe)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => useUnsafe ?
                ThreadPool.UnsafeQueueUserWorkItem(null, new object(), preferLocal) :
                ThreadPool.QueueUserWorkItem(null, new object(), preferLocal));
        }

        [Theory]
        [MemberData(nameof(TwoBools))]
        public async Task QueueUserWorkItem_PreferLocal_NullValidForState(bool preferLocal, bool useUnsafe)
        {
            var tcs = new TaskCompletionSource<int>();
            if (useUnsafe)
            {
                ThreadPool.UnsafeQueueUserWorkItem(s => tcs.SetResult(84), (object)null, preferLocal);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(s => tcs.SetResult(84), (object)null, preferLocal);
            }
            Assert.Equal(84, await tcs.Task);
        }

        [Theory]
        [MemberData(nameof(TwoBools))]
        public async Task QueueUserWorkItem_PreferLocal_ReferenceTypeStateObjectPassedThrough(bool preferLocal, bool useUnsafe)
        {
            var tcs = new TaskCompletionSource<int>();
            if (useUnsafe)
            {
                ThreadPool.UnsafeQueueUserWorkItem(s => s.SetResult(84), tcs, preferLocal);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(s => s.SetResult(84), tcs, preferLocal);
            }
            Assert.Equal(84, await tcs.Task);
        }

        [Theory]
        [MemberData(nameof(TwoBools))]
        public async Task QueueUserWorkItem_PreferLocal_ValueTypeStateObjectPassedThrough(bool preferLocal, bool useUnsafe)
        {
            var tcs = new TaskCompletionSource<int>();
            if (useUnsafe)
            {
                ThreadPool.UnsafeQueueUserWorkItem(s => s.tcs.SetResult(s.value), (tcs, value: 42), preferLocal);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(s => s.tcs.SetResult(s.value), (tcs, value: 42), preferLocal);
            }
            Assert.Equal(42, await tcs.Task);
        }

        [Theory]
        [MemberData(nameof(TwoBools))]
        public async Task QueueUserWorkItem_PreferLocal_RunsAsynchronously(bool preferLocal, bool useUnsafe)
        {
            await Task.Factory.StartNew(() =>
            {
                int origThread = Environment.CurrentManagedThreadId;
                var tcs = new TaskCompletionSource<int>();
                if (useUnsafe)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(s => s.SetResult(Environment.CurrentManagedThreadId), tcs, preferLocal);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(s => s.SetResult(Environment.CurrentManagedThreadId), tcs, preferLocal);
                }
                Assert.NotEqual(origThread, tcs.Task.GetAwaiter().GetResult());
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        [Theory]
        [MemberData(nameof(TwoBools))]
        public async Task QueueUserWorkItem_PreferLocal_ExecutionContextFlowedIfSafe(bool preferLocal, bool useUnsafe)
        {
            var tcs = new TaskCompletionSource<int>();
            var asyncLocal = new AsyncLocal<int>() { Value = 42 };
            if (useUnsafe)
            {
                ThreadPool.UnsafeQueueUserWorkItem(s => s.SetResult(asyncLocal.Value), tcs, preferLocal);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(s => s.SetResult(asyncLocal.Value), tcs, preferLocal);
            }
            asyncLocal.Value = 0;
            Assert.Equal(useUnsafe ? 0 : 42, await tcs.Task);
        }

        [Theory]
        [MemberData(nameof(OneBool))]
        public void UnsafeQueueUserWorkItem_IThreadPoolWorkItem_Invalid_Throws(bool preferLocal)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(null, preferLocal));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(new InvalidWorkItemAndTask(() => { }), preferLocal));
        }

        [Theory]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ManyIndividualItems_AllInvoked(bool preferLocal)
        {
            TaskCompletionSource<bool>[] tasks = Enumerable.Range(0, 100).Select(_ => new TaskCompletionSource<bool>()).ToArray();
            for (int i = 0; i < tasks.Length; i++)
            {
                int localI = i;
                ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
                {
                    tasks[localI].TrySetResult(true);
                }), preferLocal);
            }
            await Task.WhenAll(tasks.Select(t => t.Task));
        }

        [Theory]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_SameObjectReused_AllInvoked(bool preferLocal)
        {
            const int Iters = 100;
            int remaining = Iters;
            var tcs = new TaskCompletionSource<bool>();
            var workItem = new SimpleWorkItem(() =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    tcs.TrySetResult(true);
                }
            });
            for (int i = 0; i < Iters; i++)
            {
                ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal);
            }
            await tcs.Task;
            Assert.Equal(0, remaining);
        }

        [Theory]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ExecutionContextNotFlowed(bool preferLocal)
        {
            var al = new AsyncLocal<int> { Value = 42 };
            var tcs = new TaskCompletionSource<bool>();
            ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
            {
                Assert.Equal(0, al.Value);
                tcs.TrySetResult(true);
            }), preferLocal);
            await tcs.Task;
            Assert.Equal(42, al.Value);
        }

        private sealed class SimpleWorkItem : IThreadPoolWorkItem
        {
            private readonly Action _action;
            public SimpleWorkItem(Action action) => _action = action;
            public void Execute() => _action();
        }

        private sealed class InvalidWorkItemAndTask : Task, IThreadPoolWorkItem
        {
            public InvalidWorkItemAndTask(Action action) : base(action) { }
            public void Execute() { }
        }

        [ConditionalFact(nameof(HasAtLeastThreeProcessors))]
        public void MetricsTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                int processorCount = Environment.ProcessorCount;
                if (processorCount <= 2)
                {
                    return;
                }

                bool waitForWorkStart = false;
                var workStarted = new AutoResetEvent(false);
                var localWorkScheduled = new AutoResetEvent(false);
                int completeWork = 0;
                int queuedWorkCount = 0;
                var allWorkCompleted = new ManualResetEvent(false);
                Exception backgroundEx = null;
                Action work = () =>
                {
                    if (waitForWorkStart)
                    {
                        workStarted.Set();
                    }
                    try
                    {
                        // Blocking can affect thread pool thread injection heuristics, so don't block, pretend like a
                        // long-running CPU-bound work item
                        ThreadTestHelpers.WaitForConditionWithoutRelinquishingTimeSlice(
                                () => Interlocked.CompareExchange(ref completeWork, 0, 0) != 0);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref backgroundEx, ex, null);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref queuedWorkCount) == 0)
                        {
                            allWorkCompleted.Set();
                        }
                    }
                };
                WaitCallback threadPoolGlobalWork = data => work();
                Action<object> threadPoolLocalWork = data => work();
                WaitCallback scheduleThreadPoolLocalWork = data =>
                {
                    try
                    {
                        int n = (int)data;
                        for (int i = 0; i < n; ++i)
                        {
                            ThreadPool.QueueUserWorkItem(threadPoolLocalWork, null, preferLocal: true);
                            if (waitForWorkStart)
                            {
                                workStarted.CheckedWait();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref backgroundEx, ex, null);
                    }
                    finally
                    {
                        localWorkScheduled.Set();
                    }
                };

                var signaledEvent = new ManualResetEvent(true);
                var timers = new List<Timer>();
                int totalWorkCountToQueue = 0;
                Action scheduleWork = () =>
                {
                    Assert.True(queuedWorkCount < totalWorkCountToQueue);

                    int workCount = (totalWorkCountToQueue - queuedWorkCount) / 2;
                    if (workCount > 0)
                    {
                        queuedWorkCount += workCount;
                        ThreadPool.QueueUserWorkItem(scheduleThreadPoolLocalWork, workCount);
                        localWorkScheduled.CheckedWait();
                    }

                    for (; queuedWorkCount < totalWorkCountToQueue; ++queuedWorkCount)
                    {
                        ThreadPool.QueueUserWorkItem(threadPoolGlobalWork);
                        if (waitForWorkStart)
                        {
                            workStarted.CheckedWait();
                        }
                    }
                };

                Interlocked.MemoryBarrierProcessWide(); // get a reasonably accurate value for the following
                long initialCompletedWorkItemCount = ThreadPool.CompletedWorkItemCount;

                try
                {
                    // Schedule some simultaneous work that would all be scheduled and verify the thread count
                    totalWorkCountToQueue = processorCount - 2;
                    Assert.True(totalWorkCountToQueue >= 1);
                    waitForWorkStart = true;
                    scheduleWork();
                    Assert.True(ThreadPool.ThreadCount >= totalWorkCountToQueue);

                    int runningWorkItemCount = queuedWorkCount;

                    // Schedule more work that would not all be scheduled and roughly verify the pending work item count
                    totalWorkCountToQueue = processorCount * 64;
                    waitForWorkStart = false;
                    scheduleWork();
                    int minExpectedPendingWorkCount = Math.Max(1, queuedWorkCount - runningWorkItemCount * 8);
                    ThreadTestHelpers.WaitForCondition(() => ThreadPool.PendingWorkItemCount >= minExpectedPendingWorkCount);
                }
                finally
                {
                    // Complete the work
                    Interlocked.Exchange(ref completeWork, 1);
                }

                // Wait for work items to exit, for counting
                allWorkCompleted.CheckedWait();
                backgroundEx = Interlocked.CompareExchange(ref backgroundEx, null, null);
                if (backgroundEx != null)
                {
                    throw new AggregateException(backgroundEx);
                }

                // Verify the completed work item count
                ThreadTestHelpers.WaitForCondition(() =>
                {
                    Interlocked.MemoryBarrierProcessWide(); // get a reasonably accurate value for the following
                    return ThreadPool.CompletedWorkItemCount - initialCompletedWorkItemCount >= totalWorkCountToQueue;
                });
            }).Dispose();
        }

        public static bool HasAtLeastThreeProcessors => Environment.ProcessorCount >= 3;
    }
}
