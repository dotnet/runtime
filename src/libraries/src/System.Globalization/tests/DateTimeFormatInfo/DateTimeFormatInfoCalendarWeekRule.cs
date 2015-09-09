// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        // TestGetter: Call CalendarWeekRule getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    CalendarWeekRule.FirstDay,
                    false);
        }

        // TestSetter: Call CalendarWeekRule setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            VerificationHelper(new DateTimeFormatInfo(),
                CalendarWeekRule.FirstDay,
                true);
            VerificationHelper(new DateTimeFormatInfo(),
                CalendarWeekRule.FirstFourDayWeek,
                true);
            VerificationHelper(new DateTimeFormatInfo(),
                CalendarWeekRule.FirstFullWeek,
                true);
        }

        // TestInvalidValue: ArgumentOutOfRangeException should be thrown when The property is being set to a value that is 
        // not a valid CalendarWeekRule value
        [Fact]
        public void TestInvalidValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new DateTimeFormatInfo().CalendarWeekRule = (CalendarWeekRule)(-1);
            });
        }

        // TestLocaleFirstDay: Verify value of property CalendarWeekRule for specific locale
        [Fact]
        public void TestLocaleFirstDay()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            DateTimeFormatInfo dti = myTestCulture.DateTimeFormat;
            CalendarWeekRule actual = dti.CalendarWeekRule;
            Assert.Equal(CalendarWeekRule.FirstDay, actual);
        }

        // TestLocaleFirstFourDayWeek: Verify value of property CalendarWeekRule for specific locale
        [Fact]
        public void TestLocaleFirstFourDayWeek()
        {
            CultureInfo myTestCulture = new CultureInfo("br-FR");
            DateTimeFormatInfo dti = myTestCulture.DateTimeFormat;
            CalendarWeekRule actual = dti.CalendarWeekRule;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // todo: determine why this is not FirstFourDayWeek; Full .NET returns FirstFourDayWeek
                Assert.Equal(CalendarWeekRule.FirstFullWeek, actual);
            }
            else
            {
                Assert.Equal(CalendarWeekRule.FirstFourDayWeek, actual);
            }
        }

        private void VerificationHelper(DateTimeFormatInfo info, CalendarWeekRule expected, bool setter)
        {
            if (setter)
            {
                info.CalendarWeekRule = expected;
            }

            CalendarWeekRule actual = info.CalendarWeekRule;
            Assert.Equal(expected, actual);
        }
    }
}
