// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public static class CalendarsTests
    {
        [Fact]
        public static void GregorianTest()
        {
            GregorianCalendar cal = new GregorianCalendar();
            cal = new GregorianCalendar(GregorianCalendarTypes.Arabic);
            cal = new GregorianCalendar(GregorianCalendarTypes.Localized);
            cal = new GregorianCalendar(GregorianCalendarTypes.MiddleEastFrench);
            cal = new GregorianCalendar(GregorianCalendarTypes.TransliteratedEnglish);
            cal = new GregorianCalendar(GregorianCalendarTypes.TransliteratedFrench);
            cal = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
        }

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
        public static void JapaneseLunisolarTest()
        {
            JapaneseLunisolarCalendar cal = new JapaneseLunisolarCalendar();
            Assert.True(cal.Eras.Length >= 2);
        }

        [Fact]
        public static void JulianTest()
        {
            JulianCalendar jc = new JulianCalendar();
            Assert.False(jc.IsLeapYear(1999));
        }

        [Fact]
        public static void KoreanTest()
        {
            KoreanCalendar kc = new KoreanCalendar();
            Assert.Equal(1, kc.Eras.Length);
        }

        [Fact]
        public static void KoreanLunisolarTest()
        {
            KoreanLunisolarCalendar kls = new KoreanLunisolarCalendar();
            Assert.Equal(1, kls.Eras.Length);
        }

        [Fact]
        public static void PersianTest()
        {
            PersianCalendar pc = new PersianCalendar();
            Assert.Equal(1, pc.Eras.Length);
        }

        [Fact]
        public static void TaiwanTest()
        {
            TaiwanCalendar tc = new TaiwanCalendar();
            Assert.Equal(1, tc.Eras.Length);
        }

        [Fact]
        public static void TaiwanLunisolarTest()
        {
            TaiwanLunisolarCalendar tc = new TaiwanLunisolarCalendar();
            Assert.Equal(1, tc.Eras.Length);
        }

        [Fact]
        public static void ThaiBuddhistTest()
        {
            ThaiBuddhistCalendar tc = new ThaiBuddhistCalendar();
            Assert.Equal(1, tc.Eras.Length);
        }
    }
}
