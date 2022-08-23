// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.ThreadPools.Tests
{
    public partial class RegisteredWaitTests
    {
        private const int UnexpectedTimeoutMilliseconds = ThreadTestHelpers.UnexpectedTimeoutMilliseconds;
        private const int ExpectedTimeoutMilliseconds = ThreadTestHelpers.ExpectedTimeoutMilliseconds;

        private sealed class InvalidWaitHandle : WaitHandle
        {
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

            // Validate a repeating waithandle with infinite timeout.
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
                    -1,      // Infinite
                    false);  // Execute once
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void SignalingRegisteredWaitHandleCallsCallback()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            bool timedOut = false;
            ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, timedOut2) =>
            {
                timedOut = timedOut2;
                waitCallbackInvoked.Set();
            }, null, UnexpectedTimeoutMilliseconds, true);

            waitEvent.Set();
            waitCallbackInvoked.CheckedWait();
            Assert.False(timedOut);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TimingOutRegisteredWaitHandleCallsCallback()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            bool timedOut = false;
            ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, timedOut2) =>
            {
                timedOut = timedOut2;
                waitCallbackInvoked.Set();
            }, null, ExpectedTimeoutMilliseconds, true);

            waitCallbackInvoked.CheckedWait();
            Assert.True(timedOut);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void UnregisteringWaitWithInvalidWaitHandleBeforeSignalingDoesNotCallCallback()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) =>
            {
                waitCallbackInvoked.Set();
            }, null, UnexpectedTimeoutMilliseconds, true);

            Assert.True(registeredWaitHandle.Unregister(new InvalidWaitHandle())); // blocking unregister
            waitEvent.Set();
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void UnregisteringWaitWithEventBeforeSignalingDoesNotCallCallback()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitUnregistered = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) =>
            {
                waitCallbackInvoked.Set();
            }, null, UnexpectedTimeoutMilliseconds, true);

            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            waitUnregistered.CheckedWait();
            waitEvent.Set();
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void NonrepeatingWaitFiresOnlyOnce()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            bool anyTimedOut = false;
            var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, timedOut) =>
            {
                anyTimedOut |= timedOut;
                waitCallbackInvoked.Set();
            }, null, UnexpectedTimeoutMilliseconds, true);

            waitEvent.Set();
            waitCallbackInvoked.CheckedWait();
            waitEvent.Set();
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
            Assert.False(anyTimedOut);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RepeatingWaitFiresUntilUnregistered()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            bool anyTimedOut = false;
            var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, timedOut) =>
            {
                anyTimedOut |= timedOut;
                waitCallbackInvoked.Set();
            }, null, UnexpectedTimeoutMilliseconds, false);

            for (int i = 0; i < 4; ++i)
            {
                waitEvent.Set();
                waitCallbackInvoked.CheckedWait();
            }

            Assert.True(registeredWaitHandle.Unregister(new InvalidWaitHandle())); // blocking unregister
            waitEvent.Set();
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
            Assert.False(anyTimedOut);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void UnregisterEventSignaledWhenUnregistered()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackInvoked = new AutoResetEvent(false);
            var waitUnregistered = new AutoResetEvent(false);
            bool timedOut = false;
            WaitOrTimerCallback waitCallback = (_, timedOut2) =>
            {
                timedOut = timedOut2;
                waitCallbackInvoked.Set();
            };

            // executeOnlyOnce = true, no timeout and no callback invocation
            var registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(waitEvent, waitCallback, null, Timeout.Infinite, executeOnlyOnce: true);
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            waitUnregistered.CheckedWait();
            Assert.False(timedOut);

            // executeOnlyOnce = true, no timeout with callback invocation
            registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(waitEvent, waitCallback, null, Timeout.Infinite, executeOnlyOnce: true);
            waitEvent.Set();
            waitCallbackInvoked.CheckedWait();
            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            waitUnregistered.CheckedWait();
            Assert.False(timedOut);

            // executeOnlyOnce = true, with timeout
            registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(
                    waitEvent, waitCallback, null, ExpectedTimeoutMilliseconds, executeOnlyOnce: true);
            waitCallbackInvoked.CheckedWait();
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            waitUnregistered.CheckedWait();
            Assert.True(timedOut);
            timedOut = false;

            // executeOnlyOnce = false
            registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(
                    waitEvent, waitCallback, null, UnexpectedTimeoutMilliseconds, executeOnlyOnce: false);
            Assert.False(waitCallbackInvoked.WaitOne(ExpectedTimeoutMilliseconds));
            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            waitUnregistered.CheckedWait();
            Assert.False(timedOut);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CanRegisterMoreThan64Waits()
        {
            RegisteredWaitHandle[] registeredWaitHandles = new RegisteredWaitHandle[65];
            WaitOrTimerCallback waitCallback = (_, __) => { };
            for (int i = 0; i < registeredWaitHandles.Length; ++i)
            {
                registeredWaitHandles[i] =
                    ThreadPool.RegisterWaitForSingleObject(
                        new AutoResetEvent(false), waitCallback, null, UnexpectedTimeoutMilliseconds, true);
            }
            for (int i = 0; i < registeredWaitHandles.Length; ++i)
            {
                Assert.True(registeredWaitHandles[i].Unregister(null));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void StateIsPassedThroughToCallback()
        {
            object state = new object();
            var waitCallbackInvoked = new AutoResetEvent(false);
            object statePassedToCallback = null;
            ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(true), (callbackState, _) =>
            {
                statePassedToCallback = callbackState;
                waitCallbackInvoked.Set();
            }, state, 0, true);

            waitCallbackInvoked.CheckedWait();
            Assert.Same(state, statePassedToCallback);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void UnregisterWaitHandleIsNotSignaledWhenCallbackIsRunning()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackProgressMade = new AutoResetEvent(false);
            var completeWaitCallback = new AutoResetEvent(false);
            var waitUnregistered = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) =>
            {
                waitCallbackProgressMade.Set();
                completeWaitCallback.WaitOne(UnexpectedTimeoutMilliseconds);
                waitCallbackProgressMade.Set();
            }, null, UnexpectedTimeoutMilliseconds, false);

            waitEvent.Set();
            waitCallbackProgressMade.CheckedWait(); // one callback running
            waitEvent.Set();
            waitCallbackProgressMade.CheckedWait(); // two callbacks running
            Assert.True(registeredWaitHandle.Unregister(waitUnregistered));
            Assert.False(waitUnregistered.WaitOne(ExpectedTimeoutMilliseconds));
            completeWaitCallback.Set(); // complete one callback
            waitCallbackProgressMade.CheckedWait();
            Assert.False(waitUnregistered.WaitOne(ExpectedTimeoutMilliseconds));
            completeWaitCallback.Set(); // complete other callback
            waitCallbackProgressMade.CheckedWait();
            waitUnregistered.CheckedWait();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void BlockingUnregisterBlocksWhileCallbackIsRunning()
        {
            var waitEvent = new AutoResetEvent(false);
            var waitCallbackProgressMade = new AutoResetEvent(false);
            var completeWaitCallback = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) =>
            {
                waitCallbackProgressMade.Set();
                completeWaitCallback.WaitOne(UnexpectedTimeoutMilliseconds);
                waitCallbackProgressMade.Set();
            }, null, UnexpectedTimeoutMilliseconds, false);

            waitEvent.Set();
            waitCallbackProgressMade.CheckedWait(); // one callback running
            waitEvent.Set();
            waitCallbackProgressMade.CheckedWait(); // two callbacks running

            Thread t = ThreadTestHelpers.CreateGuardedThread(out Action waitForThread, () =>
                Assert.True(registeredWaitHandle.Unregister(new InvalidWaitHandle())));
            t.IsBackground = true;
            t.Start();

            Assert.False(t.Join(ExpectedTimeoutMilliseconds));
            completeWaitCallback.Set(); // complete one callback
            waitCallbackProgressMade.CheckedWait();
            Assert.False(t.Join(ExpectedTimeoutMilliseconds));
            completeWaitCallback.Set(); // complete other callback
            waitCallbackProgressMade.CheckedWait();
            waitForThread();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CallingUnregisterOnAutomaticallyUnregisteredHandleReturnsTrue()
        {
            var waitCallbackInvoked = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(
                    new AutoResetEvent(true),
                    (_, __) => waitCallbackInvoked.Set(),
                    null,
                    UnexpectedTimeoutMilliseconds,
                    true);
            waitCallbackInvoked.CheckedWait();
            Thread.Sleep(ExpectedTimeoutMilliseconds); // wait for callback to exit
            Assert.True(registeredWaitHandle.Unregister(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void EventSetAfterUnregisterNotObservedOnWaitThread()
        {
            var waitEvent = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) => { }, null, UnexpectedTimeoutMilliseconds, true);
            Assert.True(registeredWaitHandle.Unregister(null));
            waitEvent.Set();
            Thread.Sleep(ExpectedTimeoutMilliseconds); // give wait thread a chance to observe the signal
            waitEvent.CheckedWait(); // signal should not have been observed by wait thread
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CanDisposeEventAfterNonblockingUnregister()
        {
            using (var waitEvent = new AutoResetEvent(false))
            {
                RegisteredWaitHandle registeredWaitHandle =
                    ThreadPool.RegisterWaitForSingleObject(waitEvent, (_, __) => { }, null, UnexpectedTimeoutMilliseconds, true);
                Assert.True(registeredWaitHandle.Unregister(null));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void MultipleRegisteredWaitsUnregisterHandleShiftTest()
        {
            var handlePendingRemoval = new AutoResetEvent(false);
            var completeWaitCallback = new AutoResetEvent(false);
            WaitOrTimerCallback waitCallback = (_, __) =>
            {
                handlePendingRemoval.Set();
                completeWaitCallback.CheckedWait();
            };

            var waitEvent = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle =
                ThreadPool.RegisterWaitForSingleObject(waitEvent, waitCallback, null, UnexpectedTimeoutMilliseconds, true);

            var waitEvent2 = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle2 =
                ThreadPool.RegisterWaitForSingleObject(waitEvent2, waitCallback, null, UnexpectedTimeoutMilliseconds, true);

            var waitEvent3 = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle3 =
                ThreadPool.RegisterWaitForSingleObject(waitEvent3, waitCallback, null, UnexpectedTimeoutMilliseconds, true);

            void SetAndUnregister(AutoResetEvent waitEvent, RegisteredWaitHandle registeredWaitHandle)
            {
                waitEvent.Set();
                handlePendingRemoval.CheckedWait();
                Thread.Sleep(ExpectedTimeoutMilliseconds); // wait for removal
                Assert.True(registeredWaitHandle.Unregister(null));
                completeWaitCallback.Set();
                waitEvent.Dispose();
            }

            SetAndUnregister(waitEvent, registeredWaitHandle);
            SetAndUnregister(waitEvent2, registeredWaitHandle2);

            var waitEvent4 = new AutoResetEvent(false);
            RegisteredWaitHandle registeredWaitHandle4 =
                ThreadPool.RegisterWaitForSingleObject(waitEvent4, waitCallback, null, UnexpectedTimeoutMilliseconds, true);

            SetAndUnregister(waitEvent3, registeredWaitHandle3);
            SetAndUnregister(waitEvent4, registeredWaitHandle4);
        }
    }
}
