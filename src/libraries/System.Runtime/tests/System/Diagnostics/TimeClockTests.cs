// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public class TimeClockTests
    {
        public class TestClock : TimeClock
        {
            public static readonly DateTimeOffset Value = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
            protected override DateTimeOffset GetCurrentUtcDateTimeOffsetImpl() => Value;
        }

        [Fact]
        public void CurrentDateTimeOffset_AdjustsToUniversalTime()
        {
            TimeClock clock = new TestClock();
            DateTimeOffset actual = clock.GetCurrentUtcDateTimeOffset();

            DateTimeOffset expected = TestClock.Value.ToUniversalTime();
            Assert.Equal(expected.DateTime, actual.DateTime);
            Assert.Equal(expected.Offset, actual.Offset);
        }

        [Fact]
        public void CurrentDateTime_AdjustsToUniversalTime()
        {
            TimeClock clock = new TestClock();
            DateTime actual = clock.GetCurrentUtcDateTime();

            DateTime expected = TestClock.Value.UtcDateTime;
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Kind, actual.Kind);
        }
    }
}
