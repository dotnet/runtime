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
        public void VietnameseTest_MatchAbbreviatedMonthName_ContinuesSearchAfterMatch(string monthAbberavition, int expectedMonthNum)
        {
            var vi = new System.Globalization.CultureInfo("vi-VN");
            Assert.Equal(expectedMonthNum, DateTime.ParseExact(monthAbberavition, "MMM", vi.DateTimeFormat).Month);
        }
    }
}
