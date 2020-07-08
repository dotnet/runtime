// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        public static IEnumerable<object[]> Calendars_TestData()
        {
            //                              Calendar               yearHasLeapMonth        CalendarAlgorithmType
            yield return new object[] { new ChineseLunisolarCalendar()  , 2017  , CalendarAlgorithmType.LunisolarCalendar   };
            yield return new object[] { new GregorianCalendar()         , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new HebrewCalendar()            , 5345  , CalendarAlgorithmType.LunisolarCalendar   };
            yield return new object[] { new HijriCalendar()             , 0     , CalendarAlgorithmType.LunarCalendar       };
            yield return new object[] { new JapaneseCalendar()          , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new JapaneseLunisolarCalendar() , 29    , CalendarAlgorithmType.LunisolarCalendar   };
            yield return new object[] { new JulianCalendar()            , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new KoreanCalendar()            , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new KoreanLunisolarCalendar()   , 2017  , CalendarAlgorithmType.LunisolarCalendar   };
            yield return new object[] { new PersianCalendar()           , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new TaiwanCalendar()            , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new TaiwanLunisolarCalendar()   , 106   , CalendarAlgorithmType.LunisolarCalendar   };
            yield return new object[] { new ThaiBuddhistCalendar()      , 0     , CalendarAlgorithmType.SolarCalendar       };
            yield return new object[] { new UmAlQuraCalendar()          , 0     , CalendarAlgorithmType.LunarCalendar       };
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void CloningTest(Calendar calendar, int yearHasLeapMonth, CalendarAlgorithmType algorithmType)
        {
            _ = yearHasLeapMonth;
            _ = algorithmType;
            Calendar cloned = (Calendar) calendar.Clone();
            Assert.Equal(calendar.GetType(), cloned.GetType());
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void GetLeapMonthTest(Calendar calendar, int yearHasLeapMonth, CalendarAlgorithmType algorithmType)
        {
            _ = algorithmType;
            if (yearHasLeapMonth > 0)
            {
                Assert.NotEqual(0, calendar.GetLeapMonth(yearHasLeapMonth));
                Assert.Equal(0, calendar.GetLeapMonth(yearHasLeapMonth - 1));
            }
            else
            {
                Assert.True(calendar.GetLeapMonth(calendar.GetYear(DateTime.Today)) == 0,
                            "calendar.GetLeapMonth returned wrong value");
            }
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void ReadOnlyTest(Calendar calendar, int yearHasLeapMonth, CalendarAlgorithmType algorithmType)
        {
            _ = yearHasLeapMonth;
            _ = algorithmType;
            Assert.False(calendar.IsReadOnly);
            var readOnlyCal = Calendar.ReadOnly(calendar);
            Assert.True(readOnlyCal.IsReadOnly, "expect readOnlyCal.IsReadOnly returns true");
            var colnedCal = (Calendar) readOnlyCal.Clone();
            Assert.False(colnedCal.IsReadOnly, "expect colnedCal.IsReadOnly returns false");
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AlgorithmTypeTest(Calendar calendar, int yearHasLeapMonth, CalendarAlgorithmType algorithmType)
        {
            _ = yearHasLeapMonth;
            Assert.Equal(calendar.AlgorithmType, algorithmType);
        }

        [Fact]
        public static void CalendarErasTest()
        {
            Assert.Equal(1, ChineseLunisolarCalendar.ChineseEra);
            Assert.Equal(1, GregorianCalendar.ADEra);
            Assert.Equal(1, JapaneseLunisolarCalendar.JapaneseEra);
            Assert.Equal(1, HebrewCalendar.HebrewEra);
            Assert.Equal(1, HijriCalendar.HijriEra);
            Assert.Equal(1, JulianCalendar.JulianEra);
            Assert.Equal(1, KoreanCalendar.KoreanEra);
            Assert.Equal(1, KoreanLunisolarCalendar.GregorianEra);
            Assert.Equal(1, PersianCalendar.PersianEra);
            Assert.Equal(1, ThaiBuddhistCalendar.ThaiBuddhistEra);
            Assert.Equal(1, UmAlQuraCalendar.UmAlQuraEra);
        }
    }
}
