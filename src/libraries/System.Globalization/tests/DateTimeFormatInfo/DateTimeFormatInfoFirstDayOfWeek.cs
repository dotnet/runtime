// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFirstDayOfWeek
    {
        // PosTest1: Call FirstDayOfWeek getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    DayOfWeek.Sunday,
                    false);
        }

        // PosTest2: Call FirstDayOfWeek setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            DayOfWeek[] days = new DayOfWeek[] {
                DayOfWeek.Friday,
                DayOfWeek.Monday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday,
                DayOfWeek.Thursday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
            };

            for (int i = 0; i < days.Length; ++i)
            {
                VerificationHelper(new DateTimeFormatInfo(),
                    days[i],
                    true);
            }
        }

        // NegTest1: ArgumentOutOfRangeException should be thrown when The property is being set to a 
        // value that is not a valid FirstDayOfWeek value
        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new DateTimeFormatInfo().FirstDayOfWeek = (DayOfWeek)(-1);
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and 
        // the DateTimeFormatInfo is read-only
        [Fact]
        public void NegTest2()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.FirstDayOfWeek = DayOfWeek.Wednesday;
            });
        }

        // TestLocaleSunday: Verify value of property FirstDayOfWeek for specific locale
        [Fact]
        public void TestLocaleSunday()
        {
            CultureInfo myTestCulture = new CultureInfo("en-US");
            DateTimeFormatInfo dti = myTestCulture.DateTimeFormat;
            DayOfWeek actual = dti.FirstDayOfWeek;
            Assert.Equal(DayOfWeek.Sunday, actual);
        }

        // TestLocaleMonday: Verify value of property FirstDayOfWeek for specific locale
        [Fact]
        public void TestLocaleMonday()
        {
            CultureInfo myTestCulture = new CultureInfo("fr");
            DateTimeFormatInfo dti = myTestCulture.DateTimeFormat;
            DayOfWeek actual = dti.FirstDayOfWeek;
            Assert.Equal(DayOfWeek.Monday, actual);
        }

        private void VerificationHelper(DateTimeFormatInfo info, DayOfWeek expected, bool setter)
        {
            if (setter)
            {
                info.FirstDayOfWeek = expected;
            }

            DayOfWeek actual = info.FirstDayOfWeek;
            Assert.Equal(expected, actual);
        }
    }
}
