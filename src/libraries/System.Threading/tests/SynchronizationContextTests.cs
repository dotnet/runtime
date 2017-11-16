// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class SynchronizationContextTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono, "SynchronizationContext.Wait(IntPtr[], bool, int) is not implemented on Mono")]
        public static void WaitTest()
        {
            var tsc = new TestSynchronizationContext();
            Assert.Throws<ArgumentNullException>(() => tsc.Wait(null, false, 0));

            var e = new ManualResetEvent(false);
            IntPtr eventHandle = e.SafeWaitHandle.DangerousGetHandle();
            var handles = new IntPtr[] { eventHandle, eventHandle };
            Assert.Equal(WaitHandle.WaitTimeout, tsc.Wait(handles, false, 0));
            Assert.Throws<DuplicateWaitObjectException>(() => Task.Run(() => tsc.Wait(handles, true, 0)).GetAwaiter().GetResult()); // ensure Wait runs on MTA thread

            var e2 = new ManualResetEvent(false);
            handles = new IntPtr[] { eventHandle, e2.SafeWaitHandle.DangerousGetHandle() };
            Assert.Equal(WaitHandle.WaitTimeout, tsc.Wait(handles, false, 0));
            Assert.Equal(WaitHandle.WaitTimeout, tsc.Wait(handles, true, 0));

            e.Set();
            Assert.Equal(0, tsc.Wait(handles, false, 0));
            Assert.Equal(WaitHandle.WaitTimeout, tsc.Wait(handles, true, 0));

            e2.Set();
            Assert.Equal(0, tsc.Wait(handles, false, 0));
            Assert.Equal(0, tsc.Wait(handles, true, 0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // desktop framework does not check for null and crashes
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono)]
        public static void WaitTest_ChangedInDotNetCore()
        {
            Assert.Throws<ArgumentNullException>(() => TestSynchronizationContext.WaitHelper(null, false, 0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono, "https://bugzilla.xamarin.com/show_bug.cgi?id=60568")]
        public static void WaitNotificationTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var tsc = new TestSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(tsc);
                Assert.Same(tsc, SynchronizationContext.Current);

                var e = new ManualResetEvent(false);
                tsc.WaitAction = () => e.Set();
                Assert.False(tsc.IsWaitNotificationRequired());
                Assert.False(e.WaitOne(0));
                tsc.SetWaitNotificationRequired();
                Assert.True(tsc.IsWaitNotificationRequired());
                Assert.True(e.WaitOne(0));

                var mres = new ManualResetEventSlim();
                tsc.WaitAction = () => mres.Set();
                mres.Reset();
                mres.CheckedWait();

                e.Reset();
                tsc.WaitAction = () => e.Set();
                SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());
                Assert.False(e.WaitOne(0));
                SynchronizationContext.SetSynchronizationContext(tsc);
                Assert.True(e.WaitOne(0));
                e.Reset();
                e.CheckedWait();

                e.Reset();
                var lockObj = new object();
                var lockAcquiredFromBackground = new AutoResetEvent(false);
                Action waitForThread;
                Thread t =
                    ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                    {
                        lock (lockObj)
                        {
                            lockAcquiredFromBackground.Set();
                            e.CheckedWait();
                        }
                    });
                t.IsBackground = true;
                t.Start();
                lockAcquiredFromBackground.CheckedWait();
                Assert.True(Monitor.TryEnter(lockObj, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                Monitor.Exit(lockObj);
                waitForThread();

                e.Reset();
                var m = new Mutex();
                t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    m.CheckedWait();
                    try
                    {
                        lockAcquiredFromBackground.Set();
                        e.CheckedWait();
                    }
                    finally
                    {
                        m.ReleaseMutex();
                    }
                });
                t.IsBackground = true;
                t.Start();
                lockAcquiredFromBackground.CheckedWait();
                m.CheckedWait();
                m.ReleaseMutex();
                waitForThread();
            });
        }

        private class TestSynchronizationContext : SynchronizationContext
        {
            public Action WaitAction { get; set; }

            public new void SetWaitNotificationRequired()
            {
                base.SetWaitNotificationRequired();
            }

            public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
            {
                if (WaitAction != null)
                {
                    WaitAction();
                }
                return base.Wait(waitHandles, waitAll, millisecondsTimeout);
            }

            public static new int WaitHelper(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
            {
                return SynchronizationContext.WaitHelper(waitHandles, waitAll, millisecondsTimeout);
            }
        }
    }
}
