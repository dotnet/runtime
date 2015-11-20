// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetDayName
    {
        // PosTest1: Call GetDayName on default invariant DateTimeFormatInfo instance
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] expected = new string[] {
                "Sunday",
                "Monday",
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
            };

            VerificationHelper(info, expected);
        }

        // Call GetDayName on en-us culture DateTimeFormatInfo instance
        // Call GetDayName on fr-FR culture DateTimeFormatInfo instance
        [Theory]
        [InlineData("en-us")]
        [InlineData("fr-FR")]
        public void PosTest2(string localeName)
        {
            CultureInfo culture = new CultureInfo(localeName);
            string[] expected = DateTimeFormatInfoData.GetDayNames(culture);

            DateTimeFormatInfo info = culture.DateTimeFormat;
            VerificationHelper(info, expected);
        }

        // PosTest4: Call GetDayName on DateTimeFormatInfo instance created from ctor
        [Fact]
        public void PosTest4()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            string[] expected = new string[] {
                "Sunday",
                "Monday",
                "Tuesday",
                "Wednesday",
                "Thursday",
                "Friday",
                "Saturday"
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
                string actual = info.GetDayName(values[i]);
                Assert.Equal(expected[i], actual);
            }
        }
    }
}
