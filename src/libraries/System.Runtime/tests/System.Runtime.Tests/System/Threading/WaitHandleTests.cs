// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Threading.Tests
{
    public static partial class WaitHandleTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        [InlineData(Timeout.Infinite)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(20)]
        public static void SingleThreadedUnsatisfiedWaits(int millisecondsTimeout)
        {
            using AutoResetEvent first = new(true);
            using ManualResetEvent second = new(false);
            using Semaphore signal = new(0, 1);
            using Mutex mutex = new(initiallyOwned: true);
            WaitHandle[] handles = [first, second];

            AssertUnsatisfiedWait(() => second.WaitOne(millisecondsTimeout));
            AssertUnsatisfiedWait(() => WaitHandle.WaitAll(handles, millisecondsTimeout));
            Assert.True(first.WaitOne(0));
            AssertUnsatisfiedWait(() => WaitHandle.WaitAll([mutex, second], millisecondsTimeout));
            mutex.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => mutex.ReleaseMutex());
            AssertUnsatisfiedWait(() => WaitHandle.WaitAny(handles, millisecondsTimeout) != WaitHandle.WaitTimeout);
            AssertUnsatisfiedWait(() => WaitHandle.SignalAndWait(signal, second, millisecondsTimeout, exitContext: false));
            Assert.True(signal.WaitOne(0));
            Assert.False(signal.WaitOne(0));

            first.Set();
            second.Set();
            Assert.True(WaitHandle.WaitAll(handles, millisecondsTimeout));
            Assert.True(second.WaitOne(millisecondsTimeout));
            Assert.Equal(1, WaitHandle.WaitAny(handles, millisecondsTimeout));

            void AssertUnsatisfiedWait(Func<bool> wait)
            {
                if (millisecondsTimeout == Timeout.Infinite)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => wait());
                }
                else
                {
                    Assert.False(wait());
                }
            }
        }

        [Fact]
        public static void RecursiveMutexWaitAllRemainsBalanced()
        {
            using Mutex mutex = new();
            using AutoResetEvent signal = new(true);
            Assert.True(mutex.WaitOne(0));
            Assert.True(WaitHandle.WaitAll([mutex, signal], 0));
            mutex.ReleaseMutex();
            mutex.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => mutex.ReleaseMutex());
            Assert.True(mutex.WaitOne(0));
            mutex.ReleaseMutex();
        }

        [Fact]
        public static void WaitAllDoesNotConsumePartialSignals()
        {
            using AutoResetEvent first = new(true);
            using Semaphore second = new(0, 1);
            WaitHandle[] handles = [first, second];
            Assert.False(WaitHandle.WaitAll(handles, 0));
            Assert.True(first.WaitOne(0));
            Assert.False(first.WaitOne(0));
            first.Set();
            second.Release();
            Assert.True(WaitHandle.WaitAll(handles, 0));
            Assert.False(first.WaitOne(0));
            Assert.False(second.WaitOne(0));
        }

        [Fact]
        public static void WaitAnyConsumesTheLowestSignaledIndex()
        {
            using AutoResetEvent first = new(true);
            using AutoResetEvent second = new(true);
            WaitHandle[] handles = [first, second];
            Assert.Equal(0, WaitHandle.WaitAny(handles, 0));
            Assert.Equal(1, WaitHandle.WaitAny(handles, 0));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, 0));
        }

        [Fact]
        public static void SignalAndWaitRetainsTheSignalOnTimeout()
        {
            using Semaphore signal = new(0, 1);
            using ManualResetEvent target = new(true);
            Assert.True(WaitHandle.SignalAndWait(signal, target, 0, exitContext: false));
            Assert.True(signal.WaitOne(0));
            target.Reset();
            Assert.False(WaitHandle.SignalAndWait(signal, target, 0, exitContext: false));
            Assert.True(signal.WaitOne(0));
            Assert.False(signal.WaitOne(0));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void SingleThreadedPendingInterruptPrecedesSignaling(int operation)
        {
            using AutoResetEvent target = new(true);
            using AutoResetEvent signal = new(false);
            Thread.CurrentThread.Interrupt();
            Assert.Throws<ThreadInterruptedException>(() =>
            {
                switch (operation)
                {
                    case 0: target.WaitOne(0); break;
                    case 1: WaitHandle.WaitAny([target, signal], 0); break;
                    case 2: WaitHandle.WaitAll([target, signal], 0); break;
                    case 3: WaitHandle.SignalAndWait(signal, target, 0, exitContext: false); break;
                }
            });
            Assert.True(target.WaitOne(0));
            Assert.False(signal.WaitOne(0));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void SingleThreadedMultiWaitValidatesEveryHandleBeforeInterrupting(bool waitAll)
        {
            using AutoResetEvent signal = new(true);
            using TestWaitHandle invalid = new() { SafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, ownsHandle: false) };
            WaitHandle[] handles = [signal, invalid];

            Thread.CurrentThread.Interrupt();
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (waitAll)
                {
                    WaitHandle.WaitAll(handles, 0);
                }
                else
                {
                    WaitHandle.WaitAny(handles, 0);
                }
            });
            Assert.Throws<ThreadInterruptedException>(() => signal.WaitOne(0));
            Assert.True(signal.WaitOne(0));
            Assert.False(signal.WaitOne(0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public static void SingleThreadedDuplicateWaitAllPreservesSignalAndInterrupt()
        {
            using AutoResetEvent signal = new(true);
            Thread.CurrentThread.Interrupt();
            Assert.Throws<DuplicateWaitObjectException>(() => WaitHandle.WaitAll([signal, signal], 0));
            Assert.Throws<ThreadInterruptedException>(() => signal.WaitOne(0));
            Assert.True(signal.WaitOne(0));
            Assert.False(signal.WaitOne(0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void SignalAndWaitOnTheSameObjectConsumesTheSignal(bool useSemaphore)
        {
            using WaitHandle signal = useSemaphore ? new Semaphore(0, 1) : new AutoResetEvent(false);
            Assert.True(WaitHandle.SignalAndWait(signal, signal, 0, exitContext: false));
            Assert.False(signal.WaitOne(0));
            Assert.True(WaitHandle.SignalAndWait(signal, signal, 0, exitContext: false));
            Assert.False(signal.WaitOne(0));
        }

        [Fact]
        public static void WaitOne()
        {
            ManualResetEvent h = new ManualResetEvent(true);

            Assert.True(h.WaitOne());
            Assert.True(h.WaitOne(1));
            Assert.True(h.WaitOne(TimeSpan.FromMilliseconds(1)));

            h.Reset();

            Assert.False(h.WaitOne(1));
            Assert.False(h.WaitOne(TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAny()
        {
            var handles = new ManualResetEvent[] {
            new ManualResetEvent(false),
            new ManualResetEvent(false),
            new ManualResetEvent(true)
        };

            Assert.Equal(2, WaitHandle.WaitAny(handles));
            Assert.Equal(2, WaitHandle.WaitAny(handles, 1));
            Assert.Equal(2, WaitHandle.WaitAny(handles, TimeSpan.FromMilliseconds(1)));

            handles[2].Reset();

            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, 1));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(handles, TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAny_NullArray_Throws()
        {
            WaitHandle[] handles = null;

            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAny(handles));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAny(handles, 0));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAny(handles, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAny(handles, 0, exitContext: false));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAny(handles, TimeSpan.Zero, exitContext: false));
        }

        [Fact]
        public static void WaitAny_NullHandle_Throws()
        {
            var handles = new WaitHandle[] { new ManualResetEvent(true), null, new AutoResetEvent(true) };

            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAny(handles));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAny(handles, 0));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAny(handles, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAny(handles, 0, exitContext: false));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAny(handles, TimeSpan.Zero, exitContext: false));
        }

        [Fact]
        public static void WaitAny_SameHandles()
        {
            ManualResetEvent[] wh = new ManualResetEvent[2];
            wh[0] = new ManualResetEvent(true);
            wh[1] = wh[0];

            Assert.Equal(0, WaitHandle.WaitAny(wh));
        }

        static ManualResetEvent[] CreateManualResetEvents(int length)
        {
            var handles = new ManualResetEvent[length];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = new ManualResetEvent(true);
            return handles;
        }

        [Fact]
        public static void WaitAny_MaxHandles()
        {
            Assert.Equal(0, WaitHandle.WaitAny(CreateManualResetEvents(64)));
            Assert.Throws<NotSupportedException>(() => WaitHandle.WaitAny(CreateManualResetEvents(65)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34366", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WaitAny_MaxHandles_STA()
        {
            Thread t = new Thread(() =>
            {
                Assert.Equal(0, WaitHandle.WaitAny(CreateManualResetEvents(63)));
                Assert.Throws<NotSupportedException>(() => WaitHandle.WaitAny(CreateManualResetEvents(64)));
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public static void WaitAll()
        {
            ManualResetEvent[] handles = CreateManualResetEvents(3);

            Assert.True(WaitHandle.WaitAll(handles));
            Assert.True(WaitHandle.WaitAll(handles, 1));
            Assert.True(WaitHandle.WaitAll(handles, TimeSpan.FromMilliseconds(1)));

            handles[2].Reset();

            Assert.False(WaitHandle.WaitAll(handles, 1));
            Assert.False(WaitHandle.WaitAll(handles, TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public static void WaitAll_NullArray_Throws()
        {
            WaitHandle[] handles = null;

            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAll(handles));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAll(handles, 0));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAll(handles, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAll(handles, 0, exitContext: false));
            Assert.Throws<ArgumentNullException>("waitHandles", () => WaitHandle.WaitAll(handles, TimeSpan.Zero, exitContext: false));
        }

        [Fact]
        public static void WaitAll_NullHandle_Throws()
        {
            var handles = new WaitHandle[] { new ManualResetEvent(true), null, new AutoResetEvent(true) };

            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAll(handles));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAll(handles, 0));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAll(handles, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAll(handles, 0, exitContext: false));
            Assert.Throws<ArgumentNullException>("waitHandles[1]", () => WaitHandle.WaitAll(handles, TimeSpan.Zero, exitContext: false));
        }

        [Fact]
        public static void WaitAll_SameHandles()
        {
            ManualResetEvent[] wh = new ManualResetEvent[2];
            wh[0] = new ManualResetEvent(true);
            wh[1] = wh[0];

            Assert.ThrowsAny<ArgumentException>(() => WaitHandle.WaitAll(wh));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34366", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public static void WaitAll_SameNames()
        {
            Mutex[] wh = new Mutex[2];
            wh[0] = new Mutex(false, "test");
            wh[1] = new Mutex(false, "test");

            AssertExtensions.Throws<ArgumentException>(null, () => WaitHandle.WaitAll(wh));
        }

        [Fact]
        public static void WaitTimeout()
        {
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(new[] { new ManualResetEvent(false) }, 0));
        }

        [Fact]
        public static void Dispose()
        {
            var wh = new ManualResetEvent(false);
            wh.Dispose();
            Assert.Throws<ObjectDisposedException>(() => wh.WaitOne(0));
        }

        [Fact]
        public static void DisposeVirtual_ThroughDispose()
        {
            var wh = new TestWaitHandle();
            wh.Dispose();
            Assert.True(wh.WasExplicitlyDisposed);
        }

        private partial class TestWaitHandle : WaitHandle
        {
            public bool WasExplicitlyDisposed { get; private set; } = false;

            protected override void Dispose(bool explicitDisposing)
            {
                if (explicitDisposing)
                {
                    WasExplicitlyDisposed = true;
                }
                base.Dispose(explicitDisposing);
            }
        }

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

        [Fact]
        public static void SafeWaitHandleViaExtension()
        {
            ManualResetEvent eventWaitHandle = new ManualResetEvent(false);
            SafeWaitHandle eventSafeWaitHandle = eventWaitHandle.GetSafeWaitHandle();
            TestWaitHandle testWaitHandle = new TestWaitHandle();
            testWaitHandle.SetSafeWaitHandle(eventSafeWaitHandle);
            Assert.False(testWaitHandle.WaitOne(0));
            eventWaitHandle.Set();
            Assert.True(testWaitHandle.WaitOne(0));
            testWaitHandle.SetSafeWaitHandle(null);
            Assert.Throws<ObjectDisposedException>(() => testWaitHandle.WaitOne(0));
        }

        [Fact]
        public static void SetSafeWaitHandleOnNull() =>
            AssertExtensions.Throws<ArgumentNullException>("waitHandle", () => default(WaitHandle).SetSafeWaitHandle(null));

        [Fact]
        public static void GetSafeWaitHandleOnNull() =>
            AssertExtensions.Throws<ArgumentNullException>("waitHandle", () => default(WaitHandle).GetSafeWaitHandle());

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
                Assert.Throws<ApplicationException>(() => mutexWh.ReleaseMutex());
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34366", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(SignalAndWait_MemberData))]
        public static void SignalAndWait(
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
                Assert.Throws<ApplicationException>(() => callSignalAndWait(toSignal, toWaitOn));
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
