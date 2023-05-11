// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Threading.Tests
{
    public class TimerChangeTests
    {
        private void EmptyTimerTarget(object o) { }

        [Fact]
        public void Timer_Change_DueTime_OutOfRange_Throws()
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(-2, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(-2L, 1L));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(-2), TimeSpan.FromSeconds(1)));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(0xFFFFFFFFL, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => t.Change(TimeSpan.FromMilliseconds(0xFFFFFFFFL), TimeSpan.FromSeconds(1)));
            }
        }

        [Fact]
        public void Timer_Change_Period_OutOfRange_Throws()
        {
            using (var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => t.Change(1, -2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => t.Change(1L, -2L));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-2)));

                AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => t.Change(1, 0xFFFFFFFFL));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => t.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(0xFFFFFFFFL)));
            }
        }

        [Fact]
        public void Timer_Change_AfterDispose_Test()
        {
            var t = new Timer(new TimerCallback(EmptyTimerTarget), null, 1, 1);
            Assert.True(t.Change(1, 1));
            t.Dispose();
            Assert.False(t.Change(1, 1));
            Assert.False(t.Change(1L, 1L));
            Assert.False(t.Change(1u, 1u));
            Assert.False(t.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Timer_Change_BeforeDueTime_ChangesWhenTimerWillFire(int mode)
        {
            var are = new AutoResetEvent(false);

            using (var t = new Timer(_ => are.Set(), null, TimeSpan.FromSeconds(500), TimeSpan.FromMilliseconds(50)))
            {
                Assert.False(are.WaitOne(TimeSpan.FromMilliseconds(100)), "The reset event should not have been set yet");
                switch (mode)
                {
                    case 0:
                        t.Change(100, -1);
                        break;
                    case 1:
                        t.Change(100L, -1L);
                        break;
                    case 2:
                        t.Change(100u, uint.MaxValue);
                        break;
                    case 3:
                        t.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(-1));
                        break;
                }
                Assert.True(are.WaitOne(TimeSpan.FromMilliseconds(TimerFiringTests.MaxPositiveTimeoutInMs)), "Should have received a timer event after this new duration");
            }
        }
    }
}
