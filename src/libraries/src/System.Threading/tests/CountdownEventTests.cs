// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit;

namespace System.Threading.Tests
{
    public class CountdownEventTests
    {
        [Fact]
        public static void RunCountdownEventTest0_StateTrans()
        {
            RunCountdownEventTest0_StateTrans(0, 0, false);
            RunCountdownEventTest0_StateTrans(1, 0, false);
            RunCountdownEventTest0_StateTrans(128, 0, false);
            RunCountdownEventTest0_StateTrans(1024 * 1024, 0, false);
            RunCountdownEventTest0_StateTrans(1, 1024, false);
            RunCountdownEventTest0_StateTrans(128, 1024, false);
            RunCountdownEventTest0_StateTrans(1024 * 1024, 1024, false);
            RunCountdownEventTest0_StateTrans(1, 0, true);
            RunCountdownEventTest0_StateTrans(128, 0, true);
            RunCountdownEventTest0_StateTrans(1024 * 1024, 0, true);
            RunCountdownEventTest0_StateTrans(1, 1024, true);
            RunCountdownEventTest0_StateTrans(128, 1024, true);
            RunCountdownEventTest0_StateTrans(1024 * 1024, 1024, true);
        }

        [Fact]
        public static void RunCountdownEventTest1_SimpleTimeout()
        {
            RunCountdownEventTest1_SimpleTimeout(0);
            RunCountdownEventTest1_SimpleTimeout(100);
        }

        // Validates init, set, reset state transitions.
        private static void RunCountdownEventTest0_StateTrans(int initCount, int increms, bool takeAllAtOnce)
        {
            string methodParameters = string.Format("RunCountdownEventTest0_StateTrans(initCount={0}, increms={1}, takeAllAtOnce={2})", initCount, increms, takeAllAtOnce);

            CountdownEvent ev = new CountdownEvent(initCount);

            // Check initial count.
            if (ev.InitialCount != initCount)
            {
                Debug.WriteLine(methodParameters);
                Assert.True(false, string.Format("  > error: initial count wrong, saw {0} expected {1}", ev.InitialCount, initCount));
            }

            // Increment (optionally).
            for (int i = 0; i < increms; i++)
            {
                ev.AddCount();
                if (ev.CurrentCount != initCount + i + 1)
                {
                    Debug.WriteLine(methodParameters);
                    Assert.True(false, string.Format("  > error: after incrementing, count is wrong, saw {0}, expect {1}", ev.CurrentCount, initCount + i + 1));
                }
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
                    if (ev.IsSet)
                    {
                        Debug.WriteLine(methodParameters);
                        Assert.True(false, string.Format("  > error: latch is set after {0} signals", i));
                    }
                    ev.Signal();
                }
            }

            // Check the status.
            if (!ev.IsSet)
            {
                Debug.WriteLine(methodParameters);
                Assert.True(false, string.Format("  > error: latch was not set after all signals received"));
            }
            if (ev.CurrentCount != 0)
            {
                Debug.WriteLine(methodParameters);
                Assert.True(false, string.Format("  > error: latch count wasn't 0 after all signals received"));
            }

            // Now reset the event and check its count.
            ev.Reset();
            if (ev.CurrentCount != ev.InitialCount)
            {
                Debug.WriteLine(methodParameters);
                Assert.True(false, string.Format("  > error: latch count wasn't correctly reset"));
            }
        }

        // Tries some simple timeout cases.
        private static void RunCountdownEventTest1_SimpleTimeout(int ms)
        {
            // Wait on the event.
            CountdownEvent ev = new CountdownEvent(999);
            if (ev.Wait(ms))
            {
                Debug.WriteLine("RunCountdownEventTest1_SimpleTimeout(ms={0})", ms);
                Assert.True(false, string.Format("  > error: wait returned true, yet it was supposed to timeout"));
            }

            if (ev.IsSet)
            {
                Debug.WriteLine("RunCountdownEventTest1_SimpleTimeout(ms={0})", ms);
                Assert.True(false, string.Format("  > error: event says it was set...  shouldn't be"));
            }

            if (ev.WaitHandle.WaitOne(ms))
            {
                Debug.WriteLine("RunCountdownEventTest1_SimpleTimeout(ms={0})", ms);
                Assert.True(false, string.Format("  > error: WaitHandle.Wait returned true, yet it was supposed to timeout"));
            }
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
