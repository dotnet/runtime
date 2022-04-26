// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.ThreadPools.Tests
{
    public partial class ThreadPoolTests
    {
        private const int UnexpectedTimeoutMilliseconds = ThreadTestHelpers.UnexpectedTimeoutMilliseconds;
        private const int ExpectedTimeoutMilliseconds = ThreadTestHelpers.ExpectedTimeoutMilliseconds;

        private static readonly int MaxPossibleThreadCount = short.MaxValue;

        static ThreadPoolTests()
        {
            // Run the following tests before any others
            if (IsThreadingAndRemoteExecutorSupported)
            {
                ConcurrentInitializeTest();
            }
        }

        public static IEnumerable<object[]> OneBool() =>
            from b in new[] { true, false }
            select new object[] { b };

        public static IEnumerable<object[]> TwoBools() =>
            from b1 in new[] { true, false }
            from b2 in new[] { true, false }
            select new object[] { b1, b2 };

        // Tests concurrent calls to ThreadPool.SetMinThreads. Invoked from the static constructor.
        private static void ConcurrentInitializeTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                int processorCount = Environment.ProcessorCount;
                var countdownEvent = new CountdownEvent(processorCount);
                Action threadMain =
                    () =>
                    {
                        countdownEvent.Signal();
                        countdownEvent.Wait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                        Assert.True(ThreadPool.SetMinThreads(processorCount, processorCount));
                    };

                var waitForThreadArray = new Action[processorCount];
                for (int i = 0; i < processorCount; ++i)
                {
                    var t = ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i], threadMain);
                    t.IsBackground = true;
                    t.Start();
                }

                foreach (Action waitForThread in waitForThreadArray)
                {
                    waitForThread();
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void GetMinMaxThreadsTest()
        {
            int minw, minc;
            ThreadPool.GetMinThreads(out minw, out minc);
            Assert.True(minw >= 0);
            Assert.True(minc >= 0);

            int maxw, maxc;
            ThreadPool.GetMaxThreads(out maxw, out maxc);
            Assert.True(minw <= maxw);
            Assert.True(minc <= maxc);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void GetAvailableThreadsTest()
        {
            int w, c;
            ThreadPool.GetAvailableThreads(out w, out c);
            Assert.True(w >= 0);
            Assert.True(c >= 0);

            int maxw, maxc;
            ThreadPool.GetMaxThreads(out maxw, out maxc);
            Assert.True(w <= maxw);
            Assert.True(c <= maxc);
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        [ActiveIssue("https://github.com/mono/mono/issues/15164", TestRuntimes.Mono)]
        public static void SetMinMaxThreadsTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                int minw, minc, maxw, maxc;
                ThreadPool.GetMinThreads(out minw, out minc);
                ThreadPool.GetMaxThreads(out maxw, out maxc);

                try
                {
                    int mint = Environment.ProcessorCount * 2;
                    int maxt = mint + 1;
                    ThreadPool.SetMinThreads(mint, mint);
                    ThreadPool.SetMaxThreads(maxt, maxt);

                    Assert.False(ThreadPool.SetMinThreads(maxt + 1, mint));
                    Assert.False(ThreadPool.SetMinThreads(mint, maxt + 1));
                    Assert.False(ThreadPool.SetMinThreads(MaxPossibleThreadCount, mint));
                    Assert.False(ThreadPool.SetMinThreads(mint, MaxPossibleThreadCount));
                    Assert.False(ThreadPool.SetMinThreads(MaxPossibleThreadCount + 1, mint));
                    Assert.False(ThreadPool.SetMinThreads(mint, MaxPossibleThreadCount + 1));
                    Assert.False(ThreadPool.SetMinThreads(-1, mint));
                    Assert.False(ThreadPool.SetMinThreads(mint, -1));

                    Assert.False(ThreadPool.SetMaxThreads(mint - 1, maxt));
                    Assert.False(ThreadPool.SetMaxThreads(maxt, mint - 1));

                    VerifyMinThreads(mint, mint);
                    VerifyMaxThreads(maxt, maxt);

                    Assert.True(ThreadPool.SetMaxThreads(MaxPossibleThreadCount, MaxPossibleThreadCount));
                    VerifyMaxThreads(MaxPossibleThreadCount, MaxPossibleThreadCount);
                    Assert.True(ThreadPool.SetMaxThreads(MaxPossibleThreadCount + 1, MaxPossibleThreadCount + 1));
                    VerifyMaxThreads(MaxPossibleThreadCount, MaxPossibleThreadCount);
                    Assert.Equal(PlatformDetection.IsNetFramework, ThreadPool.SetMaxThreads(-1, -1));
                    VerifyMaxThreads(MaxPossibleThreadCount, MaxPossibleThreadCount);

                    Assert.True(ThreadPool.SetMinThreads(MaxPossibleThreadCount, MaxPossibleThreadCount));
                    VerifyMinThreads(MaxPossibleThreadCount, MaxPossibleThreadCount);

                    Assert.False(ThreadPool.SetMinThreads(MaxPossibleThreadCount + 1, MaxPossibleThreadCount));
                    Assert.False(ThreadPool.SetMinThreads(MaxPossibleThreadCount, MaxPossibleThreadCount + 1));
                    Assert.False(ThreadPool.SetMinThreads(-1, MaxPossibleThreadCount));
                    Assert.False(ThreadPool.SetMinThreads(MaxPossibleThreadCount, -1));
                    VerifyMinThreads(MaxPossibleThreadCount, MaxPossibleThreadCount);

                    Assert.True(ThreadPool.SetMinThreads(0, 0));
                    Assert.True(ThreadPool.SetMaxThreads(1, 1));
                    VerifyMaxThreads(1, 1);
                    Assert.True(ThreadPool.SetMinThreads(1, 1));
                    VerifyMinThreads(1, 1);
                }
                finally
                {
                    Assert.True(ThreadPool.SetMaxThreads(maxw, maxc));
                    VerifyMaxThreads(maxw, maxc);
                    Assert.True(ThreadPool.SetMinThreads(minw, minc));
                    VerifyMinThreads(minw, minc);
                }
            }).Dispose();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void SetMinMaxThreadsTest_ChangedInDotNetCore()
        {
            RemoteExecutor.Invoke(() =>
            {
                int minw, minc, maxw, maxc;
                ThreadPool.GetMinThreads(out minw, out minc);
                ThreadPool.GetMaxThreads(out maxw, out maxc);

                try
                {
                    Assert.True(ThreadPool.SetMinThreads(0, 0));
                    VerifyMinThreads(1, 1);
                    Assert.False(ThreadPool.SetMaxThreads(0, 1));
                    Assert.False(ThreadPool.SetMaxThreads(1, 0));
                    VerifyMaxThreads(maxw, maxc);
                }
                finally
                {
                    Assert.True(ThreadPool.SetMaxThreads(maxw, maxc));
                    VerifyMaxThreads(maxw, maxc);
                    Assert.True(ThreadPool.SetMinThreads(minw, minc));
                    VerifyMinThreads(minw, minc);
                }
            }).Dispose();

            // Verify that SetMinThreads() and SetMaxThreads() return false when trying to set a different value from what is
            // configured through config
            var options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions["System.Threading.ThreadPool.MinThreads"] = "1";
            options.RuntimeConfigurationOptions["System.Threading.ThreadPool.MaxThreads"] = "2";
            RemoteExecutor.Invoke(() =>
            {
                int w, c;
                ThreadPool.GetMinThreads(out w, out c);
                Assert.Equal(1, w);
                ThreadPool.GetMaxThreads(out w, out c);
                Assert.Equal(2, w);
                Assert.True(ThreadPool.SetMinThreads(1, 1));
                Assert.True(ThreadPool.SetMaxThreads(2, 1));
                Assert.False(ThreadPool.SetMinThreads(2, 1));
                Assert.False(ThreadPool.SetMaxThreads(1, 1));
            }, options).Dispose();
        }

        private static void VerifyMinThreads(int expectedMinw, int expectedMinc)
        {
            int minw, minc;
            ThreadPool.GetMinThreads(out minw, out minc);
            Assert.Equal(expectedMinw, minw);
            Assert.Equal(expectedMinc, minc);
        }

        private static void VerifyMaxThreads(int expectedMaxw, int expectedMaxc)
        {
            int maxw, maxc;
            ThreadPool.GetMaxThreads(out maxw, out maxc);
            Assert.Equal(expectedMaxw, maxw);
            Assert.Equal(expectedMaxc, maxc);
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void SetMinThreadsTo0Test()
        {
            RemoteExecutor.Invoke(() =>
            {
                int minw, minc, maxw, maxc;
                ThreadPool.GetMinThreads(out minw, out minc);
                ThreadPool.GetMaxThreads(out maxw, out maxc);

                try
                {
                    Assert.True(ThreadPool.SetMinThreads(0, minc));
                    Assert.True(ThreadPool.SetMaxThreads(1, maxc));

                    int count = 0;
                    var done = new ManualResetEvent(false);
                    WaitCallback callback = null;
                    callback = state =>
                    {
                        ++count;
                        if (count > 100)
                        {
                            done.Set();
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(callback);
                        }
                    };
                    ThreadPool.QueueUserWorkItem(callback);
                    done.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                }
                finally
                {
                    Assert.True(ThreadPool.SetMaxThreads(maxw, maxc));
                    Assert.True(ThreadPool.SetMinThreads(minw, minc));
                }
            }).Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(TwoBools))]
        public void QueueUserWorkItem_PreferLocal_InvalidArguments_Throws(bool preferLocal, bool useUnsafe)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => useUnsafe ?
                ThreadPool.UnsafeQueueUserWorkItem(null, new object(), preferLocal) :
                ThreadPool.QueueUserWorkItem(null, new object(), preferLocal));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(OneBool))]
        public void UnsafeQueueUserWorkItem_IThreadPoolWorkItem_Invalid_Throws(bool preferLocal)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(null, preferLocal));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(new InvalidWorkItemAndTask(() => { }), preferLocal));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ManyIndividualItems_AllInvoked(bool preferLocal)
        {
            TaskCompletionSource[] tasks = Enumerable.Range(0, 100).Select(_ => new TaskCompletionSource()).ToArray();
            for (int i = 0; i < tasks.Length; i++)
            {
                int localI = i;
                ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
                {
                    tasks[localI].TrySetResult();
                }), preferLocal);
            }
            await Task.WhenAll(tasks.Select(t => t.Task));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_SameObjectReused_AllInvoked(bool preferLocal)
        {
            const int Iters = 100;
            int remaining = Iters;
            var tcs = new TaskCompletionSource();
            var workItem = new SimpleWorkItem(() =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    tcs.TrySetResult();
                }
            });
            for (int i = 0; i < Iters; i++)
            {
                ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal);
            }
            await tcs.Task;
            Assert.Equal(0, remaining);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(OneBool))]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ExecutionContextNotFlowed(bool preferLocal)
        {
            var al = new AsyncLocal<int> { Value = 42 };
            var tcs = new TaskCompletionSource();
            ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
            {
                Assert.Equal(0, al.Value);
                tcs.TrySetResult();
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

        public static bool IsMetricsTestSupported => Environment.ProcessorCount >= 3 && IsThreadingAndRemoteExecutorSupported;

        [ConditionalFact(nameof(IsMetricsTestSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunProcessorCountItemsInParallel()
        {
            int processorCount = Environment.ProcessorCount;
            AutoResetEvent allWorkItemsStarted = new AutoResetEvent(false);
            int startedWorkItemCount = 0;
            WaitCallback workItem = _ =>
            {
                if (Interlocked.Increment(ref startedWorkItemCount) == processorCount)
                {
                    allWorkItemsStarted.Set();
                }
            };

            // Run the test twice to make sure we can reuse the threads.
            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < processorCount; ++i)
                {
                    ThreadPool.QueueUserWorkItem(workItem);
                }

                allWorkItemsStarted.CheckedWait();
                Interlocked.Exchange(ref startedWorkItemCount, 0);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/43754", TestPlatforms.Android)]
        public static void ThreadPoolCanPickUpOneOrMoreWorkItemsWhenThreadIsAvailable()
        {
            int processorCount = Environment.ProcessorCount;
            AutoResetEvent allBlockingWorkItemsStarted = new AutoResetEvent(false);
            AutoResetEvent allTestWorkItemsStarted = new AutoResetEvent(false);
            ManualResetEvent unblockWorkItems = new ManualResetEvent(false);
            int startedBlockingWorkItemCount = 0;
            int startedTestWorkItemCount = 0;
            WaitCallback blockingWorkItem = _ =>
            {
                if (Interlocked.Increment(ref startedBlockingWorkItemCount) == processorCount - 1)
                {
                    allBlockingWorkItemsStarted.Set();
                }
                unblockWorkItems.CheckedWait();
            };
            WaitCallback testWorkItem = _ =>
            {
                if (Interlocked.Increment(ref startedTestWorkItemCount) == processorCount)
                {
                    allTestWorkItemsStarted.Set();
                }
            };

            for (int i = 0; i < processorCount - 1; ++i)
            {
                ThreadPool.QueueUserWorkItem(blockingWorkItem);
            }

            allBlockingWorkItemsStarted.CheckedWait();
            for (int i = 0; i < processorCount; ++i)
            {
                ThreadPool.QueueUserWorkItem(testWorkItem);
            }

            allTestWorkItemsStarted.CheckedWait();
            unblockWorkItems.Set();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunMoreThanMaxWorkItemsMakesOneWorkItemWaitForStarvationDetection()
        {
            int processorCount = Environment.ProcessorCount;
            AutoResetEvent allBlockingWorkItemsStarted = new AutoResetEvent(false);
            AutoResetEvent testWorkItemStarted = new AutoResetEvent(false);
            ManualResetEvent unblockWorkItems = new ManualResetEvent(false);
            int startedBlockingWorkItemCount = 0;
            WaitCallback blockingWorkItem = _ =>
            {
                if (Interlocked.Increment(ref startedBlockingWorkItemCount) == processorCount)
                {
                    allBlockingWorkItemsStarted.Set();
                }
                unblockWorkItems.CheckedWait();
            };

            for (int i = 0; i < processorCount; ++i)
            {
                ThreadPool.QueueUserWorkItem(blockingWorkItem);
            }

            allBlockingWorkItemsStarted.CheckedWait();
            ThreadPool.QueueUserWorkItem(_ => testWorkItemStarted.Set());
            testWorkItemStarted.CheckedWait();
            unblockWorkItems.Set();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void WorkQueueDepletionTest()
        {
            ManualResetEvent done = new ManualResetEvent(false);
            int numLocalScheduled = 1;
            int numGlobalScheduled = 1;
            int numOfEachTypeToSchedule = Environment.ProcessorCount * 64;
            int numTotalCompleted = 0;
            Action<bool> workItem = null;
            workItem = preferLocal =>
            {
                int numScheduled =
                    preferLocal ? Interlocked.Increment(ref numLocalScheduled) : Interlocked.Increment(ref numGlobalScheduled);
                if (numScheduled <= numOfEachTypeToSchedule)
                {
                    ThreadPool.QueueUserWorkItem(workItem, preferLocal, preferLocal);
                    if (Interlocked.Increment(ref numScheduled) <= numOfEachTypeToSchedule)
                    {
                        ThreadPool.QueueUserWorkItem(workItem, preferLocal, preferLocal);
                    }
                }

                if (Interlocked.Increment(ref numTotalCompleted) == numOfEachTypeToSchedule * 2)
                {
                    done.Set();
                }
            };

            ThreadPool.QueueUserWorkItem(workItem, true, preferLocal: true);
            ThreadPool.QueueUserWorkItem(workItem, false, preferLocal: false);
            done.CheckedWait();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void WorkerThreadStateResetTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                ThreadPool.GetMinThreads(out int minw, out int minc);
                ThreadPool.GetMaxThreads(out int maxw, out int maxc);
                try
                {
                    // Use maximum one worker thread to have all work items below run on the same thread
                    Assert.True(ThreadPool.SetMinThreads(1, minc));
                    Assert.True(ThreadPool.SetMaxThreads(1, maxc));

                    var done = new AutoResetEvent(false);
                    string failureMessage = string.Empty;
                    WaitCallback setNameWorkItem = null;
                    WaitCallback verifyNameWorkItem = null;
                    WaitCallback setIsBackgroundWorkItem = null;
                    WaitCallback verifyIsBackgroundWorkItem = null;
                    WaitCallback setPriorityWorkItem = null;
                    WaitCallback verifyPriorityWorkItem = null;

                    setNameWorkItem = _ =>
                    {
                        Thread.CurrentThread.Name = nameof(WorkerThreadStateResetTest);
                        ThreadPool.QueueUserWorkItem(verifyNameWorkItem);
                    };

                    verifyNameWorkItem = _ =>
                    {
                        Thread currentThread = Thread.CurrentThread;
                        if (currentThread.Name == nameof(WorkerThreadStateResetTest))
                        {
                            failureMessage += $"Name was not reset: {currentThread.Name}{Environment.NewLine}";
                        }
                        ThreadPool.QueueUserWorkItem(setIsBackgroundWorkItem);
                    };

                    setIsBackgroundWorkItem = _ =>
                    {
                        Thread.CurrentThread.IsBackground = false;
                        ThreadPool.QueueUserWorkItem(verifyIsBackgroundWorkItem);
                    };

                    verifyIsBackgroundWorkItem = _ =>
                    {
                        Thread currentThread = Thread.CurrentThread;
                        if (!currentThread.IsBackground)
                        {
                            failureMessage += $"IsBackground was not reset: {currentThread.IsBackground}{Environment.NewLine}";
                            currentThread.IsBackground = true;
                        }
                        ThreadPool.QueueUserWorkItem(setPriorityWorkItem);
                    };

                    setPriorityWorkItem = _ =>
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                        ThreadPool.QueueUserWorkItem(verifyPriorityWorkItem);
                    };

                    verifyPriorityWorkItem = _ =>
                    {
                        Thread currentThread = Thread.CurrentThread;
                        if (currentThread.Priority != ThreadPriority.Normal)
                        {
                            failureMessage += $"Priority was not reset: {currentThread.Priority}{Environment.NewLine}";
                            currentThread.Priority = ThreadPriority.Normal;
                        }
                        done.Set();
                    };

                    ThreadPool.QueueUserWorkItem(setNameWorkItem);
                    done.CheckedWait();
                    Assert.Equal(string.Empty, failureMessage);
                }
                finally
                {
                    Assert.True(ThreadPool.SetMaxThreads(maxw, maxc));
                    Assert.True(ThreadPool.SetMinThreads(minw, minc));
                }
            }).Dispose();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void SettingMinWorkerThreadsWillCreateThreadsUpToMinimum()
        {
            RemoteExecutor.Invoke(() =>
            {
                ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIocpThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIocpThreads);

                AutoResetEvent allWorkItemsExceptOneStarted = new AutoResetEvent(false);
                AutoResetEvent allWorkItemsStarted = new AutoResetEvent(false);
                ManualResetEvent unblockWorkItems = new ManualResetEvent(false);
                int startedWorkItemCount = 0;
                WaitCallback workItem = _ =>
                {
                    int newStartedWorkItemCount = Interlocked.Increment(ref startedWorkItemCount);
                    if (newStartedWorkItemCount == minWorkerThreads)
                    {
                        allWorkItemsExceptOneStarted.Set();
                    }
                    else if (newStartedWorkItemCount == minWorkerThreads + 1)
                    {
                        allWorkItemsStarted.Set();
                    }

                    unblockWorkItems.CheckedWait();
                };

                ThreadPool.SetMaxThreads(minWorkerThreads, maxIocpThreads);
                for (int i = 0; i < minWorkerThreads + 1; ++i)
                {
                    ThreadPool.QueueUserWorkItem(workItem);
                }

                allWorkItemsExceptOneStarted.CheckedWait();
                Assert.False(allWorkItemsStarted.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));

                Assert.True(ThreadPool.SetMaxThreads(minWorkerThreads + 1, maxIocpThreads));
                Assert.True(ThreadPool.SetMinThreads(minWorkerThreads + 1, minIocpThreads));
                allWorkItemsStarted.CheckedWait();

                unblockWorkItems.Set();
            }).Dispose();
        }

        // See https://github.com/dotnet/corert/pull/6822
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ThreadPoolCanProcessManyWorkItemsInParallelWithoutDeadlocking()
        {
            int processorCount = Environment.ProcessorCount;
            int iterationCount = 100_000;
            var done = new ManualResetEvent(false);

            WaitCallback workItem = null;
            workItem = _ =>
            {
                if (Interlocked.Decrement(ref iterationCount) > 0)
                {
                    ThreadPool.QueueUserWorkItem(workItem);
                }
                else
                {
                    done.Set();
                }
            };

            for (int i = 0; i < processorCount; ++i)
            {
                ThreadPool.QueueUserWorkItem(workItem);
            }

            done.CheckedWait();
        }

        [ThreadStatic]
        private static int t_ThreadPoolThreadCreationDoesNotTransferExecutionContext_asyncLocalSideEffect;

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void ThreadPoolThreadCreationDoesNotTransferExecutionContext()
        {
            // Run in a separate process to test in a clean thread pool environment such that work items queued by the test
            // would cause the thread pool to create threads
            RemoteExecutor.Invoke(() =>
            {
                var done = new AutoResetEvent(false);

                // Create an AsyncLocal with value change notifications, this changes the EC on this thread to non-default
                var asyncLocal = new AsyncLocal<int>(e =>
                {
                    // There is nothing in this test that should cause a thread's EC to change due to EC flow
                    Assert.False(e.ThreadContextChanged);

                    // Record a side-effect from AsyncLocal value changes caused by flow. This is mainly because AsyncLocal
                    // value change notifications can have side-effects like impersonation, we want to ensure that not only the
                    // AsyncLocal's value is correct, but also that the side-effect matches the value, confirming that any value
                    // changes cause matching notifications.
                    t_ThreadPoolThreadCreationDoesNotTransferExecutionContext_asyncLocalSideEffect = e.CurrentValue;
                });
                asyncLocal.Value = 1;

                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    // The EC should not have flowed. If the EC had flowed, the assertion in the value change notification would
                    // fail. Just for additional verification, check the side-effect as well.
                    Assert.Equal(0, t_ThreadPoolThreadCreationDoesNotTransferExecutionContext_asyncLocalSideEffect);

                    done.Set();
                }, null);
                done.CheckedWait();

                ThreadPool.UnsafeRegisterWaitForSingleObject(done, (_, timedOut) =>
                {
                    Assert.True(timedOut);

                    // The EC should not have flowed. If the EC had flowed, the assertion in the value change notification would
                    // fail. Just for additional verification, check the side-effect as well.
                    Assert.Equal(0, t_ThreadPoolThreadCreationDoesNotTransferExecutionContext_asyncLocalSideEffect);

                    done.Set();
                }, null, 0, true);
                done.CheckedWait();
            }).Dispose();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void CooperativeBlockingCanCreateThreadsFaster()
        {
            // Run in a separate process to test in a clean thread pool environment such that work items queued by the test
            // would cause the thread pool to create threads
            RemoteExecutor.Invoke(() =>
            {
                // All but the last of these work items will block and the last queued work item would release the blocking.
                // Without cooperative blocking, this would lead to starvation after <proc count> work items run. Since
                // starvation adds threads at a rate of at most 2 per second, the extra 120 work items would take roughly 60
                // seconds to get unblocked and since the test waits for 30 seconds it would time out. Cooperative blocking is
                // configured below to increase the rate of thread injection for testing purposes while getting a decent amount
                // of coverage for its behavior. With cooperative blocking as configured below, the test should finish within a
                // few seconds.
                int processorCount = Environment.ProcessorCount;
                int workItemCount = processorCount + 120;
                SetBlockingConfigValue("ThreadsToAddWithoutDelay_ProcCountFactor", 1);
                SetBlockingConfigValue("MaxDelayMs", 1);
                SetBlockingConfigValue("IgnoreMemoryUsage", true);

                var allWorkItemsUnblocked = new AutoResetEvent(false);

                // Run a second iteration for some extra coverage. Iterations after the first one would be much faster because
                // the necessary number of threads would already have been created by then, and would not add much to the test
                // time.
                for (int iterationIndex = 0; iterationIndex < 2; ++iterationIndex)
                {
                    var tcs = new TaskCompletionSource<int>();
                    int unblockedThreadCount = 0;

                    Action<int> blockingWorkItem = _ =>
                    {
                        tcs.Task.Wait();
                        if (Interlocked.Increment(ref unblockedThreadCount) == workItemCount - 1)
                        {
                            allWorkItemsUnblocked.Set();
                        }
                    };

                    for (int i = 0; i < workItemCount - 1; ++i)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(blockingWorkItem, 0, preferLocal: false);
                    }

                    Action<int> unblockingWorkItem = _ => tcs.SetResult(0);
                    ThreadPool.UnsafeQueueUserWorkItem(unblockingWorkItem, 0, preferLocal: false);
                    Assert.True(allWorkItemsUnblocked.WaitOne(30_000));
                }

                void SetBlockingConfigValue(string name, object value) =>
                    AppContextSetData("System.Threading.ThreadPool.Blocking." + name, value);

                void AppContextSetData(string name, object value)
                {
                    if (value is bool boolValue)
                    {
                        AppContext.SetSwitch(name, boolValue);
                    }
                    else
                    {
                        AppContext.SetData(name, value);
                    }
                }
            }).Dispose();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public static void CooperativeBlockingWithProcessingThreadsAndGoalThreadsAndAddWorkerRaceTest()
        {
            // Avoid contaminating the main process' environment
            RemoteExecutor.Invoke(() =>
            {
                try
                {
                    // The test is run affinitized to at most 2 processors for more frequent repros. The actual test process below
                    // will inherit the affinity.
                    Process testParentProcess = Process.GetCurrentProcess();
                    testParentProcess.ProcessorAffinity = (nint)testParentProcess.ProcessorAffinity & 0x3;
                }
                catch (PlatformNotSupportedException)
                {
                    // Processor affinity is not supported on some platforms, try to run the test anyway
                }

                RemoteExecutor.Invoke(() =>
                {
                    const uint TestDurationMs = 4000;

                    var done = new ManualResetEvent(false);
                    int startTimeMs = Environment.TickCount;
                    Action<object> completingTask = data => ((TaskCompletionSource<int>)data).SetResult(0);
                    Action repeatingTask = null;
                    repeatingTask = () =>
                    {
                        if ((uint)(Environment.TickCount - startTimeMs) >= TestDurationMs)
                        {
                            done.Set();
                            return;
                        }

                        Task.Run(repeatingTask);

                        var tcs = new TaskCompletionSource<int>();
                        Task.Factory.StartNew(completingTask, tcs);
                        tcs.Task.Wait();
                    };

                    for (int i = 0; i < Environment.ProcessorCount; ++i)
                    {
                        Task.Run(repeatingTask);
                    }

                    done.CheckedWait();
                }).Dispose();
            }).Dispose();
        }

        [ConditionalFact(nameof(IsThreadingAndRemoteExecutorSupported))]
        public void FileStreamFlushAsyncThreadPoolDeadlockTest()
        {
            // This test was occasionally causing the deadlock described in https://github.com/dotnet/runtime/pull/68171. Run it
            // in a remote process to test it with a dedicated thread pool.
            RemoteExecutor.Invoke(async () =>
            {
                const int OneKibibyte = 1 << 10;
                const int FourKibibytes = OneKibibyte << 2;
                const int FileSize = 1024;

                string destinationFilePath = null;
                try
                {
                    destinationFilePath = CreateFileWithRandomContent(FileSize);

                    static string CreateFileWithRandomContent(int fileSize)
                    {
                        string filePath = Path.GetTempFileName();
                        File.WriteAllBytes(filePath, CreateArray(fileSize));
                        return filePath;
                    }

                    static byte[] CreateArray(int count)
                    {
                        var result = new byte[count];
                        const int Seed = 12345;
                        var random = new Random(Seed);
                        random.NextBytes(result);
                        return result;
                    }

                    for (int j = 0; j < 100; j++)
                    {
                        using var fileStream =
                            new FileStream(
                                destinationFilePath,
                                FileMode.Create,
                                FileAccess.Write,
                                FileShare.Read,
                                FourKibibytes,
                                FileOptions.None);
                        for (int i = 0; i < FileSize; i++)
                        {
                            fileStream.WriteByte(default);
                            await fileStream.FlushAsync();
                        }
                    }
                }
                finally
                {
                    if (!string.IsNullOrEmpty(destinationFilePath) && File.Exists(destinationFilePath))
                    {
                        File.Delete(destinationFilePath);
                    }
                }
            }).Dispose();
        }

        public static bool IsThreadingAndRemoteExecutorSupported =>
            PlatformDetection.IsThreadingSupported && RemoteExecutor.IsSupported;
    }
}
