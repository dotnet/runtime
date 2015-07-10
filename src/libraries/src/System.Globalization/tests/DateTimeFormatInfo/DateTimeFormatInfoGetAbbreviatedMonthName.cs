// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedMonthName
    {
        private const int c_MIN_MONTH_VALUE = 1;
        private const int c_MAX_MONTH_VALUE = 13;

        // PosTest1: Call GetAbbreviatedDayName on default invariant DateTimeFormatInfo instance
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb",
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            VerificationHelper(info, expected);
        }

        // PosTest2: Call GetAbbreviatedDayName on en-us culture DateTimeFormatInfo instance
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb",
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            VerificationHelper(info, expected);
        }

        // PosTest3: Call GetAbbreviatedDayName on fr-FR culture DateTimeFormatInfo instance
        [Fact]
        public void PosTest3()
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;
            string[] expected = new string[] {
                "",
                "janv.",
                "f\u00E9vr.",
                "mars",
                "avr.",
                "mai",
                "juin",
                "juil.",
                "ao\u00FBt",
                "sept.",
                "oct.",
                "nov.",
                "d\u00E9c.",
                "",
            };

            VerificationHelper(info, expected);
        }

        // PosTest4: Call GetAbbreviatedDayName on DateTimeFormatInfo instance created from ctor
        [Fact]
        public void PosTest4()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb",
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            VerificationHelper(info, expected);
        }

        // NegTest1: ArgumentOutOfRangeException should be thrown when dayofweek is not a valid System.DayOfWeek value
        [Fact]
        public void TestInvalidDayOfWeek()
        {
            DateTimeFormatInfo info1 = new DateTimeFormatInfo();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                info1.GetAbbreviatedMonthName(c_MIN_MONTH_VALUE - 1);
            });

            DateTimeFormatInfo info2 = new DateTimeFormatInfo();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                info2.GetAbbreviatedMonthName(c_MAX_MONTH_VALUE + 1);
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string[] expected)
        {
            for (int i = c_MIN_MONTH_VALUE; i <= c_MAX_MONTH_VALUE; ++i)
            {
                string actual = info.GetAbbreviatedMonthName(i);
                Assert.Equal(expected[i], actual);
            }
        }
    }
}
