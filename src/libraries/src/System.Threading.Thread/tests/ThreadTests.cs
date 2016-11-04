// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using Xunit;

namespace System.Threading.Threads.Tests
{
    public static class ThreadTests
    {
        private const int ExpectedTimeoutMilliseconds = 500;
        private const int UnexpectedTimeoutMilliseconds = 30000;

        private static Thread CreateGuardedThread(out Action waitForThread, Action start)
        {
            Exception backgroundEx = null;
            var t =
                new Thread(() =>
                {
                    try
                    {
                        start();
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                    }
                });
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
                    if (backgroundEx != null)
                    {
                        throw new TargetInvocationException(backgroundEx);
                    }
                };
            return t;
        }

        private static Thread CreateGuardedThread(out Action waitForThread, Action<object> start)
        {
            Exception backgroundEx = null;
            var t =
                new Thread(parameter =>
                {
                    try
                    {
                        start(parameter);
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                    }
                });
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
                    if (backgroundEx != null)
                    {
                        throw new TargetInvocationException(backgroundEx);
                    }
                };
            return t;
        }

        private static void RunTestInBackgroundThread(Action test)
        {
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, test);
            t.IsBackground = true;
            t.Start();
            waitForThread();
        }

        private static void CheckedWait(this WaitHandle wh)
        {
            Assert.True(wh.WaitOne(UnexpectedTimeoutMilliseconds));
        }

        private static void WaitForCondition(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Sleep(1));
        }

        private static void WaitForConditionWithoutBlocking(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Yield());
        }

        private static void WaitForConditionWithCustomDelay(Func<bool> condition, Action delay)
        {
            var startTime = DateTime.Now;
            while (!condition())
            {
                Assert.True((DateTime.Now - startTime).TotalMilliseconds < UnexpectedTimeoutMilliseconds);
                delay();
            }
        }

        [Fact]
        public static void ConstructorTest()
        {
            var t = new Thread(() => { });
            t = new Thread(() => { }, 0);
            t = new Thread(state => { });
            t = new Thread(state => { }, 0);

            Assert.Throws<ArgumentNullException>(() => new Thread((ThreadStart)null));
            Assert.Throws<ArgumentNullException>(() => new Thread((ThreadStart)null, 0));
            Assert.Throws<ArgumentNullException>(() => new Thread((ParameterizedThreadStart)null));
            Assert.Throws<ArgumentNullException>(() => new Thread((ParameterizedThreadStart)null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => new Thread(() => { }, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Thread(state => { }, -1));
        }

        private static IEnumerable<object[]> ApartmentStateTest_MemberData()
        {
            yield return
                new object[]
                {
#pragma warning disable 618 // Obsolete members
                    new Func<Thread, ApartmentState>(t => t.ApartmentState),
#pragma warning restore 618 // Obsolete members
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
#pragma warning disable 618 // Obsolete members
                                t.ApartmentState = value;
#pragma warning restore 618 // Obsolete members
                                return 0;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    0
                };
            yield return
                new object[]
                {
                    new Func<Thread, ApartmentState>(t => t.GetApartmentState()),
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
                                t.SetApartmentState(value);
                                return 0;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (InvalidOperationException)
                            {
                                return 2;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    1
                };
            yield return
                new object[]
                {
                    new Func<Thread, ApartmentState>(t => t.GetApartmentState()),
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
                                return t.TrySetApartmentState(value) ? 0 : 2;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    2
                };
        }

        [Theory]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetSetApartmentStateTest_ChangeAfterThreadStarted_Windows(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            RunTestInBackgroundThread(() =>
            {
                var t = Thread.CurrentThread;
                Assert.Equal(1, setApartmentState(t, ApartmentState.STA - 1));
                Assert.Equal(1, setApartmentState(t, ApartmentState.Unknown + 1));

                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
                Assert.Equal(0, setApartmentState(t, ApartmentState.MTA));
                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
                Assert.Equal(setType == 0 ? 0 : 2, setApartmentState(t, ApartmentState.STA)); // cannot be changed after thread is started
                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
            });
        }

        [Theory]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void ApartmentStateTest_ChangeBeforeThreadStarted_Windows(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            ApartmentState apartmentStateInThread = ApartmentState.Unknown;
            Thread t = null;
            t = new Thread(() => apartmentStateInThread = getApartmentState(t));
            t.IsBackground = true;
            Assert.Equal(0, setApartmentState(t, ApartmentState.STA));
            Assert.Equal(ApartmentState.STA, getApartmentState(t));
            Assert.Equal(setType == 0 ? 0 : 2, setApartmentState(t, ApartmentState.MTA)); // cannot be changed more than once
            Assert.Equal(ApartmentState.STA, getApartmentState(t));
            t.Start();
            t.Join(UnexpectedTimeoutMilliseconds);
            Assert.Equal(ApartmentState.STA, apartmentStateInThread);
        }

        [Theory]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void ApartmentStateTest_Unix(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            var t = new Thread(() => { });
            Assert.Equal(ApartmentState.Unknown, getApartmentState(t));
            Assert.Equal(0, setApartmentState(t, ApartmentState.Unknown));

            int expectedFailure;
            switch (setType)
            {
                case 0:
                    expectedFailure = 0; // ApartmentState setter - no exception, but value does not change
                    break;
                case 1:
                    expectedFailure = 3; // SetApartmentState - InvalidOperationException
                    break;
                default:
                    expectedFailure = 2; // TrySetApartmentState - returns false
                    break;
            }
            Assert.Equal(expectedFailure, setApartmentState(t, ApartmentState.STA));
            Assert.Equal(expectedFailure, setApartmentState(t, ApartmentState.MTA));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void CurrentCultureTest_SkipOnDesktopFramework()
        {
            // Cannot access culture properties on a thread object from a different thread
            var t = new Thread(() => { });
            Assert.Throws<InvalidOperationException>(() => t.CurrentCulture);
            Assert.Throws<InvalidOperationException>(() => t.CurrentUICulture);
        }

        [Fact]
        public static void CurrentCultureTest()
        {
            RunTestInBackgroundThread(() =>
            {
                var t = Thread.CurrentThread;
                var originalCulture = CultureInfo.CurrentCulture;
                var originalUICulture = CultureInfo.CurrentUICulture;
                var otherCulture = CultureInfo.InvariantCulture;

                // Culture properties return the same value as those on CultureInfo
                Assert.Equal(originalCulture, t.CurrentCulture);
                Assert.Equal(originalUICulture, t.CurrentUICulture);

                try
                {
                    // Changing culture properties on CultureInfo causes the values of properties on the current thread to change
                    CultureInfo.CurrentCulture = otherCulture;
                    CultureInfo.CurrentUICulture = otherCulture;
                    Assert.Equal(otherCulture, t.CurrentCulture);
                    Assert.Equal(otherCulture, t.CurrentUICulture);

                    // Changing culture properties on the current thread causes new values to be returned, and causes the values of
                    // properties on CultureInfo to change
                    t.CurrentCulture = originalCulture;
                    t.CurrentUICulture = originalUICulture;
                    Assert.Equal(originalCulture, t.CurrentCulture);
                    Assert.Equal(originalUICulture, t.CurrentUICulture);
                    Assert.Equal(originalCulture, CultureInfo.CurrentCulture);
                    Assert.Equal(originalUICulture, CultureInfo.CurrentUICulture);
                }
                finally
                {
                    CultureInfo.CurrentCulture = originalCulture;
                    CultureInfo.CurrentUICulture = originalUICulture;
                }

                Assert.Throws<ArgumentNullException>(() => t.CurrentCulture = null);
                Assert.Throws<ArgumentNullException>(() => t.CurrentUICulture = null);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void CurrentPrincipalTest_SkipOnDesktopFramework()
        {
            RunTestInBackgroundThread(() => Assert.Null(Thread.CurrentPrincipal));
        }

        [Fact]
        public static void CurrentPrincipalTest()
        {
            RunTestInBackgroundThread(() =>
            {
                var originalPrincipal = Thread.CurrentPrincipal;
                var otherPrincipal =
                    new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[] { string.Empty });

                Thread.CurrentPrincipal = otherPrincipal;
                Assert.Equal(otherPrincipal, Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = originalPrincipal;
                Assert.Equal(originalPrincipal, Thread.CurrentPrincipal);
            });
        }

        [Fact]
        public static void CurrentThreadTest()
        {
            Thread otherThread = null;
            var t = new Thread(() => otherThread = Thread.CurrentThread);
            t.IsBackground = true;
            t.Start();
            t.Join(UnexpectedTimeoutMilliseconds);

            Assert.Equal(t, otherThread);

            var mainThread = Thread.CurrentThread;
            Assert.NotNull(mainThread);
            Assert.NotEqual(mainThread, otherThread);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void ExecutionContextTest()
        {
            RunTestInBackgroundThread(() => Assert.Equal(ExecutionContext.Capture(), Thread.CurrentThread.ExecutionContext));
        }

        [Fact]
        public static void IsAliveTest()
        {
            var isAliveWhenRunning = false;
            Thread t = null;
            t = new Thread(() => isAliveWhenRunning = t.IsAlive);
            t.IsBackground = true;

            Assert.False(t.IsAlive);
            t.Start();
            t.Join(UnexpectedTimeoutMilliseconds);
            Assert.True(isAliveWhenRunning);
            Assert.False(t.IsAlive);
        }

        [Fact]
        public static void IsBackgroundTest()
        {
            var t = new Thread(() => { });
            Assert.False(t.IsBackground);
            t.IsBackground = true;
            Assert.True(t.IsBackground);
            t.Start();
            t.Join(UnexpectedTimeoutMilliseconds);

            // Cannot use this property after the thread is dead
            Assert.Throws<ThreadStateException>(() => t.IsBackground);
            Assert.Throws<ThreadStateException>(() => t.IsBackground = false);
            Assert.Throws<ThreadStateException>(() => t.IsBackground = true);

            // Verify that the test process can shut down gracefully with a hung background thread
            t = new Thread(() => Thread.Sleep(Timeout.Infinite));
            t.IsBackground = true;
            t.Start();
        }

        [Fact]
        public static void IsThreadPoolThreadTest()
        {
            var isThreadPoolThread = false;
            Thread t = null;
            t = new Thread(() => { isThreadPoolThread = t.IsThreadPoolThread; });
            t.IsBackground = true;
            Assert.False(t.IsThreadPoolThread);

            t.Start();
            t.Join(UnexpectedTimeoutMilliseconds);
            Assert.False(isThreadPoolThread);

            var e = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                    e.Set();
                });
            e.CheckedWait();
            Assert.True(isThreadPoolThread);
        }

        [Fact]
        public static void ManagedThreadIdTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;
            t.Start();
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, t.ManagedThreadId);
            e.Set();
            waitForThread();
        }

        [Fact]
        public static void NameTest()
        {
            var t = new Thread(() => { });
            Assert.Null(t.Name);
            t.Name = "a";
            Assert.Equal("a", t.Name);
            Assert.Throws<InvalidOperationException>(() => t.Name = "b");
            Assert.Equal("a", t.Name);
        }

        [Fact]
        public static void PriorityTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;
            Assert.Equal(ThreadPriority.Normal, t.Priority);
            t.Priority = ThreadPriority.AboveNormal;
            Assert.Equal(ThreadPriority.AboveNormal, t.Priority);
            t.Start();
            Assert.Equal(ThreadPriority.AboveNormal, t.Priority);
            t.Priority = ThreadPriority.Normal;
            Assert.Equal(ThreadPriority.Normal, t.Priority);
            e.Set();
            waitForThread();
        }

        [Fact]
        public static void ThreadStateTest()
        {
            var e0 = new ManualResetEvent(false);
            var e1 = new AutoResetEvent(false);
            Action waitForThread;
            var t =
                CreateGuardedThread(out waitForThread, () =>
                {
                    e0.CheckedWait();
                    WaitForConditionWithoutBlocking(() => e1.WaitOne(0));
                });
            Assert.Equal(ThreadState.Unstarted, t.ThreadState);
            t.IsBackground = true;
            Assert.Equal(ThreadState.Unstarted | ThreadState.Background, t.ThreadState);

            t.Start();
            WaitForCondition(() => t.ThreadState == (ThreadState.WaitSleepJoin | ThreadState.Background));

            e0.Set();
            WaitForCondition(() => t.ThreadState == (ThreadState.Running | ThreadState.Background));

            e1.Set();
            waitForThread();
            Assert.Equal(ThreadState.Stopped, t.ThreadState);

            t = CreateGuardedThread(out waitForThread, () => WaitForConditionWithoutBlocking(() => e1.WaitOne(0)));
            t.Start();
            WaitForCondition(() => t.ThreadState == ThreadState.Running);

            e1.Set();
            waitForThread();
            Assert.Equal(ThreadState.Stopped, t.ThreadState);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void AbortSuspendTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;

            Action verify = () =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => t.Abort());
                Assert.Throws<PlatformNotSupportedException>(() => t.Abort(t));
#pragma warning disable 618 // Obsolete members
                Assert.Throws<PlatformNotSupportedException>(() => t.Suspend());
                Assert.Throws<PlatformNotSupportedException>(() => t.Resume());
#pragma warning restore 618 // Obsolete members
            };
            verify();

            t.Start();
            verify();

            e.Set();
            waitForThread();

            Assert.Throws<PlatformNotSupportedException>(() => Thread.ResetAbort());
        }

        private static void VerifyLocalDataSlot(LocalDataStoreSlot slot)
        {
            Assert.NotNull(slot);

            var waitForThreadArray = new Action[2];
            var threadArray = new Thread[2];
            var barrier = new Barrier(threadArray.Length);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Func<bool> barrierSignalAndWait =
                () =>
                {
                    try
                    {
                        Assert.True(barrier.SignalAndWait(UnexpectedTimeoutMilliseconds, cancellationToken));
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    return true;
                };

            Action<int> threadMain =
                threadIndex =>
                {
                    try
                    {
                        Assert.Null(Thread.GetData(slot));
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex == 0)
                        {
                            Thread.SetData(slot, threadIndex);
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex == 0)
                        {
                            Assert.Equal(threadIndex, Thread.GetData(slot));
                        }
                        else
                        {
                            Assert.Null(Thread.GetData(slot));
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex != 0)
                        {
                            Thread.SetData(slot, threadIndex);
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        Assert.Equal(threadIndex, Thread.GetData(slot));
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        cancellationTokenSource.Cancel();
                        throw new TargetInvocationException(ex);
                    }
                };

            for (int i = 0; i < threadArray.Length; ++i)
            {
                threadArray[i] = CreateGuardedThread(out waitForThreadArray[i], () => threadMain(i));
                threadArray[i].IsBackground = true;
                threadArray[i].Start();
            }

            foreach (var waitForThread in waitForThreadArray)
            {
                waitForThread();
            }
        }

        [Fact]
        public static void LocalDataSlotTest()
        {
            var slot = Thread.AllocateDataSlot();
            var slot2 = Thread.AllocateDataSlot();
            Assert.NotEqual(slot, slot2);
            VerifyLocalDataSlot(slot);
            VerifyLocalDataSlot(slot2);

            var slotName = "System.Threading.Threads.Tests.LocalDataSlotTest";
            var slotName2 = slotName + ".2";
            var invalidSlotName = slotName + ".Invalid";

            try
            {
                // AllocateNamedDataSlot allocates
                slot = Thread.AllocateNamedDataSlot(slotName);
                Assert.Throws<ArgumentException>(() => Thread.AllocateNamedDataSlot(slotName));
                slot2 = Thread.AllocateNamedDataSlot(slotName2);
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);

                // Free the same slot twice, should be fine
                Thread.FreeNamedDataSlot(slotName2);
                Thread.FreeNamedDataSlot(slotName2);
            }
            catch (Exception ex)
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
                throw new TargetInvocationException(ex);
            }

            try
            {
                // GetNamedDataSlot gets or allocates
                var tempSlot = Thread.GetNamedDataSlot(slotName);
                Assert.Equal(slot, tempSlot);
                tempSlot = Thread.GetNamedDataSlot(slotName2);
                Assert.NotEqual(slot2, tempSlot);
                slot2 = tempSlot;
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);
            }
            finally
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
            }

            try
            {
                // A named slot can be used after the name is freed, since only the name is freed, not the slot
                slot = Thread.AllocateNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName);
                slot2 = Thread.AllocateNamedDataSlot(slotName); // same name
                Thread.FreeNamedDataSlot(slotName);
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);
            }
            finally
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
            }
        }

        [Fact]
        public static void InterruptTest()
        {
            // Interrupting a thread that is not blocked does not do anything, but once the thread starts blocking, it gets
            // interrupted
            var threadReady = new AutoResetEvent(false);
            var continueThread = new AutoResetEvent(false);
            bool continueThreadBool = false;
            Action waitForThread;
            var t =
                CreateGuardedThread(out waitForThread, () =>
                {
                    threadReady.Set();
                    WaitForConditionWithoutBlocking(() => Volatile.Read(ref continueThreadBool));
                    threadReady.Set();
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait());
                });
            t.IsBackground = true;
            t.Start();
            threadReady.CheckedWait();
            t.Interrupt();
            Assert.False(threadReady.WaitOne(ExpectedTimeoutMilliseconds));
            Volatile.Write(ref continueThreadBool, true);
            waitForThread();

            // Interrupting a dead thread does nothing
            t.Interrupt();

            // Interrupting an unstarted thread causes the thread to be interrupted after it is started and starts blocking
            t = CreateGuardedThread(out waitForThread, () =>
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait()));
            t.IsBackground = true;
            t.Interrupt();
            t.Start();
            waitForThread();

            // A thread that is already blocked on a synchronization primitive unblocks immediately
            continueThread.Reset();
            t = CreateGuardedThread(out waitForThread, () =>
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait()));
            t.IsBackground = true;
            t.Start();
            WaitForCondition(() => (t.ThreadState & ThreadState.WaitSleepJoin) != 0);
            t.Interrupt();
            waitForThread();
        }

        [Fact]
        public static void JoinTest()
        {
            var threadReady = new ManualResetEvent(false);
            var continueThread = new ManualResetEvent(false);
            Action waitForThread;
            var t =
                CreateGuardedThread(out waitForThread, () =>
                    {
                        threadReady.Set();
                        continueThread.CheckedWait();
                        Thread.Sleep(ExpectedTimeoutMilliseconds);
                    });
            t.IsBackground = true;

            Assert.Throws<ArgumentOutOfRangeException>(() => t.Join(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Join(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

            Assert.Throws<ThreadStateException>(() => t.Join());
            Assert.Throws<ThreadStateException>(() => t.Join(UnexpectedTimeoutMilliseconds));
            Assert.Throws<ThreadStateException>(() => t.Join(TimeSpan.FromMilliseconds(UnexpectedTimeoutMilliseconds)));

            t.Start();
            threadReady.CheckedWait();
            Assert.False(t.Join(ExpectedTimeoutMilliseconds));
            Assert.False(t.Join(TimeSpan.FromMilliseconds(ExpectedTimeoutMilliseconds)));
            continueThread.Set();
            waitForThread();

            Assert.True(t.Join(0));
            Assert.True(t.Join(TimeSpan.Zero));
        }

        [Fact]
        public static void SleepTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

            Thread.Sleep(0);
            var startTime = DateTime.Now;
            Thread.Sleep(500);
            Assert.True((DateTime.Now - startTime).TotalMilliseconds >= 100);
        }

        [Fact]
        public static void StartTest()
        {
            var e = new AutoResetEvent(false);
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;
            Assert.Throws<InvalidOperationException>(() => t.Start(null));
            Assert.Throws<InvalidOperationException>(() => t.Start(t));
            t.Start();
            Assert.Throws<ThreadStateException>(() => t.Start());
            e.Set();
            waitForThread();
            Assert.Throws<ThreadStateException>(() => t.Start());

            t = CreateGuardedThread(out waitForThread, parameter => e.CheckedWait());
            t.IsBackground = true;
            t.Start();
            Assert.Throws<ThreadStateException>(() => t.Start());
            Assert.Throws<ThreadStateException>(() => t.Start(null));
            Assert.Throws<ThreadStateException>(() => t.Start(t));
            e.Set();
            waitForThread();
            Assert.Throws<ThreadStateException>(() => t.Start());
            Assert.Throws<ThreadStateException>(() => t.Start(null));
            Assert.Throws<ThreadStateException>(() => t.Start(t));

            t = CreateGuardedThread(out waitForThread, parameter => Assert.Null(parameter));
            t.IsBackground = true;
            t.Start();
            waitForThread();

            t = CreateGuardedThread(out waitForThread, parameter => Assert.Null(parameter));
            t.IsBackground = true;
            t.Start(null);
            waitForThread();

            t = CreateGuardedThread(out waitForThread, parameter => Assert.Equal(t, parameter));
            t.IsBackground = true;
            t.Start(t);
            waitForThread();
        }

        [Fact]
        public static void MiscellaneousTest()
        {
            Thread.BeginCriticalRegion();
            Thread.EndCriticalRegion();
            Thread.BeginThreadAffinity();
            Thread.EndThreadAffinity();
            Thread.MemoryBarrier();

            var ad = Thread.GetDomain();
            Assert.NotNull(ad);
            Assert.Equal(AppDomain.CurrentDomain, ad);
            Assert.Equal(ad.Id, Thread.GetDomainID());

            Thread.SpinWait(int.MinValue);
            Thread.SpinWait(-1);
            Thread.SpinWait(0);
            Thread.SpinWait(1);
            Thread.Yield();
        }
    }
}
