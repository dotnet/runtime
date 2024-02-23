// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class ManualResetEventSlimTests
    {
        // Validates init, set, reset state transitions.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void RunManualResetEventSlimTest0_StateTrans(bool init)
        {
            ManualResetEventSlim ev = new ManualResetEventSlim(init);
            Assert.Equal(init, ev.IsSet);

            for (int i = 0; i < 50; i++)
            {
                ev.Set();
                Assert.True(ev.IsSet);

                ev.Reset();
                Assert.False(ev.IsSet);
            }
        }

        // Uses 3 events to coordinate between two threads. Very little validation.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public static void RunManualResetEventSlimTest1_SimpleWait()
        {
            ManualResetEventSlim ev1 = new ManualResetEventSlim(false);
            ManualResetEventSlim ev2 = new ManualResetEventSlim(false);
            ManualResetEventSlim ev3 = new ManualResetEventSlim(false);

            Task.Run(delegate
            {
                ev2.Set();
                ev1.Wait();
                ev3.Set();
            });

            ev2.Wait();
            //Thread.Sleep(100);
            ev1.Set();
            ev3.Wait();
        }

        // Tests timeout on an event that is never set.
        [Fact]
        public static void RunManualResetEventSlimTest2_TimeoutWait()
        {
            for (int i = 0; i < 2; i++)
            {
                ManualResetEventSlim ev = null;
                if (i == 0) // no custom SpinCount
                    ev = new ManualResetEventSlim(false);
                else
                    ev = new ManualResetEventSlim(false, 500);
                Assert.False(ev.Wait(0));
                Assert.False(ev.Wait(100));
                Assert.False(ev.Wait(TimeSpan.FromMilliseconds(100)));

                ev.Dispose();
            }
        }

        // Tests timeout on an event that is never set.
        [Fact]
        public static void RunManualResetEventSlimTest3_ConstructorTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManualResetEventSlim(false, 2048)); //max value is 2047.

            Assert.Throws<ArgumentOutOfRangeException>(() => new ManualResetEventSlim(false, -1));
        }

        // Tests that the shared state variable seems to be working correctly.
        [Fact]
        public static void RunManualResetEventSlimTest4_CombinedStateTests()
        {
            ManualResetEventSlim mres = new ManualResetEventSlim(false, 100);
            Assert.False(mres.IsSet,
               "RunManualResetEventSlimTest4_CombinedStateTests:  FAILED.  Set did not read correctly.");
            mres.Set();
            Assert.True(mres.IsSet,
               "RunManualResetEventSlimTest4_CombinedStateTests:  FAILED.  Set did not write/read correctly.");
        }

        [Fact]
        public static void RunManualResetEventSlimTest5_Dispose()
        {
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            mres.Dispose();
        }

        [Fact]
        public static void RunManualResetEventSlimTest5_Dispose_Negative()
        {
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            mres.Dispose();

            Assert.Throws<ObjectDisposedException>(() => mres.Reset());
            // Failure Case: The object has been disposed, should throw ObjectDisposedException.

            Assert.Throws<ObjectDisposedException>(() => mres.Wait(0));
            // Failure Case: The object has been disposed, should throw ObjectDisposedException.

            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    WaitHandle handle = mres.WaitHandle;
                });
            // Failure Case: The object has been disposed, should throw ObjectDisposedException.

            mres = new ManualResetEventSlim(false);

            ManualResetEvent mre = (ManualResetEvent)mres.WaitHandle;
            mres.Dispose();

            Assert.Throws<ObjectDisposedException>(() => mre.WaitOne(0));
            // Failure Case: The underlying event object has been disposed, should throw ObjectDisposedException.

        }

        [Fact]
        public static void RunManualResetEventSlimTest6_Exceptions()
        {
            ManualResetEventSlim mres = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => mres = new ManualResetEventSlim(false, -1));
            // Failure Case: Constructor didn't throw AORE when -1 passed

            mres = new ManualResetEventSlim(false);

            Assert.Throws<ArgumentOutOfRangeException>(() => mres.Wait(-2));
            // Failure Case: Wait(int) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => mres.Wait(TimeSpan.FromDays(-1)));
            // Failure Case: Wait(TimeSpan) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => mres.Wait(TimeSpan.MaxValue));
            // Failure Case: Wait(TimeSpan, CancellationToken) didn't throw AORE when the totalmilliseconds > int.max

            Assert.Throws<ArgumentOutOfRangeException>(() => mres.Wait(TimeSpan.FromDays(-1), new CancellationToken()));
            // Failure Case: Wait(TimeSpan) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => mres.Wait(TimeSpan.MaxValue, new CancellationToken()));
            // Failure Case: Wait(TimeSpan, CancellationToken) didn't throw AORE when the totalmilliseconds > int.max
        }
    }
}
