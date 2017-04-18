// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    static partial class MonitorTests
    {
        private const int FailTimeoutMilliseconds = 30000;

        // Attempts a single recursive acquisition/release cycle of a newly-created lock.
        [Fact]
        public static void BasicRecursion(ref string message)
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
        public static void DeepRecursion(ref string message)
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
            Assert.True(Monitor.IsEntered(obj));
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
            Assert.False(lockTaken);
        }

        [Fact]
        public static void Enter_Invalid()
        {
            bool lockTaken = false;
            var obj = new object();

            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Enter(null));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Enter(null, ref lockTaken));
            Assert.False(lockTaken);

            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.Enter(obj, ref lockTaken));
            Assert.False(lockTaken);
        }

        [Fact]
        public static void Exit_Invalid()
        {
            var obj = new object();
            int valueType = 1;
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Exit(null));

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
            Assert.False(lockTaken);
        }

        [Fact]
        public static void TryEnter_Invalid()
        {
            bool lockTaken = false;
            var obj = new object();

            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null, ref lockTaken));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null, 1));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null, 1, ref lockTaken));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.TryEnter(null, TimeSpan.Zero, ref lockTaken));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeout", () => Monitor.TryEnter(null, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeout", () => Monitor.TryEnter(null, -1, ref lockTaken));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.TryEnter(null, TimeSpan.FromMilliseconds(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.TryEnter(null, TimeSpan.FromMilliseconds(-1), ref lockTaken));

            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, ref lockTaken));
            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, 0, ref lockTaken));
            lockTaken = true;
            AssertExtensions.Throws<ArgumentException>("lockTaken", () => Monitor.TryEnter(obj, TimeSpan.Zero, ref lockTaken));
        }

        [Fact]
        public static void Wait_Invalid()
        {
            var obj = new object();
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Wait(null));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Wait(null, 1));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => Monitor.Wait(null, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("millisecondsTimeout", () => Monitor.Wait(null, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => Monitor.Wait(null, TimeSpan.FromMilliseconds(-1)));
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
