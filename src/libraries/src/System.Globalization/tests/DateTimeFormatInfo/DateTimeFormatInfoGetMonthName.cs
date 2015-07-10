// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetMonthName
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
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December",
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
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December",
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
                "janvier",
                "f\u00E9vrier",
                "mars",
                "avril",
                "mai",
                "juin",
                "juillet",
                "ao\u00FBt",
                "septembre",
                "octobre",
                "novembre",
                "d\u00E9cembre",
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
                "January",
                "February",
                "March",
                "April",
                "May",
                "June",
                "July",
                "August",
                "September",
                "October",
                "November",
                "December",
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
                info1.GetMonthName(c_MIN_MONTH_VALUE - 1);
            });

            DateTimeFormatInfo info2 = new DateTimeFormatInfo();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               info2.GetMonthName(c_MAX_MONTH_VALUE + 1);
           });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string[] expected)
        {
            for (int i = c_MIN_MONTH_VALUE; i <= c_MAX_MONTH_VALUE; ++i)
            {
                string actual = info.GetMonthName(i);
                Assert.Equal(expected[i], actual);
            }
        }
    }
}
