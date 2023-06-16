// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class LockTests
    {
        private const int FailTimeoutMilliseconds = 30000;

        // Attempts a single recursive acquisition/release cycle of a newly-created lock.
        [Fact]
        public static void BasicRecursion()
        {
            Lock lockObj = new();
            Assert.True(lockObj.TryEnter());
            Assert.True(lockObj.TryEnter());
            lockObj.Exit();
            Assert.True(lockObj.IsHeldByCurrentThread);
            lockObj.Enter();
            Assert.True(lockObj.IsHeldByCurrentThread);
            lockObj.Exit();
            using (lockObj.EnterScope())
            {
                Assert.True(lockObj.IsHeldByCurrentThread);
            }
            Assert.True(lockObj.IsHeldByCurrentThread);
            lockObj.Exit();
            Assert.False(lockObj.IsHeldByCurrentThread);
        }

        // Attempts to overflow the recursion count of a newly-created lock.
        [Fact]
        public static void DeepRecursion()
        {
            Lock lockObj = new();
            var hc = lockObj.GetHashCode();
            // reduced from "(long)int.MaxValue + 2;" to something that will return in a more meaningful time
            const int limit = 10000;

            for (var i = 0L; i < limit; i++)
            {
                Assert.True(lockObj.TryEnter());
            }

            for (var j = 0L; j < (limit - 1); j++)
            {
                lockObj.Exit();
                Assert.True(lockObj.IsHeldByCurrentThread);
            }

            lockObj.Exit();
            Assert.False(lockObj.IsHeldByCurrentThread);
        }

        [Fact]
        public static void IsHeldByCurrentThread()
        {
            Lock lockObj = new();
            Assert.False(lockObj.IsHeldByCurrentThread);
            using (lockObj.EnterScope())
            {
                Assert.True(lockObj.IsHeldByCurrentThread);
            }
            Assert.False(lockObj.IsHeldByCurrentThread);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void IsHeldByCurrentThread_WhenHeldBySomeoneElse()
        {
            Lock lockObj = new();
            var b = new Barrier(2);

            Task t = Task.Run(() =>
            {
                using (lockObj.EnterScope())
                {
                    b.SignalAndWait();
                    Assert.True(lockObj.IsHeldByCurrentThread);
                    b.SignalAndWait();
                }
            });

            b.SignalAndWait();
            Assert.False(lockObj.IsHeldByCurrentThread);
            b.SignalAndWait();

            t.Wait();
        }

        [Fact]
        public static void Exit_Invalid()
        {
            Lock lockObj = new();
            Assert.Throws<SynchronizationLockException>(() => lockObj.Exit());
            default(Lock.Scope).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Exit_WhenHeldBySomeoneElse_ThrowsSynchronizationLockException()
        {
            Lock lockObj = new();
            Lock.Scope lockScope;
            var b = new Barrier(2);

            Task t = Task.Run(() =>
            {
                using (lockScope = lockObj.EnterScope())
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }
            });

            b.SignalAndWait();
            Assert.Throws<SynchronizationLockException>(() => lockObj.Exit());
            Assert.Throws<SynchronizationLockException>(() => lockScope.Dispose());
            b.SignalAndWait();

            t.Wait();
        }

        [Fact]
        public static void TryEnter_Invalid()
        {
            Lock lockObj = new();

            Assert.Throws<ArgumentOutOfRangeException>(() => lockObj.TryEnter(-2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "timeout", () => lockObj.TryEnter(TimeSpan.FromMilliseconds(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "timeout",
                () => lockObj.TryEnter(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Enter_HasToWait()
        {
            Lock lockObj = new();

            // When the current thread has the lock, have background threads wait for the lock in various ways. After a short
            // duration, release the lock and allow the background threads to acquire the lock.
            {
                var backgroundTestDelegates = new List<Action>();
                Barrier readyBarrier = null;

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    lockObj.Enter();
                    lockObj.Exit();
                });

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    using (lockObj.EnterScope())
                    {
                    }
                });

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.True(lockObj.TryEnter(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                    lockObj.Exit();
                });

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.True(lockObj.TryEnter(TimeSpan.FromMilliseconds(ThreadTestHelpers.UnexpectedTimeoutMilliseconds)));
                    lockObj.Exit();
                });

                int testCount = backgroundTestDelegates.Count;
                readyBarrier = new Barrier(testCount + 1); // plus main thread
                var waitForThreadArray = new Action[testCount];
                for (int i = 0; i < backgroundTestDelegates.Count; ++i)
                {
                    int icopy = i; // for use in delegates
                    Thread t =
                        ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i],
                            () => backgroundTestDelegates[icopy]());
                    t.IsBackground = true;
                    t.Start();
                }

                using (lockObj.EnterScope())
                {
                    readyBarrier.SignalAndWait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                    Thread.Sleep(ThreadTestHelpers.ExpectedTimeoutMilliseconds);
                }
                foreach (Action waitForThread in waitForThreadArray)
                    waitForThread();
            }

            // When the current thread has the lock, have background threads wait for the lock in various ways and time out
            // after a short duration
            {
                var backgroundTestDelegates = new List<Action>();
                Barrier readyBarrier = null;

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.False(lockObj.TryEnter(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
                });

                backgroundTestDelegates.Add(() =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.False(lockObj.TryEnter(TimeSpan.FromMilliseconds(ThreadTestHelpers.ExpectedTimeoutMilliseconds)));
                });

                int testCount = backgroundTestDelegates.Count;
                readyBarrier = new Barrier(testCount + 1); // plus main thread
                var waitForThreadArray = new Action[testCount];
                for (int i = 0; i < backgroundTestDelegates.Count; ++i)
                {
                    int icopy = i; // for use in delegates
                    Thread t =
                        ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i],
                            () => backgroundTestDelegates[icopy]());
                    t.IsBackground = true;
                    t.Start();
                }

                using (lockObj.EnterScope())
                {
                    readyBarrier.SignalAndWait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                    foreach (Action waitForThread in waitForThreadArray)
                        waitForThread();
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void Enter_HasToWait_LockContentionCountTest()
        {
            long initialLockContentionCount = Monitor.LockContentionCount;
            Enter_HasToWait();
            Assert.True(Monitor.LockContentionCount - initialLockContentionCount >= 2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49521", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static void InterruptWaitTest()
        {
            Lock lockObj = new();
            using (lockObj.EnterScope())
            {
                var threadReady = new AutoResetEvent(false);
                var t =
                    ThreadTestHelpers.CreateGuardedThread(out Action waitForThread, () =>
                    {
                        threadReady.Set();
                        Assert.Throws<ThreadInterruptedException>(() => lockObj.Enter());
                    });
                t.IsBackground = true;
                t.Start();
                threadReady.CheckedWait();
                t.Interrupt();
                waitForThread();
            }
        }
    }
}
