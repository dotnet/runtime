// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarIsLeapMonth
    {
        [Fact]
        public void IsLeapMonth_ReturnsFalse()
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            Assert.False(calendar.IsLeapMonth(TaiwanCalendarUtilities.RandomYear(), TaiwanCalendarUtilities.RandomMonth()));
            Assert.False(calendar.IsLeapMonth(TaiwanCalendarUtilities.RandomYear(), TaiwanCalendarUtilities.RandomMonth(), 0));
            Assert.False(calendar.IsLeapMonth(TaiwanCalendarUtilities.RandomYear(), TaiwanCalendarUtilities.RandomMonth(), 1));
        }
    }
}
