// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedDayName
    {
        // PosTest1: Call GetAbbreviatedDayName on default invariant DateTimeFormatInfo instance
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] expected = new string[] {
                "Sun",
                "Mon",
                "Tue",
                "Wed",
                "Thu",
                "Fri",
                "Sat"
            };
            VerificationHelper(info, expected);
        }

        // PosTest2: Call GetAbbreviatedDayName on en-us culture DateTimeFormatInfo instance
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;
            string[] expected = new string[] {
                "Sun",
                "Mon",
                "Tue",
                "Wed",
                "Thu",
                "Fri",
                "Sat"
            };

            VerificationHelper(info, expected);
        }

        // PosTest3: Call GetAbbreviatedDayName on fr-FR culture DateTimeFormatInfo instance
        [Fact]
        public void PosTest3()
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;
            string[] expected = new string[] {
                "dim.",
                "lun.",
                "mar.",
                "mer.",
                "jeu.",
                "ven.",
                "sam."
            };

            VerificationHelper(info, expected);
        }

        // PosTest4: Call GetAbbreviatedDayName on DateTimeFormatInfo instance created from ctor
        [Fact]
        public void PosTest4()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            string[] expected = new string[] {
                "Sun",
                "Mon",
                "Tue",
                "Wed",
                "Thu",
                "Fri",
                "Sat"
            };

            VerificationHelper(info, expected);
        }

        // NegTest1: ArgumentOutOfRangeException should be thrown when dayofweek is not a valid System.DayOfWeek value
        [Fact]
        public void NegTest1()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                info.GetAbbreviatedDayName((DayOfWeek)(-1));
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string[] expected)
        {
            DayOfWeek[] values = new DayOfWeek[] {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday
            };

            for (int i = 0; i < values.Length; ++i)
            {
                string actual = info.GetAbbreviatedDayName(values[i]);
                Assert.Equal(expected[i], actual);
            }
        }
    }
}
