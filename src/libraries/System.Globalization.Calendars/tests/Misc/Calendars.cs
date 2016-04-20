// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarsTests
    {
        [Fact]
        public static void HijriTest()
        {
            HijriCalendar cal1 = new HijriCalendar();
            int ad = cal1.HijriAdjustment;
            Assert.True(ad >= -2 && ad <= 2);
        }

        [Fact]
        public static void UmAlQuraTest()
        {
            UmAlQuraCalendar cal2 = new UmAlQuraCalendar();
            Assert.False(cal2.IsLeapMonth(1400, 10));
        }

        [Fact]
        public static void ChineseLunisolarTest()
        {
            ChineseLunisolarCalendar cal3 = new ChineseLunisolarCalendar();
            int[] eras = cal3.Eras;
            Assert.Equal(1, eras.Length);
        }

        [Fact]
        public static void HebrewTest()
        {
            HebrewCalendar cal = new HebrewCalendar();
            Assert.False(cal.IsLeapMonth(5343, 4));
        }

        [Fact]
        public static void JapaneseTest()
        {
            JapaneseCalendar cal = new JapaneseCalendar();
            Assert.True(cal.Eras.Length >= 4);
        }

        [Fact]
        public static void JulianTest()
        {
            JulianCalendar jc = new JulianCalendar();
            Assert.False(jc.IsLeapYear(1999));
        }

        [Fact]
        public static void KoreanLunisolarTest()
        {
            KoreanLunisolarCalendar kls = new KoreanLunisolarCalendar();
            Assert.Equal(1, kls.Eras.Length);
        }

        [Fact]
        public static void TaiwanLunisolarTest()
        {
            TaiwanLunisolarCalendar tc = new TaiwanLunisolarCalendar();
            Assert.Equal(1, tc.Eras.Length);
        }
    }
}
