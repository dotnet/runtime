// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Threading.Tests
{
    public static partial class WaitHandleTests
    {
#pragma warning disable 0618 // 'WaitHandle.Handle' is obsolete: 'Use the SafeWaitHandle property instead.'
        [Fact]
        public static void Handle()
        {
            var eventWaitHandle = new ManualResetEvent(false);
            var eventRawWaitHandle = eventWaitHandle.Handle;
            var testWaitHandle = new TestWaitHandle();
            testWaitHandle.Handle = eventRawWaitHandle;
            Assert.False(testWaitHandle.WaitOne(0));
            eventWaitHandle.Set();
            Assert.True(testWaitHandle.WaitOne(0));
            testWaitHandle.ClearHandle();
            Assert.Throws<ObjectDisposedException>(() => testWaitHandle.WaitOne(0));
        }
#pragma warning restore 0618 // 'WaitHandle.Handle' is obsolete: 'Use the SafeWaitHandle property instead.'

        [Fact]
        public static void SafeWaitHandle()
        {
            var eventWaitHandle = new ManualResetEvent(false);
            var eventSafeWaitHandle = eventWaitHandle.SafeWaitHandle;
            var testWaitHandle = new TestWaitHandle();
            testWaitHandle.SafeWaitHandle = eventSafeWaitHandle;
            Assert.False(testWaitHandle.WaitOne(0));
            eventWaitHandle.Set();
            Assert.True(testWaitHandle.WaitOne(0));
            testWaitHandle.SafeWaitHandle = null;
            Assert.Throws<ObjectDisposedException>(() => testWaitHandle.WaitOne(0));
        }

        private static void Unsignal(WaitHandle wh)
        {
            var eventWh = wh as ManualResetEvent;
            var mutexWh = wh as Mutex;
            var semaphoreWh = wh as Semaphore;

            if (eventWh != null)
            {
                eventWh.Reset();
            }
            else
            {
                Assert.True(mutexWh != null || semaphoreWh != null);
                Assert.True(wh.WaitOne(0));
            }
        }

        private static void VerifySignaled(WaitHandle wh)
        {
            var eventWh = wh as ManualResetEvent;
            var mutexWh = wh as Mutex;
            var semaphoreWh = wh as Semaphore;

            if (eventWh != null)
            {
                Assert.True(eventWh.WaitOne(0));
            }
            else if (mutexWh != null)
            {
                Assert.Throws<Exception>(() => mutexWh.ReleaseMutex());
            }
            else
            {
                Assert.NotNull(semaphoreWh);
                Assert.Throws<SemaphoreFullException>(() => semaphoreWh.Release());
            }
        }

        public static IEnumerable<object[]> SignalAndWait_MemberData()
        {
            var toSignal = new WaitHandle[] { new ManualResetEvent(false), new Mutex(), new Semaphore(1, 1) };
            var toWaitOn = new AutoResetEvent(false);
            var callSignalAndWait =
                new Func<WaitHandle, WaitHandle, bool>[]
                {
                    (s, w) => WaitHandle.SignalAndWait(s, w),
                    (s, w) => WaitHandle.SignalAndWait(s, w, 0, false),
                    (s, w) => WaitHandle.SignalAndWait(s, w, TimeSpan.Zero, false),
                };

            for (int signalIndex = 0; signalIndex < toSignal.Length; ++signalIndex)
            {
                for (int callIndex = 0; callIndex < callSignalAndWait.Length; ++callIndex)
                {
                    var skipInfiniteWaitTests = callIndex == 0;
                    yield return
                        new object[]
                        {
                            toSignal[signalIndex],
                            toWaitOn,
                            callSignalAndWait[callIndex],
                            skipInfiniteWaitTests
                        };
                }
            }
        }

        [Theory]
        [MemberData(nameof(SignalAndWait_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)] // other platforms don't support SignalAndWait
        private static void SignalAndWait(
            WaitHandle toSignal,
            AutoResetEvent toWaitOn,
            Func<WaitHandle, WaitHandle, bool> callSignalAndWait,
            bool skipInfiniteWaitTests)
        {
            // Verify that signaling is done, and the wait succeeds
            Unsignal(toSignal);
            toWaitOn.Set();
            Assert.True(callSignalAndWait(toSignal, toWaitOn));
            VerifySignaled(toSignal);
            Assert.False(toWaitOn.WaitOne(0));

            if (!skipInfiniteWaitTests)
            {
                // Verify that signaling is done even when the wait fails
                Unsignal(toSignal);
                toWaitOn.Reset();
                Assert.False(callSignalAndWait(toSignal, toWaitOn));
                VerifySignaled(toSignal);
                Assert.False(toWaitOn.WaitOne(0));
            }

            // Verify that signaling an already signaled object yields the appropriate behavior
            toWaitOn.Set();
            if (toSignal is ManualResetEvent)
            {
                Assert.True(callSignalAndWait(toSignal, toWaitOn));
                Assert.False(toWaitOn.WaitOne(0));
            }
            else if (toSignal is Mutex)
            {
                Assert.Throws<Exception>(() => callSignalAndWait(toSignal, toWaitOn));
                Assert.True(toWaitOn.WaitOne(0));
            }
            else
            {
                Assert.True(toSignal is Semaphore);
                Assert.Throws<InvalidOperationException>(() => callSignalAndWait(toSignal, toWaitOn));
                Assert.True(toWaitOn.WaitOne(0));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // other platforms don't support SignalAndWait
        public static void SignalAndWait_InvalidArgs()
        {
            var toSignal = new ManualResetEvent(false);
            var toWaitOn = new ManualResetEvent(true);

            Assert.Throws<ArgumentNullException>(() => WaitHandle.SignalAndWait(null, toWaitOn));
            Assert.False(toSignal.WaitOne(0));
            Assert.Throws<ArgumentNullException>(() => WaitHandle.SignalAndWait(toSignal, null));
            Assert.False(toSignal.WaitOne(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => WaitHandle.SignalAndWait(toSignal, toWaitOn, -2, false));
            Assert.False(toSignal.WaitOne(0));
            Assert.True(WaitHandle.SignalAndWait(toSignal, toWaitOn, -1, false));
            Assert.True(toSignal.WaitOne(0));
            toSignal.Reset();

            var invalidWh = new TestWaitHandle();
            Assert.Throws<ObjectDisposedException>(() => WaitHandle.SignalAndWait(invalidWh, toWaitOn));
            Assert.False(toSignal.WaitOne(0));
            Assert.Throws<ObjectDisposedException>(() => WaitHandle.SignalAndWait(toSignal, invalidWh));
            Assert.False(toSignal.WaitOne(0));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void SignalAndWait_PlatformNotSupported()
        {
            var toSignal = new ManualResetEvent(false);
            var toWaitOn = new ManualResetEvent(true);
            Assert.Throws<PlatformNotSupportedException>(() => WaitHandle.SignalAndWait(toSignal, toWaitOn));
            Assert.Throws<PlatformNotSupportedException>(() => WaitHandle.SignalAndWait(toSignal, toWaitOn, 0, false));
            Assert.Throws<PlatformNotSupportedException>(() => WaitHandle.SignalAndWait(toSignal, toWaitOn, TimeSpan.Zero, false));
        }

        [Fact]
        public static void Close()
        {
            var wh = new ManualResetEvent(false);
            wh.Close();
            Assert.Throws<ObjectDisposedException>(() => wh.WaitOne(0));
        }

        [Fact]
        public static void DisposeVirtual_ThroughClose()
        {
            var wh = new TestWaitHandle();
            wh.Close();
            Assert.True(wh.WasExplicitlyDisposed);
        }

        private partial class TestWaitHandle : WaitHandle
        {
#pragma warning disable 0618 // 'WaitHandle.Handle' is obsolete: 'Use the SafeWaitHandle property instead.'
            public void ClearHandle()
            {
                Handle = InvalidHandle;
            }
#pragma warning restore 0618 // 'WaitHandle.Handle' is obsolete: 'Use the SafeWaitHandle property instead.'
        }
    }
}
