// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private const int UnexpectedTimeoutMilliseconds = ThreadTestHelpers.UnexpectedTimeoutMilliseconds;
        private const int ExpectedTimeoutMilliseconds = ThreadTestHelpers.ExpectedTimeoutMilliseconds;
        private const int MaxPossibleThreadCount = 0x7fff;

        static ThreadPoolTests()
        {
            // Run the following tests before any others
            ConcurrentInitializeTest();
        }

        public static IEnumerable<object[]> OneBool() =>
            from b in new[] { true, false }
            select new object[] { b };

        public static IEnumerable<object[]> TwoBools() =>
            from b1 in new[] { true, false }
            from b2 in new[] { true, false }
            select new object[] { b1, b2 };

        // Tests concurrent calls to ThreadPool.SetMinThreads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ConcurrentInitializeTest()
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

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15164", TestRuntimes.Mono)]
        public static void SetMinMaxThreadsTest()
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
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/32020", TestRuntimes.Mono)]
        public static void SetMinMaxThreadsTest_ChangedInDotNetCore()
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void SetMinThreadsTo0Test()
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
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void QueueRegisterPositiveAndFlowTest()
        {
            var asyncLocal = new AsyncLocal<int>();
            asyncLocal.Value = 1;

            var obj = new object();
            var registerWaitEvent = new AutoResetEvent(false);
            var threadDone = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle = null;
            Exception backgroundEx = null;
            int backgroundAsyncLocalValue = 0;

            Action<bool, Action> commonBackgroundTest =
                (isRegisteredWaitCallback, test) =>
                {
                    try
                    {
                        if (isRegisteredWaitCallback)
                        {
                            RegisteredWaitHandle toUnregister = registeredWaitHandle;
                            registeredWaitHandle = null;
                            Assert.True(toUnregister.Unregister(threadDone));
                        }
                        test();
                        backgroundAsyncLocalValue = asyncLocal.Value;
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                    }
                    finally
                    {
                        if (!isRegisteredWaitCallback)
                        {
                            threadDone.Set();
                        }
                    }
                };
            Action<bool> waitForBackgroundWork =
                isWaitForRegisteredWaitCallback =>
                {
                    if (isWaitForRegisteredWaitCallback)
                    {
                        registerWaitEvent.Set();
                    }
                    threadDone.CheckedWait();
                    if (backgroundEx != null)
                    {
                        throw new AggregateException(backgroundEx);
                    }
                };

            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    commonBackgroundTest(false, () =>
                    {
                        Assert.Same(obj, state);
                    });
                },
                obj);
            waitForBackgroundWork(false);
            Assert.Equal(1, backgroundAsyncLocalValue);

            ThreadPool.UnsafeQueueUserWorkItem(
                state =>
                {
                    commonBackgroundTest(false, () =>
                    {
                        Assert.Same(obj, state);
                    });
                },
                obj);
            waitForBackgroundWork(false);
            Assert.Equal(0, backgroundAsyncLocalValue);

            registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(
                    registerWaitEvent,
                    (state, timedOut) =>
                    {
                        commonBackgroundTest(true, () =>
                        {
                            Assert.Same(obj, state);
                            Assert.False(timedOut);
                        });
                    },
                    obj,
                    UnexpectedTimeoutMilliseconds,
                    false);
            waitForBackgroundWork(true);
            Assert.Equal(1, backgroundAsyncLocalValue);

            registeredWaitHandle =
                ThreadPool.UnsafeRegisterWaitForSingleObject(
                    registerWaitEvent,
                    (state, timedOut) =>
                    {
                        commonBackgroundTest(true, () =>
                        {
                            Assert.Same(obj, state);
                            Assert.False(timedOut);
                        });
                    },
                    obj,
                    UnexpectedTimeoutMilliseconds,
                    false);
            waitForBackgroundWork(true);
            Assert.Equal(0, backgroundAsyncLocalValue);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void QueueRegisterNegativeTest()
        {
            Assert.Throws<ArgumentNullException>(() => ThreadPool.QueueUserWorkItem(null));
            Assert.Throws<ArgumentNullException>(() => ThreadPool.UnsafeQueueUserWorkItem(null, null));

            WaitHandle waitHandle = new ManualResetEvent(true);
            WaitOrTimerCallback callback = (state, timedOut) => { };
            Assert.Throws<ArgumentNullException>(() => ThreadPool.RegisterWaitForSingleObject(null, callback, null, 0, true));
            Assert.Throws<ArgumentNullException>(() => ThreadPool.RegisterWaitForSingleObject(waitHandle, null, null, 0, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, -2, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, (long)-2, true));
            if (!PlatformDetection.IsNetFramework) // .NET Framework silently overflows the timeout
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                    ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, (long)int.MaxValue + 1, true));
            }
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, TimeSpan.FromMilliseconds(-2), true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () =>
                ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    callback,
                    null,
                    TimeSpan.FromMilliseconds((double)int.MaxValue + 1),
                    true));

            Assert.Throws<ArgumentNullException>(() => ThreadPool.UnsafeRegisterWaitForSingleObject(null, callback, null, 0, true));
            Assert.Throws<ArgumentNullException>(() => ThreadPool.UnsafeRegisterWaitForSingleObject(waitHandle, null, null, 0, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                ThreadPool.UnsafeRegisterWaitForSingleObject(waitHandle, callback, null, -2, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                ThreadPool.UnsafeRegisterWaitForSingleObject(waitHandle, callback, null, (long)-2, true));
            if (!PlatformDetection.IsNetFramework) // .NET Framework silently overflows the timeout
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeOutInterval", () =>
                    ThreadPool.UnsafeRegisterWaitForSingleObject(waitHandle, callback, null, (long)int.MaxValue + 1, true));
            }
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () =>
                ThreadPool.UnsafeRegisterWaitForSingleObject(waitHandle, callback, null, TimeSpan.FromMilliseconds(-2), true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () =>
                ThreadPool.UnsafeRegisterWaitForSingleObject(
                    waitHandle,
                    callback,
                    null,
                    TimeSpan.FromMilliseconds((double)int.MaxValue + 1),
                    true));
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

        [ConditionalFact(nameof(HasAtLeastThreeProcessorsAndRemoteExecutorSupported))]
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

        public static bool HasAtLeastThreeProcessorsAndRemoteExecutorSupported => Environment.ProcessorCount >= 3 && RemoteExecutor.IsSupported;
    }
}
