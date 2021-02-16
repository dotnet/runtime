// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Xunit;

using TestClock = System.Tests.TimeClockTests.TestClock;

namespace System.Tests
{
    public class ActualSystemClockTests
    {
        [Fact]
        public void CurrentUtcDateTimeOffset_Advances()
        {
            DateTimeOffset start = ActualSystemClock.Instance.GetCurrentUtcDateTimeOffset();
            Assert.True(
                SpinWait.SpinUntil(() => ActualSystemClock.Instance.GetCurrentUtcDateTimeOffset() > start, TimeSpan.FromSeconds(30)),
                "Expected GetCurrentUtcDateTimeOffset result to change");
        }

        [Fact]
        public void CurrentUtcDateTime_Advances()
        {
            DateTime start = ActualSystemClock.Instance.GetCurrentUtcDateTime();
            Assert.True(
                SpinWait.SpinUntil(() => ActualSystemClock.Instance.GetCurrentUtcDateTime() > start, TimeSpan.FromSeconds(30)),
                "Expected GetCurrentUtcDateTime result to change");
        }

        [Fact]
        public void CurrentUtcDateTimeOffset_AdvancesEvenWhenAnotherFixedClockIsActive()
        {
            // The test clock has a fixed value that never changes.
            var testClock = new TestClock();

            TimeContext.Run(testClock, () =>
            {
                // Make sure the test clock is active, not the actual system clock.
                Assert.Same(testClock, TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);

                // Despite the test clock being active, the actual system clock should still advance.
                DateTimeOffset start = ActualSystemClock.Instance.GetCurrentUtcDateTimeOffset();
                Assert.True(
                    SpinWait.SpinUntil(() => ActualSystemClock.Instance.GetCurrentUtcDateTimeOffset() > start, TimeSpan.FromSeconds(30)),
                    "Expected GetCurrentUtcDateTimeOffset result to change");
            });
        }

        [Fact]
        public void CurrentUtcDateTime_AdvancesEvenWhenAnotherFixedClockIsActive()
        {
            // The test clock has a fixed value that never changes.
            var testClock = new TestClock();

            TimeContext.Run(testClock, () =>
            {
                // Make sure the test clock is active, not the actual system clock.
                Assert.Same(testClock, TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);

                // Despite the test clock being active, the actual system clock should still advance.
                DateTime start = ActualSystemClock.Instance.GetCurrentUtcDateTime();
                Assert.True(
                    SpinWait.SpinUntil(() => ActualSystemClock.Instance.GetCurrentUtcDateTime() > start, TimeSpan.FromSeconds(30)),
                    "Expected GetCurrentUtcDateTime result to change");
            });
        }
    }
}
