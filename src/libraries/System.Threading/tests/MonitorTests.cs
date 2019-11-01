// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static partial class MonitorTests
    {
        private const int FailTimeoutMilliseconds = 30000;

        // Attempts a single recursive acquisition/release cycle of a newly-created lock.
        [Fact]
        public static void BasicRecursion()
        {
            var obj = new object();
            Assert.True(Monitor.TryEnter(obj));
            Assert.True(Monitor.TryEnter(obj));
            Monitor.Exit(obj);
            Assert.True(Monitor.IsEntered(obj));
            Monitor.Enter(obj);
            Assert.True(Monitor.IsEntered(obj));
            Monitor.Exit(obj);
            Assert.True(Monitor.IsEntered(obj));
            Monitor.Exit(obj);
            Assert.False(Monitor.IsEntered(obj));
        }

        // Attempts to overflow the recursion count of a newly-created lock.
        [Fact]
        public static void DeepRecursion()
        {
            var obj = new object();
            var hc = obj.GetHashCode();
            // reduced from "(long)int.MaxValue + 2;" to something that will return in a more meaningful time
            const int limit = 10000;

            for (var i = 0L; i < limit; i++)
            {
                Assert.True(Monitor.TryEnter(obj));
            }

            for (var j = 0L; j < (limit - 1); j++)
            {
                Monitor.Exit(obj);
                Assert.True(Monitor.IsEntered(obj));
            }

            Monitor.Exit(obj);
            Assert.False(Monitor.IsEntered(obj));
        }

        [Fact]
        public static void IsEntered()
        {
            var obj = new object();
            Assert.False(Monitor.IsEntered(obj));
            lock (obj)
            {
                Assert.True(Monitor.IsEntered(obj));
            }
            Assert.False(Monitor.IsEntered(obj));
        }

        [Fact]
        public static void IsEntered_WhenHeldBySomeoneElse_ThrowsSynchronizationLockException()
        {
            var obj = new object();
            var b = new Barrier(2);

            Task t = Task.Run(() =>
            {
                lock (obj)
                {
                    b.SignalAndWait();
                    Assert.True(Monitor.IsEntered(obj));
                    b.SignalAndWait();
                }
            });

            b.SignalAndWait();
            Assert.False(Monitor.IsEntered(obj));
            b.SignalAndWait();

            t.Wait();
        }

        [Fact]
        public static void Enter_SetsLockTaken()
        {
            bool lockTaken = false;
            var obj = new object();

            Monitor.Enter(obj, ref lockTaken);
            Assert.True(lockTaken);
            Monitor.Exit(obj);
            Assert.True(lockTaken);
        }

        [Fact]
        public static void Enter_Invalid()
        {
            bool lockTaken = false;
            var obj = new object();

            Assert.Throws<ArgumentNullException>(() => Monitor.Enter(null));
            Assert.Throws<ArgumentNullException>(() => Monitor.Enter(null, ref lockTaken));
            Assert.False(lockTaken);

            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.Enter(obj, ref lockTaken));
            Assert.True(lockTaken);
        }

        [Fact]
        public static void Exit_Invalid()
        {
            var obj = new object();
            int valueType = 1;
            Assert.Throws<ArgumentNullException>(() => Monitor.Exit(null));

            Assert.Throws<SynchronizationLockException>(() => Monitor.Exit(obj));
            Assert.Throws<SynchronizationLockException>(() => Monitor.Exit(new object()));
            Assert.Throws<SynchronizationLockException>(() => Monitor.Exit(valueType));
        }

        [Fact]
        public static void Exit_WhenHeldBySomeoneElse_ThrowsSynchronizationLockException()
        {
            var obj = new object();
            var b = new Barrier(2);

            Task t = Task.Run(() =>
            {
                lock (obj)
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }
            });

            b.SignalAndWait();
            Assert.Throws<SynchronizationLockException>(() => Monitor.Exit(obj));
            b.SignalAndWait();

            t.Wait();
        }

        [Fact]
        public static void IsEntered_Invalid()
        {
            var obj = new object();
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.IsEntered(null));
        }

        [Fact]
        public static void Pulse_Invalid()
        {
            var obj = new object();
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Pulse(null));
        }

        [Fact]
        public static void PulseAll_Invalid()
        {
            var obj = new object();
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.PulseAll(null));
        }

        [Fact]
        public static void TryEnter_SetsLockTaken()
        {
            bool lockTaken = false;
            var obj = new object();

            Monitor.TryEnter(obj, ref lockTaken);
            Assert.True(lockTaken);
            Monitor.Exit(obj);
            Assert.True(lockTaken);
        }

        [Fact]
        public static void TryEnter_Invalid()
        {
            bool lockTaken = false;
            var obj = new object();

            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null));
            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null, ref lockTaken));
            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null, 1));
            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null, 1, ref lockTaken));
            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null, TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>(() => Monitor.TryEnter(null, TimeSpan.Zero, ref lockTaken));

            Assert.Throws<ArgumentOutOfRangeException>(() => Monitor.TryEnter(obj, -2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monitor.TryEnter(obj, -2, ref lockTaken));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.TryEnter(obj, TimeSpan.FromMilliseconds(-2), ref lockTaken));

            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, ref lockTaken));
            Assert.True(lockTaken);
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, 0, ref lockTaken));
            Assert.True(lockTaken);
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, TimeSpan.Zero, ref lockTaken));
        }

        [Fact]
        public static void Enter_HasToWait()
        {
            var thinLock = new object();
            var awareLock = new object();

            // Actually transition the aware lock to an aware lock by having a background thread wait for a lock
            {
                Action waitForThread;
                Thread t =
                    ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                        Assert.False(Monitor.TryEnter(awareLock, ThreadTestHelpers.ExpectedTimeoutMilliseconds)));
                t.IsBackground = true;
                lock (awareLock)
                {
                    t.Start();
                    waitForThread();
                }
            }

            // When the current thread has the lock, have background threads wait for the lock in various ways. After a short
            // duration, release the lock and allow the background threads to acquire the lock.
            {
                var backgroundTestDelegates = new List<Action<object>>();
                Barrier readyBarrier = null;

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    Monitor.Enter(lockObj);
                    Monitor.Exit(lockObj);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    bool lockTaken = false;
                    Monitor.Enter(lockObj, ref lockTaken);
                    Assert.True(lockTaken);
                    Monitor.Exit(lockObj);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    lock (lockObj)
                    {
                    }
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.True(Monitor.TryEnter(lockObj, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                    Monitor.Exit(lockObj);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.True(
                        Monitor.TryEnter(lockObj, TimeSpan.FromMilliseconds(ThreadTestHelpers.UnexpectedTimeoutMilliseconds)));
                    Monitor.Exit(lockObj);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    bool lockTaken = false;
                    Monitor.TryEnter(lockObj, ThreadTestHelpers.UnexpectedTimeoutMilliseconds, ref lockTaken);
                    Assert.True(lockTaken);
                    Monitor.Exit(lockObj);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    bool lockTaken = false;
                    Monitor.TryEnter(
                        lockObj,
                        TimeSpan.FromMilliseconds(ThreadTestHelpers.UnexpectedTimeoutMilliseconds),
                        ref lockTaken);
                    Assert.True(lockTaken);
                    Monitor.Exit(lockObj);
                });

                int testCount = backgroundTestDelegates.Count * 2; // two iterations each, one for thin lock and one for aware lock
                readyBarrier = new Barrier(testCount + 1); // plus main thread
                var waitForThreadArray = new Action[testCount];
                for (int i = 0; i < backgroundTestDelegates.Count; ++i)
                {
                    int icopy = i; // for use in delegates
                    Thread t =
                        ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i * 2],
                            () => backgroundTestDelegates[icopy](thinLock));
                    t.IsBackground = true;
                    t.Start();
                    t = ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i * 2 + 1],
                            () => backgroundTestDelegates[icopy](awareLock));
                    t.IsBackground = true;
                    t.Start();
                }

                lock (thinLock)
                {
                    lock (awareLock)
                    {
                        readyBarrier.SignalAndWait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                        Thread.Sleep(ThreadTestHelpers.ExpectedTimeoutMilliseconds);
                    }
                }
                foreach (Action waitForThread in waitForThreadArray)
                    waitForThread();
            }

            // When the current thread has the lock, have background threads wait for the lock in various ways and time out
            // after a short duration
            {
                var backgroundTestDelegates = new List<Action<object>>();
                Barrier readyBarrier = null;

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.False(Monitor.TryEnter(lockObj, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    Assert.False(
                        Monitor.TryEnter(lockObj, TimeSpan.FromMilliseconds(ThreadTestHelpers.ExpectedTimeoutMilliseconds)));
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    bool lockTaken = false;
                    Monitor.TryEnter(lockObj, ThreadTestHelpers.ExpectedTimeoutMilliseconds, ref lockTaken);
                    Assert.False(lockTaken);
                });

                backgroundTestDelegates.Add(lockObj =>
                {
                    readyBarrier.SignalAndWait();
                    bool lockTaken = false;
                    Monitor.TryEnter(
                        lockObj,
                        TimeSpan.FromMilliseconds(ThreadTestHelpers.ExpectedTimeoutMilliseconds),
                        ref lockTaken);
                    Assert.False(lockTaken);
                });

                int testCount = backgroundTestDelegates.Count * 2; // two iterations each, one for thin lock and one for aware lock
                readyBarrier = new Barrier(testCount + 1); // plus main thread
                var waitForThreadArray = new Action[testCount];
                for (int i = 0; i < backgroundTestDelegates.Count; ++i)
                {
                    int icopy = i; // for use in delegates
                    Thread t =
                        ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i * 2],
                            () => backgroundTestDelegates[icopy](thinLock));
                    t.IsBackground = true;
                    t.Start();
                    t = ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i * 2 + 1],
                            () => backgroundTestDelegates[icopy](awareLock));
                    t.IsBackground = true;
                    t.Start();
                }

                lock (thinLock)
                {
                    lock (awareLock)
                    {
                        readyBarrier.SignalAndWait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                        foreach (Action waitForThread in waitForThreadArray)
                            waitForThread();
                    }
                }
            }
        }

        [Fact]
        public static void Wait_Invalid()
        {
            var obj = new object();
            Assert.Throws<ArgumentNullException>(() => Monitor.Wait(null));
            Assert.Throws<ArgumentNullException>(() => Monitor.Wait(null, 1));
            Assert.Throws<ArgumentNullException>(() => Monitor.Wait(null, TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => Monitor.Wait(obj, -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.Wait(obj, TimeSpan.FromMilliseconds(-2)));
        }

        [Fact]
        public static void WaitTest()
        {
            var obj = new object();
            var waitTests =
                new Func<bool>[]
                {
                    () => Monitor.Wait(obj, FailTimeoutMilliseconds, false),
                    () => Monitor.Wait(obj, FailTimeoutMilliseconds, true),
                    () => Monitor.Wait(obj, TimeSpan.FromMilliseconds(FailTimeoutMilliseconds), false),
                    () => Monitor.Wait(obj, TimeSpan.FromMilliseconds(FailTimeoutMilliseconds), true),
                };

            var t =
                new Thread(() =>
                {
                    Monitor.Enter(obj);
                    for (int i = 0; i < waitTests.Length; ++i)
                    {
                        Monitor.Pulse(obj);
                        Monitor.Wait(obj, FailTimeoutMilliseconds);
                    }
                    Monitor.Exit(obj);
                });
            t.IsBackground = true;

            Monitor.Enter(obj);
            t.Start();
            foreach (var waitTest in waitTests)
            {
                Assert.True(waitTest());
                Monitor.Pulse(obj);
            }
            Monitor.Exit(obj);
        }
    }
}
