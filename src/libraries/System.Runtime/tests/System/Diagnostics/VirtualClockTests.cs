// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.Tests
{
    public class VirtualClockTests
    {
        [Fact]
        public void CanUseFixedDateTimeOffsetValue()
        {
            var value = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TimeClock clock = new VirtualClock(value);

            DateTimeOffset firstResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(value, firstResult);

            DateTimeOffset secondResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(value, secondResult);

            DateTimeOffset thirdResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(value, thirdResult);
        }

        [Fact]
        public void CanUseFixedDateTimeValue()
        {
            var value = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeClock clock = new VirtualClock(value);

            DateTime firstResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(value, firstResult);

            DateTime secondResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(value, secondResult);

            DateTime thirdResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(value, thirdResult);
        }

        [Fact]
        public void CanUseInitialDateTimeOffsetValueAndTimespanIncrement()
        {
            var initialValue = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var increment = TimeSpan.FromHours(1);
            TimeClock clock = new VirtualClock(initialValue, increment);

            DateTimeOffset firstResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue, firstResult);

            DateTimeOffset secondResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue.Add(increment), secondResult);

            DateTimeOffset thirdResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue.Add(increment * 2), thirdResult);
        }

        [Fact]
        public void CanUseInitialDateTimeValueAndTimespanIncrement()
        {
            var initialValue = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var increment = TimeSpan.FromHours(1);
            TimeClock clock = new VirtualClock(initialValue, increment);

            DateTime firstResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue, firstResult);

            DateTime secondResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue.Add(increment), secondResult);

            DateTime thirdResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue.Add(increment * 2), thirdResult);
        }

        [Fact]
        public void CanUseInitialDateTimeOffsetValueAndFunctionIncrement()
        {
            var initialValue = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var increment = TimeSpan.FromHours(1);
            TimeClock clock = new VirtualClock(initialValue, x => x.Add(increment));

            DateTimeOffset firstResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue, firstResult);

            DateTimeOffset secondResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue.Add(increment), secondResult);

            DateTimeOffset thirdResult = clock.GetCurrentUtcDateTimeOffset();
            Assert.Equal(initialValue.Add(increment * 2), thirdResult);
        }

        [Fact]
        public void CanUseInitialDateTimeValueAndFunctionIncrement()
        {
            var initialValue = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var increment = TimeSpan.FromHours(1);
            TimeClock clock = new VirtualClock(initialValue, x => x.Add(increment));

            DateTime firstResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue, firstResult);

            DateTime secondResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue.Add(increment), secondResult);

            DateTime thirdResult = clock.GetCurrentUtcDateTime();
            Assert.Equal(initialValue.Add(increment * 2), thirdResult);
        }
    }
}
