// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class ChineseLunisolarCalendarTests : EastAsianLunisolarCalendarTestBase
    {
        public override Calendar Calendar => new ChineseLunisolarCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(1901, 02, 19);

        public override DateTime MaxSupportedDateTime => new DateTime(2101, 01, 28, 23, 59, 59).AddTicks(9999999);

        [Fact]
        public void OffByOneDay()
        {
            // The new moon separating lunar months 8/9 in 2057, 7/8 in 2089, 
            // and 6/7 in 2097 occurs close to midnight local time (UTC+8). 
            // The exact time cannot be determined accurately in advance.
            // This may lead to off-by-one-day errors if the predictions made 
            // by astronomical calculations turn out to be wrong. 
            // The current table entries for 2057, 2089, and 2097 are validated 
            // using 'Calendrical Calculations (Ultimate Edition)'. 
            // This test guards against accidental regression, should future 
            // recalibrations be required.

            // Lunar month 8 of 2057 has 30 days
            Assert.AreEqual(Calendar.GetDaysInMonth(2057, 8), 30);

            // Lunar month 9 of 2057 has 29 days and starts on 29 Sep 2057
            Assert.AreEqual(Calendar.GetDaysInMonth(2057, 9), 29);
            Assert.AreEqual(new DateOnly(2057, 9, 1, Calendar), new DateOnly(2057, 9, 28));

            // Lunar month 7 of 2089 has 29 days
            Assert.AreEqual(Calendar.GetDaysInMonth(2089, 7), 29); 

            // Lunar month 8 of 2089 has 30 days and starts on 04 Sep 2089
            Assert.AreEqual(Calendar.GetDaysInMonth(2089, 8), 30); 
            Assert.AreEqual(new DateOnly(2089, 8, 1, Calendar), new DateOnly(2089, 9, 4));

            // Lunar month 6 of 2097 has 29 days
            Assert.AreEqual(Calendar.GetDaysInMonth(2097, 6), 29);

            // Lunar month 7 of 2097 has 30 days and starts on 07 Aug 2097
            Assert.AreEqual(Calendar.GetDaysInMonth(2097, 7), 30); 
            Assert.AreEqual(new DateOnly(2097, 7, 1, Calendar), new DateOnly(2097, 8, 7));
        }
    }
}
