// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedMonthName
    {
        private const int c_MIN_MONTH_VALUE = 1;
        private const int c_MAX_MONTH_VALUE = 13;

        // PosTest1: Call GetAbbreviatedMonthName on default invariant DateTimeFormatInfo instance
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

        // PosTest2: Call GetAbbreviatedMonthName on en-us culture DateTimeFormatInfo instance
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

        // PosTest3: Call GetAbbreviatedMonthName on fr-FR culture DateTimeFormatInfo instance
        // When dotnet/corefx#2103 is addressed, we should remove the conditional fact attribute and select the right data for fr-FR
        [ConditionalFact("IsNotUbuntu1510")]
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

        // PosTest4: Call GetAbbreviatedMonthName on DateTimeFormatInfo instance created from ctor
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

        public static bool IsNotUbuntu1510()
        {
            return !PlatformDetection.IsUbuntu1510;
        }
    }
}
