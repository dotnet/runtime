// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Threading.Tests
{
    public class CountdownEventTests
    {
        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(1, 0, false)]
        [InlineData(128, 0, false)]
        [InlineData(1024 * 1024, 0, false)]
        [InlineData(1, 1024, false)]
        [InlineData(128, 1024, false)]
        [InlineData(1024 * 1024, 1024, false)]
        [InlineData(1, 0, true)]
        [InlineData(128, 0, true)]
        [InlineData(1024 * 1024, 0, true)]
        [InlineData(1, 1024, true)]
        [InlineData(128, 1024, true)]
        [InlineData(1024 * 1024, 1024, true)]
        public static void RunCountdownEventTest0_StateTrans(int initCount, int increms, bool takeAllAtOnce)
        {
            // Validates init, set, reset state transitions.

            CountdownEvent ev = new CountdownEvent(initCount);

            Assert.Equal(initCount, ev.InitialCount);

            // Increment (optionally).
            for (int i = 1; i < increms + 1; i++)
            {
                ev.AddCount();
                Assert.Equal(initCount + i, ev.CurrentCount);
            }

            // Decrement until it hits 0.
            if (takeAllAtOnce)
            {
                ev.Signal(initCount + increms);
            }
            else
            {
                for (int i = 0; i < initCount + increms; i++)
                {
                    Assert.False(ev.IsSet, string.Format("  > error: latch is set after {0} signals", i));
                    ev.Signal();
                }
            }

            Assert.True(ev.IsSet);
            Assert.Equal(0, ev.CurrentCount);

            // Now reset the event and check its count.
            ev.Reset();
            Assert.Equal(ev.InitialCount, ev.CurrentCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public static void RunCountdownEventTest1_SimpleTimeout(int ms)
        {
            CountdownEvent ev = new CountdownEvent(999);
            Assert.False(ev.Wait(ms));
            Assert.False(ev.IsSet);
            Assert.False(ev.WaitHandle.WaitOne(ms));
        }

        [Fact]
        public static void RunCountdownEventTest2_Exceptions()
        {
            CountdownEvent cde = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => cde = new CountdownEvent(-1));
            // Failure Case: Constructor didn't throw AORE when -1 passed

            cde = new CountdownEvent(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Signal(0));
            // Failure Case: Signal didn't throw AORE when 0 passed

            cde = new CountdownEvent(0);
            Assert.Throws<InvalidOperationException>(() => cde.Signal());
            // Failure Case: Signal didn't throw IOE when the count is zero

            cde = new CountdownEvent(1);
            Assert.Throws<InvalidOperationException>(() => cde.Signal(2));
            // Failure Case: Signal didn't throw IOE when the signal count > current count

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.AddCount(0));
            // Failure Case: AddCount didn't throw AORE when 0 passed

            cde = new CountdownEvent(0);
            Assert.Throws<InvalidOperationException>(() => cde.AddCount(1));
            // Failure Case: AddCount didn't throw IOE when the count is zero

            cde = new CountdownEvent(int.MaxValue - 10);
            Assert.Throws<InvalidOperationException>(() => cde.AddCount(20));
            // Failure Case: AddCount didn't throw IOE when the count > int.Max

            cde = new CountdownEvent(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Reset(-1));
            // Failure Case: Reset didn't throw AORE when the count is zero

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Wait(-2));
            // Failure Case: Wait(int) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Wait(TimeSpan.FromDays(-1)));
            // Failure Case:  FAILED.  Wait(TimeSpan) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Wait(TimeSpan.MaxValue));
            // Failure Case: Wait(TimeSpan, CancellationToken) didn't throw AORE when the totalmilliseconds > int.max

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Wait(TimeSpan.FromDays(-1), new CancellationToken()));
            // Failure Case: Wait(TimeSpan) didn't throw AORE when the totalmilliseconds < -1

            Assert.Throws<ArgumentOutOfRangeException>(() => cde.Wait(TimeSpan.MaxValue, new CancellationToken()));
            // Failure Case: Wait(TimeSpan, CancellationToken) didn't throw AORE when the totalmilliseconds > int.max

            cde.Dispose();

            Assert.Throws<ObjectDisposedException>(() => cde.Wait());
            // Failure Case: Wait() didn't throw ODE after Dispose
        }
    }
}
