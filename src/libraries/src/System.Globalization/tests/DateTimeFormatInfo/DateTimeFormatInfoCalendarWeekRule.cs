// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        // PosTest1: Call CalendarWeekRule getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    CalendarWeekRule.FirstDay,
                    false);
        }

        // PosTest2: Call CalendarWeekRule setter method should return correct value
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

        // NegTest1: ArgumentOutOfRangeException should be thrown when The property is being set to a value that is 
        // not a valid CalendarWeekRule value
        [Fact]
        public void TestInvalidValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new DateTimeFormatInfo().CalendarWeekRule = (CalendarWeekRule)(-1);
            });
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
