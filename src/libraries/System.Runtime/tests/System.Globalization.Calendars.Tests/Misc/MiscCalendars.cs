// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public static class MiscCalendarsTests
    {
        [Fact]
        public static void HebrewTest()
        {
            Calendar hCal = new HebrewCalendar();
            DateTime dTest = hCal.ToDateTime(5360, 04, 14, 0, 0, 0, 0);
            Assert.True(dTest.Equals(new DateTime(1600, 1, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(0, 03, 25, 0, 0, 0, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(10000, 03, 25, 0, 0, 0, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(5000, 0, 25, 0, 0, 0, 0);
            });
        }

        [Fact]
        public static void HijriTest()
        {
            HijriCalendar hCal = new HijriCalendar();
            DateTime dTest = hCal.ToDateTime(1008, 06, 15, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1600, 1, 1).AddDays(hCal.HijriAdjustment));
        }

        [Fact]
        public static void JapaneseTest()
        {
            JapaneseCalendar jCal = new JapaneseCalendar();
            DateTime dTest = jCal.ToDateTime(1, 1, 8, 0, 0, 0, 0, 4);
            Assert.Equal(dTest, new DateTime(1989, 1, 8));
        }
        
        [Theory]
        [InlineData(@"Thg 1", 1)]
        [InlineData(@"Thg 2", 2)]
        [InlineData(@"Thg 10", 10)]
        [InlineData(@"Thg 11", 11)]
        [InlineData(@"Thg 12", 12)]
        //[InlineData(@"Thg 13", 13)] Needs supported calendar with 13 months
        public static void VietnameseTest_MatchAbbreviatedMonthName_ContinuesSearchAfterMatch(string monthAbberavition, int expectedMonthNum)
        {
            var dtfi = new System.Globalization.CultureInfo("vi-VN").DateTimeFormat;
            dtfi.AbbreviatedMonthNames = new string[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12", "Thg 13" }; // 13 months
            Assert.Equal(expectedMonthNum, DateTime.ParseExact(monthAbberavition, "MMM", dtfi).Month);
        }
    }
}
