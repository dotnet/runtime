// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public abstract class CalendarTestBase
    {
        public abstract Calendar Calendar { get; }

        public virtual DateTime MinSupportedDateTime => DateTime.MinValue;

        public virtual DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public virtual int[] Eras => new int[] { 1 };

        public virtual bool SkipErasTest => false;

        public virtual CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public virtual bool IsReadOnly => false;

        [Fact]
        public void MinSupportedDateTime_Get_ReturnsExpected()
        {
            Assert.Equal(MinSupportedDateTime, Calendar.MinSupportedDateTime);
        }

        [Fact]
        public void MaxSupportedDateTime_Get_ReturnsExpected()
        {
            Assert.Equal(MaxSupportedDateTime, Calendar.MaxSupportedDateTime);
        }

        [Fact]
        public void Eras_Get_ReturnsExpected()
        {
            if (SkipErasTest)
            {
                return;
            }

            Assert.Equal(Eras, Calendar.Eras);
        }

        [Fact]
        public void Eras_Get_ReturnsDifferentInstance()
        {
            Calendar calendar = Calendar;
            Assert.NotSame(calendar.Eras, calendar.Eras);
        }

        [Fact]
        public void AlgorithmType_Get_ReturnsExpected()
        {
            Assert.Equal(AlgorithmType, Calendar.AlgorithmType);
        }

        [Fact]
        public void IsReadOnly_Get_ReturnsExpected()
        {
            Assert.Equal(IsReadOnly, Calendar.IsReadOnly);
        }

        public enum DataType
        {
            Year = 1,
            Month = 2,
            Day = 8
        }

        private static int MinEra(Calendar calendar) => calendar.GetEra(calendar.MinSupportedDateTime);
        private static int MaxEra(Calendar calendar) => calendar.GetEra(calendar.MaxSupportedDateTime);

        private static int MaxCalendarYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[0] || era == 0)
            {
                return calendar.GetYear(calendar.MaxSupportedDateTime);
            }
            return calendar.GetYear(calendar.ToDateTime(1, 1, 1, 0, 0, 0, 0, era + 1).AddDays(-1)) + 1;
        }

        // Get the max year in the passed era plus the sum of the max year for each subsequent era
        private static int MaxCalendarYearInEras(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[0] || era == 0)
            {
                return MaxCalendarYearInEra(calendar, era);
            }

            int year = 0;
            for (int i = era; i <= calendar.Eras[0]; i++)
            {
                year += MaxCalendarYearInEra(calendar, i);
            }

            return year;
        }

        private static int MaxGregorianYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[0] || era == 0)
            {
                return calendar.MaxSupportedDateTime.Year;
            }
            return (calendar.ToDateTime(1, 1, 1, 0, 0, 0, 0, era + 1).AddDays(-1)).Year;
        }

        private static int MinGregorianYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[eras.Length - 1] || era == 0)
            {
                return calendar.MinSupportedDateTime.Year;
            }
            return calendar.ToDateTime(1, 1, 1, 0, 0, 0, 0, era).Year;
        }

        private static int MinCalendarYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[eras.Length - 1] || era == 0)
            {
                return calendar.GetYear(calendar.MinSupportedDateTime);
            }
            return calendar.GetYear(calendar.ToDateTime(1, 1, 1, 0, 0, 0, 0, era));
        }

        public IEnumerable<(int year, int month, int day, int era, string exceptionParamName)> Year_Month_Day_Era_TestData(Calendar calendar, DataType type)
        {
            int month = 1;
            int day = 1;
            if (calendar is JapaneseLunisolarCalendar && PlatformDetection.IsFullFramework)
            {
                // desktop has a bug in JapaneseLunisolarCalendar which is fixed in .NET Core.
                // in case of a new era starts in the middle of a month which means part of the month will belong to one
                // era and the rest will belong to the new era. When calculating the calendar year number for dates which
                // in the rest of the month and exist in the new started era, we should still use the old era info instead
                // of the new era info because the rest of the month still belong to the year of last era.
                // https://github.com/dotnet/coreclr/pull/3662
                yield break;
            }
            foreach (int era in calendar.Eras)
            {
                int year = MaxCalendarYearInEra(calendar, era) - 2;

                // Year is invalid
                yield return (-1, month, day, era, "year");
                yield return (0, month, day, era, "year");

                yield return (MaxCalendarYearInEras(calendar, era) + 1, month, day, era, "year");

                if ((type & DataType.Month) != 0)
                {
                    // Month is invalid
                    yield return (year, -1, day, era, "month");
                    yield return (year, 0, day, era, "month");
                    yield return (year, calendar.GetMonthsInYear(year, era) + 1, day, era, "month");
                }

                if ((type & DataType.Day) != 0)
                {
                    // Day is invalid
                    yield return (year, month, -1, era, "day");
                    yield return (year, month, 0, era, "day");
                    yield return (year, month, calendar.GetDaysInMonth(year, month, era) + 1, era, "day");
                }
            }
            // Year is invalid
            yield return (MinCalendarYearInEra(calendar, MinEra(calendar)) - 1, month, day, MinEra(calendar), "year");

            // Era is invalid
            yield return (calendar.GetYear(calendar.MaxSupportedDateTime), month, day, MinEra(calendar) - 2, "era");
            yield return (calendar.GetYear(calendar.MaxSupportedDateTime), month, day, MaxEra(calendar) + 1, "era");
        }

        public static IEnumerable<DateTime> DateTime_TestData(Calendar calendar)
        {
            DateTime minDate = calendar.MinSupportedDateTime;
            if (minDate != DateTime.MinValue)
            {
                yield return minDate.AddDays(-1);
            }
            DateTime maxDate = calendar.MaxSupportedDateTime;
            if (maxDate != DateTime.MaxValue)
            {
                yield return maxDate.AddDays(1);
            }
        }

        [Fact]
        public void GetDaysInYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.GetDaysInYear(test.year, test.era))
            );
        }

        [Fact]
        public void GetMonthsInYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.GetMonthsInYear(test.year, test.era))
            );
        }

        [Fact]
        public void GetDaysInMonth_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year | DataType.Month), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.GetDaysInMonth(test.year, test.month, test.era))
            );
        }

        [Fact]
        public void IsLeapDay_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year | DataType.Month | DataType.Day), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.IsLeapDay(test.year, test.month, test.day, test.era))
            );
        }

        [Fact]
        public void IsLeapMonth_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year | DataType.Month), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.IsLeapMonth(test.year, test.month, test.era))
            );
        }

        [Fact]
        public void IsLeapYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.IsLeapYear(test.year, test.era))
            );
        }

        [Fact]
        public void GetLeapMonth_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(Year_Month_Day_Era_TestData(calendar, DataType.Year), test =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>(test.exceptionParamName, () => calendar.GetLeapMonth(test.year, test.era))
            );
        }

        [Fact]
        public void AddYears_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddYears(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddYears(calendar.MinSupportedDateTime, -1));
        }

        [Fact]
        public void AddMonths_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddMonths(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddMonths(calendar.MinSupportedDateTime, -1)); // JapaneseCalendar throws ArgumentException

            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, -120001));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, 120001));
        }

        [Fact]
        public void AddDays_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddDays(calendar.MaxSupportedDateTime, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddDays(calendar.MinSupportedDateTime, -1));

            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddDays(DateTime.Now, -120001 * 30));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddDays(DateTime.Now, 120001 * 30));
        }

        [Fact]
        public void AddHours_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddHours(calendar.MaxSupportedDateTime, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddHours(calendar.MinSupportedDateTime, -1));

            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddHours(DateTime.Now, -120001 * 30 * 24));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddHours(DateTime.Now, 120001 * 30 * 24));
        }

        [Fact]
        public void AddMinutes_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddMinutes(calendar.MaxSupportedDateTime, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddMinutes(calendar.MinSupportedDateTime, -1));
        }

        [Fact]
        public void AddSeconds_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddSeconds(calendar.MaxSupportedDateTime, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddSeconds(calendar.MinSupportedDateTime, -1));
        }

        [Fact]
        public void AddMilliseconds_Invalid_ThrowsArgumentException()
        {
            Calendar calendar = Calendar;
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddMilliseconds(calendar.MaxSupportedDateTime, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => calendar.AddMilliseconds(calendar.MinSupportedDateTime, -1));
        }

        [Fact]
        public void GetWeekOfYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;

            // Rule is outside supported range
            AssertExtensions.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay - 1, DayOfWeek.Saturday));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstFourDayWeek + 1, DayOfWeek.Saturday));

            // FirstDayOfWeek is outside supported range
            AssertExtensions.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday - 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Saturday + 1));
        }

        [Fact]
        public void ToDateTime_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            if (PlatformDetection.IsFullFramework && calendar is JapaneseLunisolarCalendar)
            {
                return;
            }

            int month = 1;
            int day = 1;
            int hour = 1;
            int minute = 1;
            int second = 1;
            int millisecond = 1;
            foreach (int era in calendar.Eras)
            {
                int year = MaxCalendarYearInEra(calendar, era) - 2;

                // Year is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(-1, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(0, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(MaxCalendarYearInEras(calendar, era) + 1, month, day, hour, minute, second, millisecond, era));

                // Month is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, -1, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, 0, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, calendar.GetMonthsInYear(year, era) + 1, day, hour, minute, second, millisecond, era));

                // Day is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, -1, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, 0, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, calendar.GetDaysInMonth(year, month, era) + 1, hour, minute, second, millisecond, era));

                // Hour is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, -1, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, 60, minute, second, millisecond, era));

                // Minute is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, -1, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, 60, second, millisecond, era));

                // Second is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, -1, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, 60, millisecond, era));

                // Millisecond is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, second, -1, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, second, 1000, era));
            }
            // Year is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(MinCalendarYearInEra(calendar, MinEra(calendar)) - 1, month, day, hour, minute, second, millisecond, MinEra(calendar)));

            // Era is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(calendar.GetYear(calendar.MaxSupportedDateTime), month, day, hour, minute, second, millisecond, MinEra(calendar) - 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(calendar.GetYear(calendar.MaxSupportedDateTime), month, day, hour, minute, second, millisecond, MaxEra(calendar) + 1));

            // New date is out of range
            DateTime minDateTime = calendar.MinSupportedDateTime;
            int minEra = calendar.GetEra(minDateTime);
            int minYear = calendar.GetYear(minDateTime);

            DateTime maxDateTime = calendar.MaxSupportedDateTime;
            int maxEra = calendar.GetEra(maxDateTime);
            int maxYear = calendar.GetYear(maxDateTime);

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear - 1, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear + 1, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));
        }

        [Fact]
        public void ToFourDigitYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;

            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MaxCalendarYearInEra(calendar, MaxEra(calendar)) + 1));

            if (!(calendar is JapaneseLunisolarCalendar))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MinCalendarYearInEra(calendar, MinEra(calendar)) - 2));
            }
        }

        [Fact]
        public void TwoDigitYearMax_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = 98);

            int max = Math.Max(MaxGregorianYearInEra(calendar, MaxEra(calendar)), MaxCalendarYearInEra(calendar, MaxEra(calendar)));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = max + 1);
        }

        [Fact]
        public void GetEra_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
            {
                // JapaneseCalendar throws on Unix (ICU), but not on Windows
                if ((calendar is JapaneseCalendar && PlatformDetection.IsWindows) || calendar is HebrewCalendar || calendar is TaiwanLunisolarCalendar || calendar is JapaneseLunisolarCalendar)
                {
                    calendar.GetEra(dt);
                }
                else
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => calendar.GetEra(dt));
                }
            });
        }

        [Fact]
        public void GetYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetYear(dt))
            );
        }

        [Fact]
        public void GetMonth_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetMonth(dt))
            );
        }

        [Fact]
        public void GetDayOfYear_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfYear(dt))
            );
        }

        [Fact]
        public void GetDayOfMonth_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
                AssertExtensions.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfMonth(dt))
            );
        }

        [Fact]
        public void GetDayOfWeek_Invalid_ThrowsArgumentOutOfRangeException()
        {
            Calendar calendar = Calendar;
            Assert.All(DateTime_TestData(calendar), dt =>
            {
                if (calendar is HijriCalendar || calendar is UmAlQuraCalendar || calendar is PersianCalendar || calendar is HebrewCalendar)
                {
                    calendar.GetDayOfWeek(dt);
                }
                else
                {
                    AssertExtensions.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfWeek(dt));
                }
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void TestJapaneseCalendarDateParsing()
        {
            CultureInfo ciJapanese = new CultureInfo("ja-JP") { DateTimeFormat = { Calendar = new JapaneseCalendar() } };

            DateTime dt = new DateTime(1970, 1, 1);
            string eraName = dt.ToString("gg", ciJapanese);
            Assert.Equal(new DateTime(1995, 1, 1), DateTime.Parse(eraName + " 70/1/1 0:00:00", ciJapanese));
        }
    }
}
