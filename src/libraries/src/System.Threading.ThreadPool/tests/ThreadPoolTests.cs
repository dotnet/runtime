// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading.Tests;
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
            if (!PlatformDetection.IsNetNative)
            {
                ConcurrentInitializeTest();
            }
        }

        // Tests concurrent calls to ThreadPool.SetMinThreads
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "ThreadPool.SetMinThreads is not supported on UapAot.")]
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

        [Fact]
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

        [Fact]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "ThreadPool.SetMinThreads and SetMaxThreads are not supported on UapAot.")]
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
                Assert.True(ThreadPool.SetMaxThreads(-1, -1));
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
        // Desktop framework doesn't check for this and instead, hits an assertion failure
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "ThreadPool.SetMinThreads and SetMaxThreads are not supported on UapAot.")]
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

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Triggers an assertion failure.")]
        private static void SetMinThreadsTo0Test()
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

        [Fact]
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

        [Fact]
        public static void QueueRegisterNegativeTest()
        {
            Assert.Throws<ArgumentNullException>(() => ThreadPool.QueueUserWorkItem(null));
            Assert.Throws<ArgumentNullException>(() => ThreadPool.UnsafeQueueUserWorkItem(null, null));

            WaitHandle waitHandle = new ManualResetEvent(true);
            WaitOrTimerCallback callback = (state, timedOut) => { };
            Assert.Throws<ArgumentNullException>(() => ThreadPool.RegisterWaitForSingleObject(null, callback, null, 0, true));
            Assert.Throws<ArgumentNullException>(() => ThreadPool.RegisterWaitForSingleObject(waitHandle, null, null, 0, true));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, -2, true));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, (long)-2, true));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreadPool.RegisterWaitForSingleObject(waitHandle, callback, null, TimeSpan.FromMilliseconds(-2), true));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    callback,
                    null,
                    TimeSpan.FromMilliseconds((double)int.MaxValue + 1),
                    true));
        }
    }
}
