// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class DateAndTimeTests
    {
        [Fact]
        public void Now_ReturnsDateTimeNow()
        {
            var dateTimeNowBefore = DateTime.Now;
            var now = DateAndTime.Now;
            var dateTimeNowAfter = DateTime.Now;

            Assert.InRange(now, dateTimeNowBefore, dateTimeNowAfter);
        }

        [Fact]
        public void Today_ReturnsDateTimeToday()
        {
            var dateTimeTodayBefore = DateTime.Today;
            var today = DateAndTime.Today;
            var dateTimeTodayAfter = DateTime.Today;

            Assert.InRange(today, dateTimeTodayBefore, dateTimeTodayAfter);
            Assert.Equal(TimeSpan.Zero, today.TimeOfDay);
        }
    }
}
